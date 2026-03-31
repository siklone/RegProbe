import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.DataIterator;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class ExportBranchAnalysis extends GhidraScript {
    private static final int MAX_STRINGS_PER_PATTERN = 4;
    private static final int MAX_REFERENCES_PER_STRING = 6;
    private static final int CONTEXT_LINES = 5;

    private static final String[] REGISTER_NAMES = new String[] {
        "RAX", "RBX", "RCX", "RDX", "RSI", "RDI", "RSP", "RBP",
        "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15",
        "EAX", "EBX", "ECX", "EDX", "ESI", "EDI", "ESP", "EBP"
    };

    private static final class MatchEvidence {
        String pattern;
        String address;
        String functionName;
        String functionSource;
        String functionConfidence;
        boolean unclear;
        String valueMap;
        String effectSummary;
        String compareCondition;
        String jumpCondition;
        String branchEffect;
        String stackSummary;
        boolean exceptionReviewRequired;
        String exceptionReason;
        int heuristicScore;
        List<String> heuristicReasons = new ArrayList<>();
        List<String> registerFocus = new ArrayList<>();
        List<String> flagFocus = new ArrayList<>();
        List<String> contextBefore = new ArrayList<>();
        List<String> branchSnippet = new ArrayList<>();
        List<String> contextAfter = new ArrayList<>();
    }

    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 5) {
            println("Usage: ExportBranchAnalysis <markdown-path> <evidence-json-path> <probe-name> <pdb-source> <pattern-1> [pattern-2] [...]");
            return;
        }

        File markdownFile = new File(args[0]);
        File evidenceFile = new File(args[1]);
        String probeName = args[2];
        String pdbSource = args[3];
        List<String> patterns = Arrays.asList(Arrays.copyOfRange(args, 4, args.length));

        markdownFile.getParentFile().mkdirs();
        evidenceFile.getParentFile().mkdirs();

        Listing listing = currentProgram.getListing();
        ReferenceManager refManager = currentProgram.getReferenceManager();
        String timestamp = Instant.now().toString();
        List<MatchEvidence> matches = new ArrayList<>();
        boolean pdbLoaded = false;

        try (PrintWriter writer = new PrintWriter(new OutputStreamWriter(new FileOutputStream(markdownFile), StandardCharsets.UTF_8))) {
            writer.printf("# Ghidra Branch Review%n%n");
            writer.printf("- Program: `%s`%n", currentProgram.getName());
            writer.printf("- Probe: `%s`%n", probeName);
            writer.printf("- Timestamp: `%s`%n", timestamp);
            writer.printf("- PDB source: `%s`%n", escapeInline(pdbSource));
            writer.printf("- Patterns: `%s`%n%n", String.join("`, `", patterns));

            for (String rawPattern : patterns) {
                String pattern = rawPattern == null ? "" : rawPattern.trim();
                if (pattern.isEmpty()) {
                    continue;
                }

                writer.printf("## `%s`%n%n", escapeInline(pattern));
                int emittedStrings = 0;
                boolean found = false;

                DataIterator dataIterator = listing.getDefinedData(true);
                while (dataIterator.hasNext()) {
                    Data data = dataIterator.next();
                    if (!data.hasStringValue()) {
                        continue;
                    }

                    String text = extractText(data);
                    if (text.isEmpty()) {
                        continue;
                    }

                    if (!text.toLowerCase(Locale.ROOT).contains(pattern.toLowerCase(Locale.ROOT))) {
                        continue;
                    }

                    found = true;
                    emittedStrings++;
                    writer.printf("### String @ `%s`%n%n", data.getAddress());
                    writer.printf("`%s`%n%n", escapeInline(text));

                    ReferenceIterator refs = refManager.getReferencesTo(data.getAddress());
                    int emittedRefs = 0;
                    while (refs.hasNext() && emittedRefs < MAX_REFERENCES_PER_STRING) {
                        Reference ref = refs.next();
                        MatchEvidence evidence = buildEvidence(listing, ref.getFromAddress(), pattern);
                        matches.add(evidence);
                        if ("symbolized_branch".equals(evidence.functionConfidence)) {
                            pdbLoaded = true;
                        }

                        writer.printf("- Function: `%s`%n", escapeInline(evidence.functionName));
                        writer.printf("- Function source: `%s`%n", evidence.functionSource);
                        writer.printf("- Function confidence: `%s`%n", evidence.functionConfidence);
                        writer.printf("- Address: `%s`%n", evidence.address);
                        writer.printf("- Register focus: `%s`%n", String.join("`, `", evidence.registerFocus));
                        writer.printf("- Flag focus: `%s`%n", String.join("`, `", evidence.flagFocus));
                        writer.printf("- Compare: `%s`%n", escapeInline(evidence.compareCondition));
                        writer.printf("- Jump: `%s`%n", escapeInline(evidence.jumpCondition));
                        writer.printf("- Value mapping: `%s`%n", escapeInline(evidence.valueMap));
                        writer.printf("- Branch effect: `%s`%n", escapeInline(evidence.branchEffect));
                        writer.printf("- Stack note: `%s`%n", escapeInline(evidence.stackSummary));
                        writer.printf("- Exception gate: `%s`%n", escapeInline(evidence.exceptionReason));
                        writer.printf("- Heuristic score: `%d`%n", evidence.heuristicScore);
                        writer.printf("- Heuristic reasons: `%s`%n", String.join(" | ", evidence.heuristicReasons));
                        writer.printf("- Effect: %s%n", escapeInline(evidence.effectSummary));
                        writer.printf("- Unclear: `%s`%n%n", evidence.unclear);
                        writer.printf("```asm%n%s%n```%n%n", renderContext(evidence));
                        emittedRefs++;
                    }

                    if (emittedStrings >= MAX_STRINGS_PER_PATTERN) {
                        writer.printf("_Stopped after %d matching strings to keep the export bounded._%n%n", MAX_STRINGS_PER_PATTERN);
                        break;
                    }
                }

                if (!found) {
                    writer.printf("_No matching strings found._%n%n");
                }
            }
        }

        writeEvidenceJson(evidenceFile, currentProgram.getName(), probeName, timestamp, pdbSource, pdbLoaded, matches);
    }

    private MatchEvidence buildEvidence(Listing listing, Address referenceAddress, String pattern) {
        MatchEvidence evidence = new MatchEvidence();
        evidence.pattern = pattern;
        evidence.address = referenceAddress.toString();

        Instruction anchor = instructionAtOrBefore(listing, referenceAddress);
        Address anchorAddress = anchor == null ? referenceAddress : anchor.getAddress();
        Function function = listing.getFunctionContaining(anchorAddress);

        if (function == null) {
            evidence.functionName = "<no function>";
            evidence.functionSource = "unresolved";
        }
        else {
            evidence.functionName = function.getName();
            evidence.functionSource = function.getName().startsWith("FUN_") ? "auto-analysis-fallback" : "pdb-symbol";
        }

        evidence.contextBefore.addAll(collectPreviousInstructions(listing, anchor, CONTEXT_LINES));
        evidence.branchSnippet.addAll(collectBranchSnippet(listing, anchor, CONTEXT_LINES));
        evidence.contextAfter.addAll(collectNextInstructions(listing, anchor, CONTEXT_LINES));

        List<String> allLines = new ArrayList<>();
        allLines.addAll(evidence.contextBefore);
        allLines.addAll(evidence.branchSnippet);
        allLines.addAll(evidence.contextAfter);

        evidence.registerFocus.addAll(collectRegisters(allLines));
        evidence.flagFocus.addAll(inferFlags(evidence.branchSnippet));
        evidence.compareCondition = firstCompareCondition(evidence.branchSnippet);
        evidence.jumpCondition = firstJumpCondition(evidence.branchSnippet);
        evidence.valueMap = inferValueMap(evidence.branchSnippet);
        evidence.stackSummary = inferStackSummary(allLines);
        evidence.exceptionReviewRequired = isExceptionAdjacent(allLines);
        evidence.exceptionReason = evidence.exceptionReviewRequired
            ? "trap-or-fault-adjacent instructions present; control-flow may be misleading."
            : "none";
        evidence.functionConfidence = inferFunctionConfidence(evidence);
        evidence.branchEffect = inferBranchEffect(evidence);
        evidence.unclear =
            !"symbolized_branch".equals(evidence.functionConfidence)
                || "unclear".equals(evidence.valueMap)
                || evidence.exceptionReviewRequired;
        evidence.effectSummary = inferEffectSummary(evidence);
        evidence.heuristicScore = scoreEvidence(evidence);
        evidence.heuristicReasons.addAll(scoreReasons(evidence));
        if (evidence.registerFocus.isEmpty()) {
            evidence.registerFocus.add("unclear");
        }
        if (evidence.flagFocus.isEmpty()) {
            evidence.flagFocus.add("unclear");
        }
        return evidence;
    }

    private Instruction instructionAtOrBefore(Listing listing, Address address) {
        Instruction instruction = listing.getInstructionAt(address);
        if (instruction != null) {
            return instruction;
        }
        return listing.getInstructionBefore(address);
    }

    private List<String> collectPreviousInstructions(Listing listing, Instruction anchor, int count) {
        List<String> reversed = new ArrayList<>();
        Instruction current = anchor;
        for (int i = 0; i < count; i++) {
            if (current == null) {
                break;
            }
            current = listing.getInstructionBefore(current.getAddress());
            if (current == null) {
                break;
            }
            reversed.add(formatInstruction(current));
        }

        List<String> ordered = new ArrayList<>();
        for (int i = reversed.size() - 1; i >= 0; i--) {
            ordered.add(reversed.get(i));
        }
        return ordered;
    }

    private List<String> collectNextInstructions(Listing listing, Instruction anchor, int count) {
        List<String> lines = new ArrayList<>();
        Instruction current = anchor;
        for (int i = 0; i < count; i++) {
            if (current == null) {
                break;
            }
            current = listing.getInstructionAfter(current.getAddress());
            if (current == null) {
                break;
            }
            lines.add(formatInstruction(current));
        }
        return lines;
    }

    private List<String> collectBranchSnippet(Listing listing, Instruction anchor, int count) {
        Set<String> lines = new LinkedHashSet<>();
        if (anchor != null) {
            lines.add(formatInstruction(anchor));
        }

        Instruction current = anchor;
        for (int i = 0; i < count; i++) {
            if (current == null) {
                break;
            }
            current = listing.getInstructionAfter(current.getAddress());
            if (current == null) {
                break;
            }
            if (looksLikeBranch(current)) {
                lines.add(formatInstruction(current));
            }
        }

        current = anchor;
        for (int i = 0; i < count; i++) {
            if (current == null) {
                break;
            }
            current = listing.getInstructionBefore(current.getAddress());
            if (current == null) {
                break;
            }
            if (looksLikeBranch(current)) {
                lines.add(formatInstruction(current));
            }
        }
        return new ArrayList<>(lines);
    }

    private boolean looksLikeBranch(Instruction instruction) {
        String mnemonic = extractMnemonic(instruction.toString());
        return mnemonic.startsWith("J")
            || mnemonic.startsWith("CMP")
            || mnemonic.startsWith("TEST")
            || mnemonic.startsWith("CMOV");
    }

    private String extractMnemonic(String line) {
        if (line == null) {
            return "";
        }
        String trimmed = line.trim();
        int split = trimmed.indexOf("  ");
        String body = split >= 0 ? trimmed.substring(split).trim() : trimmed;
        int space = body.indexOf(' ');
        if (space < 0) {
            return body.toUpperCase(Locale.ROOT);
        }
        return body.substring(0, space).toUpperCase(Locale.ROOT);
    }

    private List<String> collectRegisters(List<String> lines) {
        Set<String> registers = new LinkedHashSet<>();
        for (String line : lines) {
            String normalized =
                " " + line.toUpperCase(Locale.ROOT)
                    .replace("[", " ")
                    .replace("]", " ")
                    .replace(",", " ")
                    .replace("+", " ")
                    .replace("-", " ")
                    .replace(":", " ")
                    + " ";
            for (String register : REGISTER_NAMES) {
                if (normalized.contains(" " + register + " ")) {
                    registers.add(register);
                }
            }
        }
        return new ArrayList<>(registers);
    }

    private List<String> inferFlags(List<String> lines) {
        Set<String> flags = new LinkedHashSet<>();
        for (String line : lines) {
            String mnemonic = extractMnemonic(line);
            if (mnemonic.startsWith("TEST") || mnemonic.startsWith("JE") || mnemonic.startsWith("JNE") || mnemonic.startsWith("JZ") || mnemonic.startsWith("JNZ")) {
                flags.add("ZF");
            }
            if (mnemonic.startsWith("JA") || mnemonic.startsWith("JB") || mnemonic.startsWith("JC") || mnemonic.startsWith("JAE") || mnemonic.startsWith("JBE")) {
                flags.add("CF");
                flags.add("ZF");
            }
            if (mnemonic.startsWith("JG") || mnemonic.startsWith("JL") || mnemonic.startsWith("JGE") || mnemonic.startsWith("JLE")) {
                flags.add("SF");
                flags.add("OF");
                flags.add("ZF");
            }
            if (mnemonic.startsWith("JO") || mnemonic.startsWith("JNO")) {
                flags.add("OF");
            }
            if (mnemonic.startsWith("JS") || mnemonic.startsWith("JNS")) {
                flags.add("SF");
            }
            if (mnemonic.startsWith("CMP")) {
                flags.add("ZF");
                flags.add("CF");
                flags.add("SF");
                flags.add("OF");
            }
        }
        return new ArrayList<>(flags);
    }

    private String firstCompareCondition(List<String> lines) {
        for (String line : lines) {
            String mnemonic = extractMnemonic(line);
            if (mnemonic.startsWith("CMP") || mnemonic.startsWith("TEST")) {
                return line;
            }
        }
        return "unclear";
    }

    private String firstJumpCondition(List<String> lines) {
        for (String line : lines) {
            String mnemonic = extractMnemonic(line);
            if (mnemonic.startsWith("J")) {
                return line;
            }
        }
        return "unclear";
    }

    private String inferStackSummary(List<String> lines) {
        for (String line : lines) {
            String upper = line.toUpperCase(Locale.ROOT);
            if (upper.contains("[RSP") || upper.contains("[RBP") || upper.contains(" RSP") || upper.contains(" RBP")) {
                return "stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.";
            }
        }
        return "no obvious stack-relative access in the bounded context.";
    }

    private boolean isExceptionAdjacent(List<String> lines) {
        for (String line : lines) {
            String upper = line.toUpperCase(Locale.ROOT);
            if (upper.contains(" INT1") || upper.contains(" INT3") || upper.contains(" UD2") || upper.contains(" HLT") || upper.contains(" ICEBP")) {
                return true;
            }
        }
        return false;
    }

    private String inferFunctionConfidence(MatchEvidence evidence) {
        if (
            "pdb-symbol".equals(evidence.functionSource)
                && !"unclear".equals(evidence.compareCondition)
                && !"unclear".equals(evidence.jumpCondition)
                && !evidence.exceptionReviewRequired
        ) {
            return "symbolized_branch";
        }
        return "string_only_review";
    }

    private String inferValueMap(List<String> branchSnippet) {
        String blob = String.join(" ", branchSnippet).toLowerCase(Locale.ROOT);
        if (blob.contains(",0") || blob.contains(" 0x0") || blob.contains(" 00h")) {
            return "value=0 participates in this conditional block; opposite branch still needs explicit review.";
        }
        if (blob.contains(",1") || blob.contains(" 0x1") || blob.contains(" 01h")) {
            return "value=1 participates in this conditional block; opposite branch still needs explicit review.";
        }
        return "unclear";
    }

    private String inferBranchEffect(MatchEvidence evidence) {
        if (evidence.exceptionReviewRequired) {
            return "trap/fault-adjacent block detected; control-flow may be misleading.";
        }
        if (!"unclear".equals(evidence.compareCondition) && !"unclear".equals(evidence.jumpCondition)) {
            return "compare + conditional jump recovered in bounded context.";
        }
        if (!"unclear".equals(evidence.compareCondition)) {
            return "comparison recovered, but nearby jump condition is still unclear.";
        }
        if (!"unclear".equals(evidence.jumpCondition)) {
            return "jump recovered, but the compare/test anchor is still unclear.";
        }
        return "unclear";
    }

    private String inferEffectSummary(MatchEvidence evidence) {
        if ("symbolized_branch".equals(evidence.functionConfidence) && !"unclear".equals(evidence.valueMap)) {
            return "PDB-backed function identity, compare/jump structure, and a bounded value map are present.";
        }
        if (evidence.exceptionReviewRequired) {
            return "unclear - exception-adjacent control flow needs manual review before any semantic claim.";
        }
        return "unclear - keep this as review-only until a PDB-backed branch mapping is available.";
    }

    private int scoreEvidence(MatchEvidence evidence) {
        int score = 0;
        if ("pdb-symbol".equals(evidence.functionSource)) {
            score += 30;
        }
        if ("symbolized_branch".equals(evidence.functionConfidence)) {
            score += 20;
        }
        if (!"unclear".equals(evidence.compareCondition)) {
            score += 15;
        }
        if (!"unclear".equals(evidence.jumpCondition)) {
            score += 15;
        }
        if (!"unclear".equals(evidence.valueMap)) {
            score += 10;
        }
        if (evidence.stackSummary.startsWith("stack-relative")) {
            score += 5;
        }
        if (evidence.exceptionReviewRequired) {
            score -= 25;
        }
        if ("unresolved".equals(evidence.functionSource)) {
            score -= 15;
        }
        if (score < 0) {
            return 0;
        }
        if (score > 100) {
            return 100;
        }
        return score;
    }

    private List<String> scoreReasons(MatchEvidence evidence) {
        List<String> reasons = new ArrayList<>();
        if ("pdb-symbol".equals(evidence.functionSource)) {
            reasons.add("pdb-symbol present");
        }
        if ("symbolized_branch".equals(evidence.functionConfidence)) {
            reasons.add("compare+jump survived bounded symbolized review");
        }
        if (!"unclear".equals(evidence.compareCondition)) {
            reasons.add("compare/test anchor found");
        }
        if (!"unclear".equals(evidence.jumpCondition)) {
            reasons.add("conditional jump found");
        }
        if (!"unclear".equals(evidence.valueMap)) {
            reasons.add("value immediate found in bounded block");
        }
        if (evidence.stackSummary.startsWith("stack-relative")) {
            reasons.add("stack-relative context detected");
        }
        if (evidence.exceptionReviewRequired) {
            reasons.add("exception/trap gate forced review-only");
        }
        if (reasons.isEmpty()) {
            reasons.add("string match only");
        }
        return reasons;
    }

    private String renderContext(MatchEvidence evidence) {
        List<String> lines = new ArrayList<>();
        lines.add("; context_before");
        lines.addAll(evidence.contextBefore);
        lines.add("; branch_snippet");
        lines.addAll(evidence.branchSnippet);
        lines.add("; context_after");
        lines.addAll(evidence.contextAfter);
        return String.join(System.lineSeparator(), lines);
    }

    private String formatInstruction(Instruction instruction) {
        return instruction.getAddress() + "  " + instruction.toString();
    }

    private String extractText(Data data) {
        Object value = data.getValue();
        if (value == null) {
            return "";
        }
        return value.toString().replace("\u0000", "").trim();
    }

    private String escapeInline(String value) {
        return value == null ? "" : value.replace("`", "\\`");
    }

    private void writeEvidenceJson(
        File evidenceFile,
        String binary,
        String probeName,
        String timestamp,
        String pdbSource,
        boolean pdbLoaded,
        List<MatchEvidence> matches
    ) throws Exception {
        try (PrintWriter writer = new PrintWriter(new OutputStreamWriter(new FileOutputStream(evidenceFile), StandardCharsets.UTF_8))) {
            writer.println("{");
            writer.printf("  \"binary\": \"%s\",%n", escapeJson(binary));
            writer.printf("  \"probe\": \"%s\",%n", escapeJson(probeName));
            writer.printf("  \"timestamp\": \"%s\",%n", escapeJson(timestamp));
            writer.printf("  \"pdb_source\": \"%s\",%n", escapeJson(pdbSource));
            writer.printf("  \"pdb_loaded\": %s,%n", pdbLoaded);
            writer.println("  \"matches\": [");
            for (int i = 0; i < matches.size(); i++) {
                MatchEvidence match = matches.get(i);
                writer.println("    {");
                writer.printf("      \"pattern\": \"%s\",%n", escapeJson(match.pattern));
                writer.printf("      \"address\": \"%s\",%n", escapeJson(match.address));
                writer.printf("      \"function_name\": \"%s\",%n", escapeJson(match.functionName));
                writer.printf("      \"function_source\": \"%s\",%n", escapeJson(match.functionSource));
                writer.printf("      \"function_confidence\": \"%s\",%n", escapeJson(match.functionConfidence));
                writer.printf("      \"unclear\": %s,%n", match.unclear);
                writer.printf("      \"value_map\": \"%s\",%n", escapeJson(match.valueMap));
                writer.printf("      \"compare_condition\": \"%s\",%n", escapeJson(match.compareCondition));
                writer.printf("      \"jump_condition\": \"%s\",%n", escapeJson(match.jumpCondition));
                writer.printf("      \"branch_effect\": \"%s\",%n", escapeJson(match.branchEffect));
                writer.printf("      \"stack_summary\": \"%s\",%n", escapeJson(match.stackSummary));
                writer.printf("      \"exception_review_required\": %s,%n", match.exceptionReviewRequired);
                writer.printf("      \"exception_reason\": \"%s\",%n", escapeJson(match.exceptionReason));
                writer.printf("      \"heuristic_score\": %d,%n", match.heuristicScore);
                writer.printf("      \"effect_summary\": \"%s\",%n", escapeJson(match.effectSummary));
                writeStringArray(writer, "heuristic_reasons", match.heuristicReasons, "      ");
                writer.println(",");
                writeStringArray(writer, "register_focus", match.registerFocus, "      ");
                writer.println(",");
                writeStringArray(writer, "flag_focus", match.flagFocus, "      ");
                writer.println(",");
                writeStringArray(writer, "context_before", match.contextBefore, "      ");
                writer.println(",");
                writeStringArray(writer, "branch_snippet", match.branchSnippet, "      ");
                writer.println(",");
                writeStringArray(writer, "context_after", match.contextAfter, "      ");
                writer.println();
                writer.print("    }");
                if (i + 1 < matches.size()) {
                    writer.print(",");
                }
                writer.println();
            }
            writer.println("  ]");
            writer.println("}");
        }
    }

    private void writeStringArray(PrintWriter writer, String name, List<String> values, String indent) {
        writer.printf("%s\"%s\": [", indent, name);
        if (values.isEmpty()) {
            writer.print("]");
            return;
        }
        writer.println();
        for (int i = 0; i < values.size(); i++) {
            writer.printf("%s  \"%s\"", indent, escapeJson(values.get(i)));
            if (i + 1 < values.size()) {
                writer.print(",");
            }
            writer.println();
        }
        writer.printf("%s]", indent);
    }

    private String escapeJson(String value) {
        if (value == null) {
            return "";
        }
        return value
            .replace("\\", "\\\\")
            .replace("\"", "\\\"")
            .replace("\r", "\\r")
            .replace("\n", "\\n");
    }
}
