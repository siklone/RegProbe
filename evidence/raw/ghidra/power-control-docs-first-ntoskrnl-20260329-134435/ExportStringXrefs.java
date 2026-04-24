import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileOptions;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.DataIterator;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class ExportStringXrefs extends GhidraScript {
    private static final int MAX_STRINGS_PER_PATTERN = 4;
    private static final int MAX_REFERENCES_PER_STRING = 12;
    private static final int DECOMPILE_TIMEOUT_SECONDS = 20;
    private static final int MAX_DECOMPILE_LINES = 60;
    private static final int MAX_DISASSEMBLY_LINES = 120;
    private static final long DISASSEMBLY_WINDOW_BYTES = 0xC8;
    private static final long INSTRUCTION_SEARCH_WINDOW_BYTES = 0x40;
    private static final int MAX_BYTE_WINDOW = 64;
    private static final Pattern PROBE_TIMESTAMP_PATTERN = Pattern.compile("^(.*)-\\d{8}-\\d{6}$");

    private static final class MatchEvidence {
        String address;
        String functionName = "<no function>";
        boolean forcedBoundary;
        boolean decompileSuccess;
        int outputLines;
        boolean ghidraNoFunctionFallback;
        boolean naturallyResolved;
        String outputKind = "none";
        String outputText = "// no output";
        String sectionTitle = "Match";
    }

    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 4) {
            println("Usage: ExportStringXrefs <markdown-path> <evidence-json-path> <probe-name> <pattern-1> [pattern-2] [...]");
            return;
        }

        File markdownFile = new File(args[0]);
        File evidenceFile = new File(args[1]);
        String probeName = args[2];
        List<String> patterns = Arrays.asList(Arrays.copyOfRange(args, 3, args.length));

        markdownFile.getParentFile().mkdirs();
        evidenceFile.getParentFile().mkdirs();

        LinkedHashMap<String, MatchEvidence> matchMap = new LinkedHashMap<>();
        String timestamp = Instant.now().toString();

        DecompInterface decompiler = new DecompInterface();
        DecompileOptions options = new DecompileOptions();
        decompiler.setOptions(options);
        decompiler.toggleCCode(true);
        decompiler.toggleSyntaxTree(true);
        decompiler.setSimplificationStyle("decompile");
        decompiler.openProgram(currentProgram);

        Listing listing = currentProgram.getListing();
        ReferenceManager refManager = currentProgram.getReferenceManager();

        try (PrintWriter writer = new PrintWriter(new OutputStreamWriter(new FileOutputStream(markdownFile), StandardCharsets.UTF_8))) {
            writer.printf("# Ghidra String/Xref Export%n%n");
            writer.printf("- Program: `%s`%n", currentProgram.getExecutablePath());
            writer.printf("- Name: `%s`%n", currentProgram.getName());
            writer.printf("- Probe: `%s`%n", probeName);
            writer.printf("- Timestamp: `%s`%n", timestamp);
            writer.printf("- Patterns: `%s`%n%n", String.join("`, `", patterns));

            writer.printf("## Pattern Summary%n%n");

            for (String rawPattern : patterns) {
                String pattern = rawPattern == null ? "" : rawPattern.trim();
                String normalizedPattern = pattern.toLowerCase(Locale.ROOT);
                writer.printf("### Pattern: `%s`%n%n", pattern);

                if (normalizedPattern.startsWith("addr:")) {
                    String addressText = pattern.substring(5).trim();
                    Address directAddress = currentProgram.getAddressFactory().getAddress(addressText);
                    if (directAddress == null) {
                        writer.printf("_Address seed `%s` could not be parsed._%n%n", pattern);
                        continue;
                    }

                    println("MATCH " + directAddress + " from address seed");
                    writer.printf("- Address seed: `%s`%n%n", directAddress);
                    matchMap.computeIfAbsent(directAddress.toString(), key -> resolveReferenceEvidence(listing, decompiler, directAddress));
                    continue;
                }

                boolean found = false;
                int matchedStrings = 0;
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

                    if (!text.toLowerCase(Locale.ROOT).contains(normalizedPattern)) {
                        continue;
                    }

                    found = true;
                    matchedStrings++;
                    writer.printf("#### String @ `%s`%n%n", data.getAddress());
                    writer.printf("`%s`%n%n", escapeInlineCode(text));

                    ReferenceIterator refs = refManager.getReferencesTo(data.getAddress());
                    List<Reference> refList = new ArrayList<>();
                    while (refs.hasNext()) {
                        refList.add(refs.next());
                    }

                    writer.printf("- Reference count: `%d`%n", refList.size());
                    if (refList.isEmpty()) {
                        writer.printf("- No direct references resolved by Ghidra%n%n");
                        continue;
                    }

                    writer.printf("- References:%n");
                    int emittedReferences = 0;
                    for (Reference ref : refList) {
                        if (emittedReferences >= MAX_REFERENCES_PER_STRING) {
                            break;
                        }

                        Address referenceAddress = ref.getFromAddress();
                        Function function = listing.getFunctionContaining(referenceAddress);
                        String functionLabel = function == null ? "<no function>" : function.getName();
                        writer.printf("  - `%s` in `%s`%n", referenceAddress, functionLabel);
                        matchMap.computeIfAbsent(referenceAddress.toString(), key -> resolveReferenceEvidence(listing, decompiler, referenceAddress));
                        emittedReferences++;
                    }
                    if (refList.size() > emittedReferences) {
                        writer.printf("  - `... %d more references omitted ...`%n", refList.size() - emittedReferences);
                    }
                    writer.printf("%n");

                    if (matchedStrings >= MAX_STRINGS_PER_PATTERN) {
                        writer.printf("_Stopped after `%d` matching strings for `%s` to keep the export bounded._%n%n",
                            MAX_STRINGS_PER_PATTERN,
                            pattern);
                        break;
                    }
                }

                if (!found) {
                    writer.printf("_No matching strings found for `%s`._%n%n", pattern);
                }
            }

            writer.printf("## Match Analysis%n%n");
            if (matchMap.isEmpty()) {
                writer.printf("_No references were resolved for this probe session._%n");
            }
            else {
                for (MatchEvidence match : matchMap.values()) {
                    writer.printf("## %s @ `%s`%n%n", match.sectionTitle, match.address);
                    writer.printf("- Function: `%s`%n", match.functionName);
                    writer.printf("- Forced boundary: `%s`%n", match.forcedBoundary);
                    writer.printf("- Naturally resolved: `%s`%n", match.naturallyResolved);
                    writer.printf("- Decompile success: `%s`%n", match.decompileSuccess);
                    writer.printf("- Output kind: `%s`%n", match.outputKind);
                    writer.printf("- Output lines: `%d`%n%n", match.outputLines);

                    String language = match.decompileSuccess ? "c" : "asm";
                    writer.printf("```%s%n%s%n```%n%n", language, match.outputText);
                }
            }
        }

        writeEvidenceJson(evidenceFile, currentProgram.getName(), probeName, timestamp, matchMap.values());
    }

    private MatchEvidence resolveReferenceEvidence(Listing listing, DecompInterface decompiler, Address referenceAddress) {
        MatchEvidence result = new MatchEvidence();
        result.address = referenceAddress.toString();

        Function function = listing.getFunctionContaining(referenceAddress);
        result.sectionTitle = function == null ? "Unresolved Block" : "Match";
        result.functionName = function == null ? "<no function>" : function.getName();

        Address workAddress = resolveInstructionAddress(listing, referenceAddress);
        if (function == null) {
            result.ghidraNoFunctionFallback = true;
            println("MATCH " + referenceAddress + " <no function> -> fallback");
            function = tryRecoverFunction(listing, workAddress, result);
        }
        else {
            result.naturallyResolved = true;
            println("MATCH " + referenceAddress + " in " + function.getName());
        }

        if (function != null) {
            result.functionName = function.getName();
            String decompile = decompileSnippet(decompiler, function);
            if (!decompile.startsWith("// decompilation")) {
                result.decompileSuccess = true;
                result.outputKind = "decompile";
                result.outputText = decompile;
                result.outputLines = countLines(decompile);
                return result;
            }
        }

        String disassembly = disassemblySnippet(listing, workAddress);
        result.decompileSuccess = false;
        result.outputKind = "disassembly";
        result.outputText = disassembly;
        result.outputLines = countLines(disassembly);
        return result;
    }

    private Address resolveInstructionAddress(Listing listing, Address address) {
        Address anchored = findNearbyInstructionAddress(listing, address, INSTRUCTION_SEARCH_WINDOW_BYTES);
        return anchored == null ? address : anchored;
    }

    private Function tryRecoverFunction(Listing listing, Address address, MatchEvidence result) {
        Function function = listing.getFunctionContaining(address);
        if (function != null) {
            result.naturallyResolved = true;
            return function;
        }

        disassemble(address);
        function = listing.getFunctionContaining(address);
        if (function != null) {
            result.naturallyResolved = true;
            return function;
        }

        try {
            Function forced = createFunction(address, null);
            if (forced != null) {
                result.forcedBoundary = true;
                return forced;
            }
        }
        catch (Exception ex) {
            println("MATCH " + address + " createFunction failed: " + ex.getMessage());
        }

        Address nearbyInstruction = findNearbyInstructionAddress(listing, address, INSTRUCTION_SEARCH_WINDOW_BYTES);
        if (nearbyInstruction != null && !nearbyInstruction.equals(address)) {
            function = listing.getFunctionContaining(nearbyInstruction);
            if (function != null) {
                result.naturallyResolved = true;
                return function;
            }

            try {
                Function forced = createFunction(nearbyInstruction, null);
                if (forced != null) {
                    result.forcedBoundary = true;
                    return forced;
                }
            }
            catch (Exception ex) {
                println("MATCH " + nearbyInstruction + " createFunction failed: " + ex.getMessage());
            }
        }

        function = listing.getFunctionContaining(address);
        if (function != null) {
            result.forcedBoundary = true;
            return function;
        }

        return null;
    }

    private String decompileSnippet(DecompInterface decompiler, Function function) {
        try {
            DecompileResults results = decompiler.decompileFunction(function, DECOMPILE_TIMEOUT_SECONDS, monitor);
            if (!results.decompileCompleted() || results.getDecompiledFunction() == null) {
                return "// decompilation not available";
            }

            String c = results.getDecompiledFunction().getC();
            if (c == null || c.isBlank()) {
                return "// empty decompilation output";
            }

            String[] lines = c.replace("\r", "").split("\n");
            int maxLines = Math.min(lines.length, MAX_DECOMPILE_LINES);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < maxLines; i++) {
                builder.append(lines[i]).append(System.lineSeparator());
            }
            if (lines.length > maxLines) {
                builder.append("// ... trimmed ...");
            }
            return builder.toString().trim();
        }
        catch (Exception ex) {
            return "// decompilation failed: " + ex.getMessage();
        }
    }

    private String disassemblySnippet(Listing listing, Address center) {
        Address start = safeAdjust(center, -DISASSEMBLY_WINDOW_BYTES);
        Address end = safeAdjust(center, DISASSEMBLY_WINDOW_BYTES);
        InstructionIterator iterator = listing.getInstructions(start, true);
        StringBuilder builder = new StringBuilder();
        int emitted = 0;
        while (iterator.hasNext()) {
            Instruction instruction = iterator.next();
            if (instruction.getAddress().compareTo(end) > 0) {
                break;
            }

            builder.append(instruction.getAddress())
                .append(": ")
                .append(instruction.toString())
                .append(System.lineSeparator());
            emitted++;
            if (emitted >= MAX_DISASSEMBLY_LINES) {
                builder.append("// ... trimmed ...");
                break;
            }
        }

        if (emitted == 0) {
            Address anchored = findNearbyInstructionAddress(listing, center, INSTRUCTION_SEARCH_WINDOW_BYTES);
            if (anchored != null && !anchored.equals(center)) {
                return disassemblySnippet(listing, anchored);
            }
            return byteSnippet(center);
        }

        return builder.toString().trim();
    }

    private Address findNearbyInstructionAddress(Listing listing, Address center, long maxDistance) {
        for (long offset = 0; offset <= maxDistance; offset++) {
            for (Address candidate : candidateAddresses(center, offset)) {
                Instruction instruction = listing.getInstructionContaining(candidate);
                if (instruction != null) {
                    return instruction.getAddress();
                }

                instruction = listing.getInstructionAt(candidate);
                if (instruction != null) {
                    return instruction.getAddress();
                }

                try {
                    disassemble(candidate);
                }
                catch (Exception ex) {
                }

                instruction = listing.getInstructionContaining(candidate);
                if (instruction != null) {
                    return instruction.getAddress();
                }

                instruction = listing.getInstructionAt(candidate);
                if (instruction != null) {
                    return instruction.getAddress();
                }
            }
        }

        return null;
    }

    private List<Address> candidateAddresses(Address center, long offset) {
        List<Address> addresses = new ArrayList<>();
        addresses.add(center);
        if (offset == 0) {
            return addresses;
        }

        try {
            addresses.add(center.subtractNoWrap(offset));
        }
        catch (Exception ex) {
        }

        try {
            addresses.add(center.addNoWrap(offset));
        }
        catch (Exception ex) {
        }

        return addresses;
    }

    private String byteSnippet(Address center) {
        Memory memory = currentProgram.getMemory();
        Address start = safeAdjust(center, -32);
        byte[] bytes = new byte[MAX_BYTE_WINDOW];
        int read;
        try {
            read = memory.getBytes(start, bytes);
        }
        catch (Exception ex) {
            return "// no disassembly or bytes available: " + ex.getMessage();
        }

        if (read <= 0) {
            return "// no disassembly or bytes available";
        }

        StringBuilder builder = new StringBuilder();
        builder.append("// raw bytes fallback").append(System.lineSeparator());
        builder.append(start).append(": ");
        for (int i = 0; i < read; i++) {
            builder.append(String.format("%02X", bytes[i] & 0xff));
            if (i + 1 < read) {
                builder.append(' ');
            }
        }
        return builder.toString().trim();
    }

    private Address safeAdjust(Address address, long delta) {
        try {
            return delta < 0 ? address.subtractNoWrap(-delta) : address.addNoWrap(delta);
        }
        catch (Exception ex) {
            Address min = address.getAddressSpace().getMinAddress();
            Address max = address.getAddressSpace().getMaxAddress();
            return delta < 0 ? min : max;
        }
    }

    private static String extractText(Data data) {
        Object value = data.getValue();
        if (value != null) {
            return value.toString().replace("\r", " ").replace("\n", " ").trim();
        }

        String fallback = data.getDefaultValueRepresentation();
        if (fallback == null) {
            return "";
        }

        return fallback.replace("\r", " ").replace("\n", " ").trim();
    }

    private static String escapeInlineCode(String value) {
        return value.replace("`", "\\`");
    }

    private static int countLines(String text) {
        if (text == null || text.isBlank()) {
            return 0;
        }
        return text.replace("\r", "").split("\n").length;
    }

    private void writeEvidenceJson(File outputFile, String binaryName, String probeName, String timestamp, Iterable<MatchEvidence> matches) throws Exception {
        List<MatchEvidence> materialized = new ArrayList<>();
        boolean fallbackSeen = false;
        for (MatchEvidence match : matches) {
            materialized.add(match);
            if (match.ghidraNoFunctionFallback) {
                fallbackSeen = true;
            }
        }

        try (PrintWriter writer = new PrintWriter(new OutputStreamWriter(new FileOutputStream(outputFile), StandardCharsets.UTF_8))) {
            writer.println("{");
            writer.println("  \"binary\": " + jsonString(binaryName) + ",");
            writer.println("  \"probe\": " + jsonString(probeName) + ",");
            writer.println("  \"timestamp\": " + jsonString(timestamp) + ",");
            writer.println("  \"ghidra_no_function_fallback\": " + fallbackSeen + ",");
            writer.println("  \"matches\": [");
            for (int i = 0; i < materialized.size(); i++) {
                MatchEvidence match = materialized.get(i);
                writer.println("    {");
                writer.println("      \"address\": " + jsonString(match.address) + ",");
                writer.println("      \"function\": " + jsonString(match.functionName) + ",");
                writer.println("      \"forced_boundary\": " + match.forcedBoundary + ",");
                writer.println("      \"decompile_success\": " + match.decompileSuccess + ",");
                writer.println("      \"output_lines\": " + match.outputLines + ",");
                writer.println("      \"naturally_resolved\": " + match.naturallyResolved + ",");
                writer.println("      \"output_kind\": " + jsonString(match.outputKind));
                writer.println("    }" + (i + 1 == materialized.size() ? "" : ","));
            }
            writer.println("  ]");
            writer.println("}");
        }
    }

    private static String jsonString(String value) {
        if (value == null) {
            return "null";
        }

        StringBuilder builder = new StringBuilder("\"");
        for (char ch : value.toCharArray()) {
            switch (ch) {
                case '\\':
                    builder.append("\\\\");
                    break;
                case '"':
                    builder.append("\\\"");
                    break;
                case '\n':
                    builder.append("\\n");
                    break;
                case '\r':
                    builder.append("\\r");
                    break;
                case '\t':
                    builder.append("\\t");
                    break;
                default:
                    if (ch < 0x20) {
                        builder.append(String.format("\\u%04x", (int) ch));
                    }
                    else {
                        builder.append(ch);
                    }
            }
        }
        builder.append("\"");
        return builder.toString();
    }
}
