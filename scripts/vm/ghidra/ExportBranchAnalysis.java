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

    private static final class MatchEvidence {
        String pattern;
        String address;
        String functionName;
        String functionSource;
        boolean unclear;
        String valueMap;
        String effectSummary;
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
                        if ("pdb-symbol".equals(evidence.functionSource)) {
                            pdbLoaded = true;
                        }

                        writer.printf("- Function: `%s`%n", escapeInline(evidence.functionName));
                        writer.printf("- Function source: `%s`%n", evidence.functionSource);
                        writer.printf("- Address: `%s`%n", evidence.address);
                        writer.printf("- Value mapping: `%s`%n", escapeInline(evidence.valueMap));
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

        List<String> before = collectPreviousInstructions(listing, anchor, CONTEXT_LINES);
        List<String> current = collectBranchSnippet(listing, anchor, CONTEXT_LINES);
        List<String> after = collectNextInstructions(listing, anchor, CONTEXT_LINES);
        evidence.contextBefore.addAll(before);
        evidence.branchSnippet.addAll(current);
        evidence.contextAfter.addAll(after);

        evidence.unclear = !"pdb-symbol".equals(evidence.functionSource) || evidence.branchSnippet.isEmpty();
        evidence.valueMap = inferValueMap(evidence.branchSnippet);
        evidence.effectSummary = evidence.unclear
            ? "unclear - keep this as review-only until a PDB-backed branch mapping is available."
            : "PDB-backed function context is present; use the bounded branch block for the value-to-effect interpretation.";
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
        String mnemonic = instruction.getMnemonicString().toUpperCase(Locale.ROOT);
        return mnemonic.startsWith("J") || mnemonic.startsWith("CMP") || mnemonic.startsWith("TEST") || mnemonic.startsWith("CMOV");
    }

    private String inferValueMap(List<String> branchSnippet) {
        String blob = String.join(" ", branchSnippet).toLowerCase(Locale.ROOT);
        if (blob.contains(",0") || blob.contains(" 0x0")) {
            return "value=0 participates in this conditional block; non-zero branch still needs explicit review.";
        }
        if (blob.contains(",1") || blob.contains(" 0x1")) {
            return "value=1 participates in this conditional block; opposite branch still needs explicit review.";
        }
        return "unclear";
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
                writer.printf("      \"unclear\": %s,%n", match.unclear);
                writer.printf("      \"value_map\": \"%s\",%n", escapeJson(match.valueMap));
                writer.printf("      \"effect_summary\": \"%s\",%n", escapeJson(match.effectSummary));
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
