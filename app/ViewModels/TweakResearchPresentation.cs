using System;
using System.Windows.Media;

namespace RegProbe.App.ViewModels;

internal static class TweakResearchPresentation
{
    private static readonly SolidColorBrush PowerBrush = CreateFrozenBrush("#e74c3c");
    private static readonly SolidColorBrush KernelBrush = CreateFrozenBrush("#3498db");
    private static readonly SolidColorBrush SystemBrush = CreateFrozenBrush("#2ecc71");
    private static readonly SolidColorBrush NetworkBrush = CreateFrozenBrush("#9b59b6");
    private static readonly SolidColorBrush PrivacyBrush = CreateFrozenBrush("#e67e22");
    private static readonly SolidColorBrush PolicyBrush = CreateFrozenBrush("#1abc9c");
    private static readonly SolidColorBrush DefaultAccentBrush = CreateFrozenBrush("#888888");

    private static readonly SolidColorBrush PromotedBackgroundBrush = CreateFrozenBrush("#0a2200");
    private static readonly SolidColorBrush PromotedBorderBrush = CreateFrozenBrush("#1a4a00");
    private static readonly SolidColorBrush PromotedForegroundBrush = CreateFrozenBrush("#4caf50");
    private static readonly SolidColorBrush DraftBackgroundBrush = CreateFrozenBrush("#1a1500");
    private static readonly SolidColorBrush DraftBorderBrush = CreateFrozenBrush("#3a3000");
    private static readonly SolidColorBrush DraftForegroundBrush = CreateFrozenBrush("#ffc107");
    private static readonly SolidColorBrush HoldBackgroundBrush = CreateFrozenBrush("#1a0000");
    private static readonly SolidColorBrush HoldBorderBrush = CreateFrozenBrush("#3a0000");
    private static readonly SolidColorBrush HoldForegroundBrush = CreateFrozenBrush("#f44336");
    private static readonly SolidColorBrush ArchivedBackgroundBrush = CreateFrozenBrush("#111111");
    private static readonly SolidColorBrush ArchivedBorderBrush = CreateFrozenBrush("#222222");
    private static readonly SolidColorBrush ArchivedForegroundBrush = CreateFrozenBrush("#444444");

    private static readonly SolidColorBrush TierABrush = CreateFrozenBrush("#FFD700");
    private static readonly SolidColorBrush TierBBrush = CreateFrozenBrush("#C0C0C0");
    private static readonly SolidColorBrush TierCBrush = CreateFrozenBrush("#cd7f32");
    private static readonly SolidColorBrush TierDBrush = CreateFrozenBrush("#333333");
    private static readonly SolidColorBrush TierDForegroundBrush = CreateFrozenBrush("#666666");
    private static readonly SolidColorBrush TierDarkTextBrush = CreateFrozenBrush("#000000");

    private static readonly SolidColorBrush DocsBrush = CreateFrozenBrush("#888888");
    private static readonly SolidColorBrush SourceBrush = CreateFrozenBrush("#1abc9c");
    private static readonly SolidColorBrush RollbackBrush = CreateFrozenBrush("#aaaaaa");
    private static readonly SolidColorBrush ProofReadyBackgroundBrush = CreateFrozenBrush("#111111");
    private static readonly SolidColorBrush ProofReadyBorderBrush = CreateFrozenBrush("#2a2a2a");
    private static readonly SolidColorBrush ProofPartialBackgroundBrush = CreateFrozenBrush("#111111");
    private static readonly SolidColorBrush ProofPartialBorderBrush = CreateFrozenBrush("#333333");
    private static readonly SolidColorBrush ProofPendingBackgroundBrush = CreateFrozenBrush("#0d0d0d");
    private static readonly SolidColorBrush ProofPendingBorderBrush = CreateFrozenBrush("#1e1e1e");
    private static readonly SolidColorBrush ProofReadyForegroundBrush = CreateFrozenBrush("#ffffff");
    private static readonly SolidColorBrush ProofPartialForegroundBrush = CreateFrozenBrush("#bbbbbb");
    private static readonly SolidColorBrush ProofPendingForegroundBrush = CreateFrozenBrush("#666666");

