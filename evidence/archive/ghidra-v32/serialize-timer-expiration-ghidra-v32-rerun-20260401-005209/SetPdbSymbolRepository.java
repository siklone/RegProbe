import java.io.File;

import ghidra.app.plugin.core.analysis.PdbUniversalAnalyzer;
import ghidra.app.script.GhidraScript;

public class SetPdbSymbolRepository extends GhidraScript {
    private File findFirstPdb(File root) {
        if (root == null || !root.exists()) {
            return null;
        }

        if (root.isFile()) {
            return root.getName().toLowerCase().endsWith(".pdb") ? root : null;
        }

        File[] children = root.listFiles();
        if (children == null) {
            return null;
        }

        for (File child : children) {
            File match = findFirstPdb(child);
            if (match != null) {
                return match;
            }
        }

        return null;
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
        File pdbFile = findFirstPdb(repositoryRoot);

        setAnalysisOption(currentProgram, "PDB.Symbol Repository Path", repositoryPath);

        if (pdbFile == null) {
            printerr("No PDB file was found under " + repositoryPath);
            println("Configured PDB.Symbol Repository Path = " + repositoryPath);
            return;
        }

        PdbUniversalAnalyzer.setPdbFileOption(currentProgram, pdbFile);
        println("Configured PDB.Symbol Repository Path = " + repositoryPath);
        println("Configured explicit PDB file = " + pdbFile.getAbsolutePath());
    }
}
