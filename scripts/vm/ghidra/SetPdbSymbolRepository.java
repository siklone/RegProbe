import java.io.File;
import java.io.IOException;
import java.util.Locale;

import ghidra.app.plugin.core.analysis.PdbUniversalAnalyzer;
import ghidra.app.script.GhidraScript;

public class SetPdbSymbolRepository extends GhidraScript {
    private File findFirstPdbArtifact(File root) {
        if (root == null || !root.exists()) {
            return null;
        }

        if (root.isFile()) {
            String lowerName = root.getName().toLowerCase(Locale.ROOT);
            return lowerName.endsWith(".pdb") || lowerName.endsWith(".pd_") ? root : null;
        }

        File[] children = root.listFiles();
        if (children == null) {
            return null;
        }

        for (File child : children) {
            File match = findFirstPdbArtifact(child);
            if (match != null) {
                return match;
            }
        }

        return null;
    }

    private boolean isCompressedPdb(File file) {
        return file != null && file.getName().toLowerCase(Locale.ROOT).endsWith(".pd_");
    }

    private File expandCompressedPdb(File compressedPdb) throws IOException, InterruptedException {
        String expandedName = compressedPdb.getName().replaceAll("(?i)\\.pd_$", ".pdb");
        File expandedPdb = new File(compressedPdb.getParentFile(), expandedName);
        if (expandedPdb.exists()) {
            return expandedPdb;
        }

        Process process = new ProcessBuilder(
            "C:\\Windows\\System32\\expand.exe",
            compressedPdb.getAbsolutePath(),
            expandedPdb.getAbsolutePath()
        ).redirectErrorStream(true).start();

        String output = new String(process.getInputStream().readAllBytes());
        int exitCode = process.waitFor();
        if (exitCode != 0 || !expandedPdb.exists()) {
            throw new IOException("expand.exe failed for " + compressedPdb.getAbsolutePath() + ": " + output.trim());
        }

        return expandedPdb;
    }

    @Override
    protected void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 1) {
            println("Usage: SetPdbSymbolRepository <symbol-repository-path>");
            return;
        }

        String repositoryPath = args[0];
        if (repositoryPath == null || repositoryPath.trim().isEmpty()) {
            println("Symbol repository path was blank; leaving PDB analysis options unchanged.");
            return;
        }

        File repositoryRoot = new File(repositoryPath);
        File pdbArtifact = findFirstPdbArtifact(repositoryRoot);

        setAnalysisOption(currentProgram, "PDB.Symbol Repository Path", repositoryPath);

        if (pdbArtifact == null) {
            printerr("No PDB file was found under " + repositoryPath);
            println("Configured PDB.Symbol Repository Path = " + repositoryPath);
            return;
        }

        File pdbFile = isCompressedPdb(pdbArtifact) ? expandCompressedPdb(pdbArtifact) : pdbArtifact;
        PdbUniversalAnalyzer.setPdbFileOption(currentProgram, pdbFile);
        println("Configured PDB.Symbol Repository Path = " + repositoryPath);
        println("Configured explicit PDB file = " + pdbFile.getAbsolutePath());
    }
}
