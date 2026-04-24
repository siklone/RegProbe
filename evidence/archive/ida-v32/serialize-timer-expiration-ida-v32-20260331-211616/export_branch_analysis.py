import json
import os
import sys

import ida_funcs
import ida_hexrays
import ida_lines
import ida_nalt
import ida_name
import idaapi
import idautils
import idc


def bounded_context(ea, radius=5):
    items = []
    current = ea
    before = []
    for _ in range(radius):
        current = idc.prev_head(current)
        if current == idc.BADADDR:
            break
        before.append(f"{current:08X}  {idc.generate_disasm_line(current, 0) or ''}".rstrip())
    before.reverse()

    current = ea
    branch = [f"{ea:08X}  {idc.generate_disasm_line(ea, 0) or ''}".rstrip()]
    current = ea
    for _ in range(radius):
        current = idc.next_head(current)
        if current == idc.BADADDR:
            break
        line = f"{current:08X}  {idc.generate_disasm_line(current, 0) or ''}".rstrip()
        mnemonic = (idc.print_insn_mnem(current) or "").upper()
        if mnemonic.startswith("J") or mnemonic.startswith("CMP") or mnemonic.startswith("TEST") or mnemonic.startswith("CMOV"):
            branch.append(line)

    current = ea
    after = []
    for _ in range(radius):
        current = idc.next_head(current)
        if current == idc.BADADDR:
            break
        after.append(f"{current:08X}  {idc.generate_disasm_line(current, 0) or ''}".rstrip())
    return before, branch, after


def infer_value_map(branch_lines):
    blob = " ".join(branch_lines).lower()
    if ", 0" in blob or ",0" in blob:
        return "value=0 participates in this conditional block; opposite branch still needs explicit review."
    if ", 1" in blob or ",1" in blob:
        return "value=1 participates in this conditional block; opposite branch still needs explicit review."
    return "unclear"


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
                unclear = function_source != "pdb-symbol" or len(branch) == 0
                matches.append(
                    {
                        "pattern": pattern,
                        "address": f"{xref.frm:08X}",
                        "function_name": func_name,
                        "function_source": function_source,
                        "unclear": unclear,
                        "value_map": infer_value_map(branch),
                        "effect_summary": "unclear - keep this as review-only until a symbol-backed branch mapping is available."
                        if unclear
                        else "IDA recovered a symbolized function and bounded branch block for this value.",
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
