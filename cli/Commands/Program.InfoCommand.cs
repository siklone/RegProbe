using System;
using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateInfoCommand()
    {
        var infoCommand = new Command("info", "Display system information");

        infoCommand.SetHandler(() =>
        {
            Console.WriteLine("=======================================");
            Console.WriteLine(" RegProbe - System Information");
            Console.WriteLine("=======================================");
            Console.WriteLine($"  OS:         {Environment.OSVersion}");
            Console.WriteLine($"  Machine:    {Environment.MachineName}");
            Console.WriteLine($"  User:       {Environment.UserName}");
            Console.WriteLine($"  Processors: {Environment.ProcessorCount}");
            Console.WriteLine($"  64-bit:     {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"  CLR:        {Environment.Version}");
            Console.WriteLine("=======================================");
        });

        return infoCommand;
    }
}
