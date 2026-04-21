using System;
using System.CommandLine;
using System.Linq;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateDnsCommand()
    {
        var dnsCommand = new Command("dns", "Manage DNS settings");

        var listCommand = new Command("list", "List DNS providers");
        listCommand.SetHandler(() =>
        {
            Console.WriteLine("Available DNS providers:");
            foreach (var provider in DnsService.GetProviders())
            {
                var secondary = string.IsNullOrWhiteSpace(provider.SecondaryDns) ? "" : $" / {provider.SecondaryDns}";
                Console.WriteLine($"  {provider.Name.ToLowerInvariant()}  {provider.PrimaryDns}{secondary}  ({provider.Description})");
            }
        });
        dnsCommand.AddCommand(listCommand);

        var setCommand = new Command("set", "Set DNS provider (default: dry-run)");
        var providerArg = CreateArgument<string>("provider", "DNS provider name");
        var applyOption = CreateOption<bool>("--apply", "Actually apply changes (default: dry-run)");
        var flushOption = CreateOption<bool>("--flush", "Flush DNS cache after applying");
        setCommand.AddArgument(providerArg);
        setCommand.AddOption(applyOption);
        setCommand.AddOption(flushOption);
        setCommand.SetHandler(async context =>
        {
            var provider = NormalizeCliText(context.ParseResult.GetValueForArgument(providerArg));
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var flush = context.ParseResult.GetValueForOption(flushOption);
            var providerValidationError = ValidateRequiredCliText(provider, "provider");
            if (!string.IsNullOrWhiteSpace(providerValidationError))
            {
                Console.WriteLine(providerValidationError);
                context.ExitCode = 1;
                return;
            }

            var validationError = ValidateDnsSetOptions(apply, flush);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                Console.WriteLine(validationError);
                context.ExitCode = 1;
                return;
            }

            var service = new DnsService();
            var providers = DnsService.GetProviders();
            var providerLookupError = ValidateKnownDnsProvider(provider, providers);
            if (!string.IsNullOrWhiteSpace(providerLookupError))
            {
                Console.WriteLine(providerLookupError);
                context.ExitCode = 1;
                return;
            }

            var match = FindDnsProviderByName(providers, provider);

            if (match is null)
            {
                Console.WriteLine($"Unknown DNS provider: {provider}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"DNS provider: {match.Name}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            if (!apply)
            {
                Console.WriteLine("Dry-run: DNS change skipped.");
                context.ExitCode = 0;
                return;
            }

            var success = await service.SetDnsAsync(match);
            if (!success)
            {
                Console.WriteLine("Failed to update DNS settings.");
                context.ExitCode = 2;
                return;
            }

            if (flush)
            {
                await service.FlushDnsCacheAsync();
            }

            Console.WriteLine("DNS updated successfully.");
            context.ExitCode = 0;
        });
        dnsCommand.AddCommand(setCommand);

        var resetCommand = new Command("reset", "Reset DNS to automatic (default: dry-run)");
        var resetApplyOption = CreateOption<bool>("--apply", "Actually apply changes (default: dry-run)");
        resetCommand.AddOption(resetApplyOption);
        resetCommand.SetHandler(async context =>
        {
            var apply = context.ParseResult.GetValueForOption(resetApplyOption);
            var service = new DnsService();
            var provider = DnsService.GetProviders()
                .First(item => string.Equals(item.Name, "Automatic", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("DNS provider: Automatic");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            if (!apply)
            {
                Console.WriteLine("Dry-run: DNS reset skipped.");
                context.ExitCode = 0;
                return;
            }

            var success = await service.SetDnsAsync(provider);
            Console.WriteLine(success ? "DNS reset successfully." : "Failed to reset DNS.");
            context.ExitCode = success ? 0 : 2;
        });
        dnsCommand.AddCommand(resetCommand);

        return dnsCommand;
    }
}