    public static string DetermineAccentKey(string tweakId, string category)
    {
        if (!string.IsNullOrWhiteSpace(tweakId))
        {
            if (tweakId.StartsWith("system.kernel.", StringComparison.OrdinalIgnoreCase))
            {
                return "KERNEL";
            }

            if (tweakId.StartsWith("power.", StringComparison.OrdinalIgnoreCase))
            {
                return "POWER";
            }

            if (tweakId.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            {
                return "SYSTEM";
            }

            if (tweakId.StartsWith("network.", StringComparison.OrdinalIgnoreCase))
            {
                return "NETWORK";
            }

            if (tweakId.StartsWith("privacy.", StringComparison.OrdinalIgnoreCase))
            {
                return "PRIVACY";
            }

            if (tweakId.StartsWith("policy.", StringComparison.OrdinalIgnoreCase))
            {
                return "POLICY";
            }
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return "OTHER";
        }

        return category.Trim().ToUpperInvariant();
    }

    public static Brush GetAccentBrush(string accentKey) => accentKey switch
    {
        "POWER" => PowerBrush,
        "KERNEL" => KernelBrush,
        "SYSTEM" => SystemBrush,
        "NETWORK" => NetworkBrush,
        "PRIVACY" => PrivacyBrush,
        "POLICY" => PolicyBrush,
        _ => DefaultAccentBrush
    };

    public static string BuildResearchStatusTone(
        string verdictState,
        bool isMutationAllowed,
        bool isResearchGated,
        bool isPromotionActionable,
        bool isEvidenceClassActionable)
    {
        if (string.Equals(verdictState, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return "archived";
        }

        if (isMutationAllowed)
        {
            return "promoted";
        }

        if (!isMutationAllowed && (isResearchGated || !isPromotionActionable || !isEvidenceClassActionable))
        {
            return "intentional-hold";
        }

        return "draft";
    }

    public static string BuildResearchStatusText(string tone) => tone switch
    {
        "promoted" => "PROMOTED",
        "intentional-hold" => "INTENTIONAL HOLD",
        "archived" => "ARCHIVED",
        _ => "DRAFT"
    };

    public static Brush GetResearchStatusBackgroundBrush(string tone) => tone switch
    {
        "promoted" => PromotedBackgroundBrush,
        "intentional-hold" => HoldBackgroundBrush,
        "archived" => ArchivedBackgroundBrush,
        _ => DraftBackgroundBrush
    };

    public static Brush GetResearchStatusBorderBrush(string tone) => tone switch
    {
        "promoted" => PromotedBorderBrush,
        "intentional-hold" => HoldBorderBrush,
        "archived" => ArchivedBorderBrush,
        _ => DraftBorderBrush
    };

    public static Brush GetResearchStatusForegroundBrush(string tone) => tone switch
    {
        "promoted" => PromotedForegroundBrush,
        "intentional-hold" => HoldForegroundBrush,
        "archived" => ArchivedForegroundBrush,
        _ => DraftForegroundBrush
    };

    public static Brush GetTierBackgroundBrush(string evidenceClassId) => evidenceClassId switch
    {
        "A" => TierABrush,
        "B" => TierBBrush,
        "C" => TierCBrush,
        _ => TierDBrush
    };

    public static Brush GetTierBorderBrush(string evidenceClassId) => GetTierBackgroundBrush(evidenceClassId);

    public static Brush GetTierForegroundBrush(string evidenceClassId) => evidenceClassId switch
    {
        "D" => TierDForegroundBrush,
        _ => TierDarkTextBrush
    };

    public static Brush GetProofAccentBrush(string key, Brush runtimeBrush)
    {
        return key switch
        {
            "docs" => DocsBrush,
            "runtime" => runtimeBrush,
            "source" => SourceBrush,
            "rollback" => RollbackBrush,
            _ => DefaultAccentBrush
        };
    }

    public static double GetProofFillFactor(string state) => state switch
    {
        "ready" => 1d,
        "partial" => 0.55d,
        _ => 0d
    };

    public static string BuildProofStateText(string state) => state switch
    {
        "ready" => "READY",
        "partial" => "PARTIAL",
        _ => "PENDING"
    };

    public static Brush GetProofStateBackgroundBrush(string state) => state switch
    {
        "ready" => ProofReadyBackgroundBrush,
        "partial" => ProofPartialBackgroundBrush,
        _ => ProofPendingBackgroundBrush
    };

    public static Brush GetProofStateBorderBrush(string state) => state switch
    {
        "ready" => ProofReadyBorderBrush,
        "partial" => ProofPartialBorderBrush,
        _ => ProofPendingBorderBrush
    };

    public static Brush GetProofStateForegroundBrush(string state) => state switch
    {
        "ready" => ProofReadyForegroundBrush,
        "partial" => ProofPartialForegroundBrush,
        _ => ProofPendingForegroundBrush
    };

    private static SolidColorBrush CreateFrozenBrush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
