using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace System.CommandLine.Invocation
{
    internal sealed class InvocationContext
    {
        private readonly CancellationToken _cancellationToken;

        internal InvocationContext(ParseResult parseResult, CancellationToken cancellationToken = default)
        {
            ParseResult = new LegacyParseResult(parseResult);
            _cancellationToken = cancellationToken;
        }

        public LegacyParseResult ParseResult { get; }

        public int ExitCode { get; set; }

        public CancellationToken GetCancellationToken() => _cancellationToken;
    }

    internal sealed class LegacyParseResult
    {
        private readonly ParseResult _parseResult;

        internal LegacyParseResult(ParseResult parseResult)
        {
            _parseResult = parseResult;
        }

        public T GetValueForOption<T>(Option<T> option) => _parseResult.GetValue(option)!;

        public T GetValueForArgument<T>(Argument<T> argument) => _parseResult.GetValue(argument)!;

        public ParseResult Inner => _parseResult;
    }
}

namespace System.CommandLine
{
    internal static class CommandCompatibilityExtensions
    {
        public static void AddOption(this Command command, Option option) => command.Options.Add(option);

        public static void AddArgument(this Command command, Argument argument) => command.Arguments.Add(argument);

        public static void AddCommand(this Command command, Command subcommand) => command.Subcommands.Add(subcommand);

        public static void SetHandler(this Command command, Action handler) =>
            command.SetAction(_ =>
            {
                handler();
                return 0;
            });

        public static void SetHandler(this Command command, Action<System.CommandLine.Invocation.InvocationContext> handler) =>
            command.SetAction(parseResult =>
            {
                var context = new System.CommandLine.Invocation.InvocationContext(parseResult);
                handler(context);
                return context.ExitCode;
            });

        public static void SetHandler(this Command command, Func<System.CommandLine.Invocation.InvocationContext, Task> handler) =>
            command.SetAction(async (parseResult, cancellationToken) =>
            {
                var context = new System.CommandLine.Invocation.InvocationContext(parseResult, cancellationToken);
                await handler(context).ConfigureAwait(false);
                return context.ExitCode;
            });
    }
}

namespace RegProbe.CLI
{
    partial class Program
    {
        private static Option<T> CreateOption<T>(string name, string description) =>
            new(name)
            {
                Description = description
            };

        private static Option<T> CreateOption<T>(string name, Func<T> defaultValueFactory, string description) =>
            new(name)
            {
                Description = description,
                DefaultValueFactory = _ => defaultValueFactory()
            };

        private static Option<T> CreateRequiredOption<T>(string name, string description) =>
            new(name)
            {
                Description = description,
                Required = true
            };

        private static Argument<T> CreateArgument<T>(string name, string description) =>
            new(name)
            {
                Description = description
            };

        private static Argument<T> CreateArgument<T>(string name, Func<T> defaultValueFactory, string description) =>
            new(name)
            {
                Description = description,
                DefaultValueFactory = _ => defaultValueFactory()
            };
    }
}
