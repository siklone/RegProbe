import json
import os
import re
import sys

import ida_funcs
import ida_hexrays
import ida_nalt
import ida_name
import idautils
import idc

REGISTER_NAMES = (
    "RAX", "RBX", "RCX", "RDX", "RSI", "RDI", "RSP", "RBP",
    "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15",
    "EAX", "EBX", "ECX", "EDX", "ESI", "EDI", "ESP", "EBP",
)


def format_line(ea):
    return f"{ea:08X}  {idc.generate_disasm_line(ea, 0) or ''}".rstrip()


def extract_mnemonic(line):
    if not line:
        return ""
    body = line.strip()
    body = re.sub(r"^[0-9A-Fa-f]+\s+", "", body)
    parts = body.split(None, 1)
    if not parts:
        return ""
    return parts[0].upper()


def looks_like_branch(ea):
    mnemonic = (idc.print_insn_mnem(ea) or "").upper()
    return (
        mnemonic.startswith("J")
        or mnemonic.startswith("CMP")
        or mnemonic.startswith("TEST")
        or mnemonic.startswith("CMOV")
    )


def bounded_context(ea, radius=5):
    current = ea
    before = []
    for _ in range(radius):
        current = idc.prev_head(current)
        if current == idc.BADADDR:
            break
        before.append(format_line(current))
    before.reverse()

    branch = []
    seen = set()
    if ea != idc.BADADDR:
        anchor = format_line(ea)
        branch.append(anchor)
        seen.add(anchor)

    current = ea
    for _ in range(radius):
        current = idc.next_head(current)
        if current == idc.BADADDR:
            break
        if looks_like_branch(current):
            line = format_line(current)
            if line not in seen:
                branch.append(line)
                seen.add(line)

    current = ea
    back_branch = []
    for _ in range(radius):
        current = idc.prev_head(current)
        if current == idc.BADADDR:
            break
        if looks_like_branch(current):
            line = format_line(current)
            if line not in seen:
                back_branch.append(line)
                seen.add(line)
    back_branch.reverse()
    branch = back_branch + branch

    current = ea
    after = []
    for _ in range(radius):
        current = idc.next_head(current)
        if current == idc.BADADDR:
            break
        after.append(format_line(current))
    return before, branch, after


def collect_registers(lines):
    found = []
    seen = set()
    for line in lines:
        normalized = (
            " "
            + line.upper()
            .replace("[", " ")
            .replace("]", " ")
            .replace(",", " ")
            .replace("+", " ")
            .replace("-", " ")
            .replace(":", " ")
            + " "
        )
        for register in REGISTER_NAMES:
            token = f" {register} "
            if token in normalized and register not in seen:
                found.append(register)
                seen.add(register)
    return found


def infer_flags(lines):
    flags = []
    seen = set()
    for line in lines:
        mnemonic = extract_mnemonic(line)
        candidates = []
        if mnemonic.startswith(("TEST", "JE", "JNE", "JZ", "JNZ")):
            candidates.append("ZF")
        if mnemonic.startswith(("JA", "JB", "JC", "JAE", "JBE")):
            candidates.extend(["CF", "ZF"])
        if mnemonic.startswith(("JG", "JL", "JGE", "JLE")):
            candidates.extend(["SF", "OF", "ZF"])
        if mnemonic.startswith(("JO", "JNO")):
            candidates.append("OF")
        if mnemonic.startswith(("JS", "JNS")):
            candidates.append("SF")
        if mnemonic.startswith("CMP"):
            candidates.extend(["ZF", "CF", "SF", "OF"])
        for flag in candidates:
            if flag not in seen:
                flags.append(flag)
                seen.add(flag)
    return flags


def first_compare_condition(lines):
    for line in lines:
        mnemonic = extract_mnemonic(line)
        if mnemonic.startswith(("CMP", "TEST")):
            return line
    return "unclear"


def first_jump_condition(lines):
    for line in lines:
        mnemonic = extract_mnemonic(line)
        if mnemonic.startswith("J"):
            return line
    return "unclear"


