using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;

namespace RegProbe.Plugins.DevTools;

/// <summary>
/// Configures Git credential helper for secure and convenient authentication.
/// Sources:
/// - Git Credential Helper Docs: https://git-scm.com/docs/gitcredentials
/// - Git Credential Manager: https://github.com/GitCredentialManager/git-credential-manager
/// - Microsoft DevBlog: https://devblogs.microsoft.com/devops/announcing-the-release-of-the-gcm-for-windows/
/// </summary>
public sealed class GitCredentialHelperTweak : ITweak
{
    public string Id => "plugin.devtools.git-credentials";
    public string Name => "Configure Git Credential Helper";
    public string Description => "Sets up Git credential helper to cache credentials securely using Windows Credential Manager. Source: Git Credential Manager official docs.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => false;
    public bool SupportsRollback => true;

    private const string CredentialHelper = "manager-core";
    private const string CacheTimeout = "3600";
    private string? _originalHelper;
    private string? _originalTimeout;

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        try
        {
            _originalHelper = GetGitConfig("credential.helper");
            _originalTimeout = GetGitConfig("credential.cachetimeout");

            var isApplied = _originalHelper == CredentialHelper;
            var status = isApplied ? TweakStatus.Applied : TweakStatus.Detected;
            var message = isApplied
                ? "Git credential helper is already configured"
                : $"Git credential helper needs configuration. Current: '{_originalHelper ?? "not set"}', Target: '{CredentialHelper}'";

            return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to detect Git credential configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        try
        {
            SetGitConfig("credential.helper", CredentialHelper);
            SetGitConfig("credential.cachetimeout", CacheTimeout);

            return Task.FromResult(new TweakResult(
                TweakStatus.Applied,
                $"Git credential helper configured:\n- Helper: {CredentialHelper}\n- Cache timeout: {CacheTimeout} seconds\n\nYour Git credentials will now be securely stored in Windows Credential Manager.",
                DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to configure Git credential helper: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        try
        {
            var currentHelper = GetGitConfig("credential.helper");
            var currentTimeout = GetGitConfig("credential.cachetimeout");

            var isCorrect = currentHelper == CredentialHelper && currentTimeout == CacheTimeout;
            var status = isCorrect ? TweakStatus.Verified : TweakStatus.Failed;
            var message = isCorrect
                ? "Git credential helper verified successfully"
                : $"Configuration mismatch:\n- Helper: expected '{CredentialHelper}', got '{currentHelper ?? "not set"}'\n- Timeout: expected '{CacheTimeout}', got '{currentTimeout ?? "not set"}'";

            return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to verify Git credential configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_originalHelper))
            {
                UnsetGitConfig("credential.helper");
            }
            else
            {
                SetGitConfig("credential.helper", _originalHelper);
            }

            if (string.IsNullOrEmpty(_originalTimeout))
            {
                UnsetGitConfig("credential.cachetimeout");
            }
            else
            {
                SetGitConfig("credential.cachetimeout", _originalTimeout);
            }

            return Task.FromResult(new TweakResult(
                TweakStatus.RolledBack,
                "Git credential helper configuration rolled back to original settings",
                DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to rollback Git credential configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    private string? GetGitConfig(string key)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global {key}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return process.ExitCode == 0 ? output : null;
    }

    private void SetGitConfig(string key, string value)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global {key} \"{value}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to set {key}: {process.StandardError.ReadToEnd()}");
        }
    }

    private void UnsetGitConfig(string key)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global --unset {key}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
}
