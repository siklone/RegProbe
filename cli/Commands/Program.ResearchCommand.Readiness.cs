using System.Collections.Generic;
using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchReadinessCommand()
    {
        var command = new Command("readiness", "Check whether the repo is ready for a manual app retest across cards, evidence, rollback coverage, and KVM smoke artifacts");
        var emitJsonOption = CreateOption<bool>("--json", "Emit the readiness report as JSON");
        command.AddOption(emitJsonOption);

        command.SetHandler(context =>
        {
            var args = new List<string>();
            if (context.ParseResult.GetValueForOption(emitJsonOption))
            {
                args.Add("--json");
            }

            context.ExitCode = RunResearchPythonScript("check_app_retest_readiness.py", args);
        });

        return command;
    }
}
