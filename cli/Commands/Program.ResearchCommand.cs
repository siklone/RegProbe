using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchCommand()
    {
        var researchCommand = new Command("research", "Inspect research-derived promotion state and automation helpers");

        researchCommand.AddCommand(CreateResearchScoreCandidateCommand());
        researchCommand.AddCommand(CreateResearchEvaluateGateCommand());
        researchCommand.AddCommand(CreateResearchShowBlockedCommand());
        researchCommand.AddCommand(CreateResearchListBlockedCommand());
        researchCommand.AddCommand(CreateResearchShowStaleCommand());
        researchCommand.AddCommand(CreateResearchShowRevalidationPendingCommand());
        researchCommand.AddCommand(CreateResearchInspectCommand());
        researchCommand.AddCommand(CreateResearchQaPlanCommand());
        researchCommand.AddCommand(CreateResearchQaBatchCommand());
        researchCommand.AddCommand(CreateResearchReadinessCommand());
        researchCommand.AddCommand(CreateResearchValidateBatchCommand());
        researchCommand.AddCommand(CreateResearchGenerateRegressionPackCommand());
        researchCommand.AddCommand(CreateResearchNormalizeRegistryTraceCommand());
        researchCommand.AddCommand(CreateResearchValidateJsonTweaksCommand());

        return researchCommand;
    }
}
