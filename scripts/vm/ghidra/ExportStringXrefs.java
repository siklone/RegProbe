import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileOptions;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.DataIterator;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class ExportStringXrefs extends GhidraScript {

    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 2) {
            println("Usage: ExportStringXrefs <output-path> <pattern-1> [pattern-2] [...]");
            return;
        }

        File outputFile = new File(args[0]);
        outputFile.getParentFile().mkdirs();
        List<String> patterns = Arrays.asList(Arrays.copyOfRange(args, 1, args.length));

        try (PrintWriter writer = new PrintWriter(new OutputStreamWriter(new FileOutputStream(outputFile), StandardCharsets.UTF_8))) {
            writer.printf("# Ghidra String/Xref Export%n%n");
            writer.printf("- Program: `%s`%n", currentProgram.getExecutablePath());
            writer.printf("- Name: `%s`%n", currentProgram.getName());
            writer.printf("- Patterns: `%s`%n%n", String.join("`, `", patterns));

            DecompInterface decompiler = new DecompInterface();
            DecompileOptions options = new DecompileOptions();
            decompiler.setOptions(options);
            decompiler.toggleCCode(true);
            decompiler.toggleSyntaxTree(true);
            decompiler.setSimplificationStyle("decompile");
            decompiler.openProgram(currentProgram);

            Listing listing = currentProgram.getListing();
            ReferenceManager refManager = currentProgram.getReferenceManager();

            for (String rawPattern : patterns) {
                String pattern = rawPattern == null ? "" : rawPattern.trim();
                String normalizedPattern = pattern.toLowerCase(Locale.ROOT);
                writer.printf("## Pattern: `%s`%n%n", pattern);

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

                    if (!text.toLowerCase(Locale.ROOT).contains(normalizedPattern)) {
                        continue;
                    }

                    found = true;
                    writer.printf("### String @ `%s`%n%n", data.getAddress());
                    writer.printf("`%s`%n%n", escapeInlineCode(text));

                    ReferenceIterator refs = refManager.getReferencesTo(data.getAddress());
                    List<Reference> refList = new ArrayList<>();
                    Set<Address> functionEntries = new LinkedHashSet<>();
                    while (refs.hasNext()) {
                        Reference ref = refs.next();
                        refList.add(ref);
                        Function function = listing.getFunctionContaining(ref.getFromAddress());
                        if (function != null) {
                            functionEntries.add(function.getEntryPoint());
                        }
                    }

                    writer.printf("- Reference count: `%d`%n", refList.size());
                    if (refList.isEmpty()) {
                        writer.printf("- No direct references resolved by Ghidra%n%n");
                        continue;
                    }

                    writer.printf("- References:%n");
                    for (Reference ref : refList) {
                        Function function = listing.getFunctionContaining(ref.getFromAddress());
                        String functionLabel = function == null ? "<no function>" : function.getName();
                        writer.printf("  - `%s` in `%s`%n", ref.getFromAddress(), functionLabel);
                    }
                    writer.printf("%n");

                    for (Address entry : functionEntries) {
                        Function function = listing.getFunctionAt(entry);
                        if (function == null) {
                            continue;
                        }

                        writer.printf("#### Function `%s` @ `%s`%n%n", function.getName(), function.getEntryPoint());
                        writer.printf("```c%n%s%n```%n%n", decompileSnippet(decompiler, function));
                    }
                }

                if (!found) {
                    writer.printf("_No matching strings found for `%s`._%n%n", pattern);
                }
            }
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

    private String decompileSnippet(DecompInterface decompiler, Function function) {
        try {
            DecompileResults results = decompiler.decompileFunction(function, 90, monitor);
            if (!results.decompileCompleted() || results.getDecompiledFunction() == null) {
                return "// decompilation not available";
            }

            String c = results.getDecompiledFunction().getC();
            if (c == null || c.isBlank()) {
                return "// empty decompilation output";
            }

            String[] lines = c.replace("\r", "").split("\n");
            int maxLines = Math.min(lines.length, 80);
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
}