def infer_value_map(branch_lines):
    blob = " ".join(branch_lines).lower()
    if ",0" in blob or ", 0" in blob or " 0x0" in blob or " 00h" in blob:
        return "value=0 participates in this conditional block; opposite branch still needs explicit review."
    if ",1" in blob or ", 1" in blob or " 0x1" in blob or " 01h" in blob:
        return "value=1 participates in this conditional block; opposite branch still needs explicit review."
    return "unclear"


def infer_stack_summary(lines):
    blob = " ".join(lines).upper()
    if "[RSP" in blob or "[RBP" in blob or " RSP" in blob or " RBP" in blob:
        return "stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics."
    return "no obvious stack-relative access in the bounded context."


def is_exception_adjacent(lines):
    blob = " ".join(lines).upper()
    return any(token in blob for token in (" INT1", " INT3", " UD2", " HLT", " ICEBP"))


def infer_function_confidence(function_source, compare_condition, jump_condition, exception_review_required):
    if (
        function_source == "pdb-symbol"
        and compare_condition != "unclear"
        and jump_condition != "unclear"
        and not exception_review_required
    ):
        return "symbolized_branch"
    return "string_only_review"


def infer_branch_effect(compare_condition, jump_condition, exception_review_required):
    if exception_review_required:
        return "trap/fault-adjacent block detected; control-flow may be misleading."
    if compare_condition != "unclear" and jump_condition != "unclear":
        return "compare + conditional jump recovered in bounded context."
    if compare_condition != "unclear":
        return "comparison recovered, but nearby jump condition is still unclear."
    if jump_condition != "unclear":
        return "jump recovered, but the compare/test anchor is still unclear."
    return "unclear"


def infer_effect_summary(function_confidence, value_map, exception_review_required):
    if function_confidence == "symbolized_branch" and value_map != "unclear":
        return "PDB-backed function identity, compare/jump structure, and a bounded value map are present."
    if exception_review_required:
        return "unclear - exception-adjacent control flow needs manual review before any semantic claim."
    return "unclear - keep this as review-only until a PDB-backed branch mapping is available."


def score_evidence(function_source, function_confidence, compare_condition, jump_condition, value_map, stack_summary, exception_review_required):
    score = 0
    if function_source == "pdb-symbol":
        score += 30
    if function_confidence == "symbolized_branch":
        score += 20
    if compare_condition != "unclear":
        score += 15
    if jump_condition != "unclear":
        score += 15
    if value_map != "unclear":
        score += 10
    if stack_summary.startswith("stack-relative"):
        score += 5
    if exception_review_required:
        score -= 25
    if function_source == "unresolved":
        score -= 15
    return max(0, min(100, score))


def score_reasons(function_source, function_confidence, compare_condition, jump_condition, value_map, stack_summary, exception_review_required):
    reasons = []
    if function_source == "pdb-symbol":
        reasons.append("pdb-symbol present")
    if function_confidence == "symbolized_branch":
        reasons.append("compare+jump survived bounded symbolized review")
    if compare_condition != "unclear":
        reasons.append("compare/test anchor found")
    if jump_condition != "unclear":
        reasons.append("conditional jump found")
    if value_map != "unclear":
        reasons.append("value immediate found in bounded block")
    if stack_summary.startswith("stack-relative"):
        reasons.append("stack-relative context detected")
    if exception_review_required:
        reasons.append("exception/trap gate forced review-only")
    if not reasons:
        reasons.append("string match only")
    return reasons


