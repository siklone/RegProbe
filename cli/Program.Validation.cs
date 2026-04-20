using System;

namespace RegProbe.CLI;

partial class Program
{
    internal static string? ValidateOverrideOptions(bool overrideRequested, string? overrideReason)
    {
        return !overrideRequested && !string.IsNullOrWhiteSpace(overrideReason)
            ? "Override reason requires --override."
            : null;
    }

    internal static string? ValidateApplyExecutionOptions(bool apply, bool noVerify, bool noRollback)
    {
        if (!apply && noVerify)
        {
            return "--no-verify requires --apply.";
        }

        if (!apply && noRollback)
        {
            return "--no-rollback requires --apply.";
        }

        return null;
    }

    internal static string? ValidateDnsSetOptions(bool apply, bool flush)
    {
        return !apply && flush
            ? "--flush requires --apply."
            : null;
    }
}
