using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateTweakCommand()
    {
        var tweakCommand = new Command("tweak", "Manage system tweaks");

        tweakCommand.AddCommand(CreateTweakListCommand());
        tweakCommand.AddCommand(CreateTweakApplyCommand());
        tweakCommand.AddCommand(CreateTweakRevertCommand());

        return tweakCommand;
    }
}
