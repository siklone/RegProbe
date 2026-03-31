import ghidra.app.script.GhidraScript;

public class SetPdbSymbolRepository extends GhidraScript {
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

        setAnalysisOption(currentProgram, "PDB.Symbol Repository Path", repositoryPath);
        println("Configured PDB.Symbol Repository Path = " + repositoryPath);
    }
}