def main():
    if len(idc.ARGV) < 5:
        print("Usage: export_branch_analysis.py <output-json> <probe-name> <pdb-source> <pattern-1> [pattern-2] [...]")
        return 1

    output_json = idc.ARGV[1]
    probe_name = idc.ARGV[2]
    pdb_source = idc.ARGV[3]
    patterns = [arg for arg in idc.ARGV[4:] if arg]

    if ida_hexrays.init_hexrays_plugin():
        ida_hexrays.load_plugin_decompiler()

    matches = []
    pdb_loaded = False

    strings = idautils.Strings()
    strings.setup(strtypes=[ida_nalt.STRTYPE_C, ida_nalt.STRTYPE_C_16], ignore_instructions=False, display_only_existing_strings=True)

    for pattern in patterns:
        needle = pattern.lower()
        emitted = 0
        for string_item in strings:
            rendered = str(string_item)
            if needle not in rendered.lower():
                continue
            for xref in idautils.XrefsTo(string_item.ea):
                func = ida_funcs.get_func(xref.frm)
                func_name = ida_name.get_name(func.start_ea) if func else "<no function>"
                function_source = "pdb-symbol" if func and not func_name.startswith("sub_") and not func_name.startswith("nullsub_") else ("auto-analysis-fallback" if func else "unresolved")
                if function_source == "pdb-symbol":
                    pdb_loaded = True
                before, branch, after = bounded_context(xref.frm)
                all_lines = before + branch + after
                register_focus = collect_registers(all_lines) or ["unclear"]
                flag_focus = infer_flags(branch) or ["unclear"]
                compare_condition = first_compare_condition(branch)
                jump_condition = first_jump_condition(branch)
                value_map = infer_value_map(branch)
                stack_summary = infer_stack_summary(all_lines)
                exception_review_required = is_exception_adjacent(all_lines)
                exception_reason = (
                    "trap-or-fault-adjacent instructions present; control-flow may be misleading."
                    if exception_review_required
                    else "none"
                )
                function_confidence = infer_function_confidence(
                    function_source=function_source,
                    compare_condition=compare_condition,
                    jump_condition=jump_condition,
                    exception_review_required=exception_review_required,
                )
                branch_effect = infer_branch_effect(
                    compare_condition=compare_condition,
                    jump_condition=jump_condition,
                    exception_review_required=exception_review_required,
                )
                heuristic_score = score_evidence(
                    function_source=function_source,
                    function_confidence=function_confidence,
                    compare_condition=compare_condition,
                    jump_condition=jump_condition,
                    value_map=value_map,
                    stack_summary=stack_summary,
                    exception_review_required=exception_review_required,
                )
                heuristic_reasons = score_reasons(
                    function_source=function_source,
                    function_confidence=function_confidence,
                    compare_condition=compare_condition,
                    jump_condition=jump_condition,
                    value_map=value_map,
                    stack_summary=stack_summary,
                    exception_review_required=exception_review_required,
                )
                unclear = (
                    function_confidence != "symbolized_branch"
                    or value_map == "unclear"
                    or exception_review_required
                )
                matches.append(
                    {
                        "pattern": pattern,
                        "address": f"{xref.frm:08X}",
                        "function_name": func_name,
                        "function_source": function_source,
                        "function_confidence": function_confidence,
                        "unclear": unclear,
                        "value_map": value_map,
                        "compare_condition": compare_condition,
                        "jump_condition": jump_condition,
                        "branch_effect": branch_effect,
                        "stack_summary": stack_summary,
                        "exception_review_required": exception_review_required,
                        "exception_reason": exception_reason,
                        "heuristic_score": heuristic_score,
                        "heuristic_reasons": heuristic_reasons,
                        "register_focus": register_focus,
                        "flag_focus": flag_focus,
                        "effect_summary": infer_effect_summary(
                            function_confidence=function_confidence,
                            value_map=value_map,
                            exception_review_required=exception_review_required,
                        ),
                        "context_before": before,
                        "branch_snippet": branch,
                        "context_after": after,
                    }
                )
                emitted += 1
                if emitted >= 6:
                    break
            if emitted >= 6:
                break

    payload = {
        "binary": os.path.basename(ida_nalt.get_input_file_path()),
        "probe": probe_name,
        "pdb_source": pdb_source,
        "pdb_loaded": pdb_loaded,
        "matches": matches,
    }
    with open(output_json, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
    return 0


if __name__ == "__main__":
    code = main()
    idc.qexit(code)
