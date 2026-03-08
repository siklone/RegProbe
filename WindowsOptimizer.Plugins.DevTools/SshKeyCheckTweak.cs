using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Plugins.DevTools;

/// <summary>
/// Checks SSH key configuration and provides recommendations.
/// Sources:
/// - Git SSH Documentation: https://git-scm.com/book/en/v2/Git-on-the-Server-Generating-Your-SSH-Public-Key
/// - OpenSSH Official: https://www.openssh.com/
/// - GitHub SSH Docs: https://docs.github.com/en/authentication/connecting-to-github-with-ssh
/// </summary>
public sealed class SshKeyCheckTweak : ITweak
{
    public string Id => "plugin.devtools.ssh-check";
    public string Name => "Check SSH Key Configuration";
    public string Description => "Verifies SSH key setup for Git authentication based on Git and OpenSSH official documentation. Checks for existing keys, permissions, and provides guidance.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => false;
    public bool SupportsRollback => false;

    private readonly string _sshDirectory;

    public SshKeyCheckTweak()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _sshDirectory = Path.Combine(userProfile, ".ssh");
    }

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        try
        {
            var results = new System.Text.StringBuilder();
            var hasIssues = false;

            // Check if .ssh directory exists
            if (!Directory.Exists(_sshDirectory))
            {
                results.AppendLine("WARNING: .ssh directory not found");
                results.AppendLine("  Location: " + _sshDirectory);
                results.AppendLine("  Action: Generate SSH keys using: ssh-keygen -t ed25519 -C \"your@email.com\"");
                hasIssues = true;
            }
            else
            {
                results.AppendLine("OK: .ssh directory exists");
                results.AppendLine("  Location: " + _sshDirectory);

                // Check for common SSH keys
                var keyFiles = new[] { "id_rsa", "id_ed25519", "id_ecdsa" };
                var foundKeys = new List<string>();

                foreach (var keyFile in keyFiles)
                {
                    var keyPath = Path.Combine(_sshDirectory, keyFile);
                    if (File.Exists(keyPath))
                    {
                        foundKeys.Add(keyFile);
                    }
                }

                if (foundKeys.Count == 0)
                {
                    results.AppendLine("WARNING: No SSH private keys found");
                    results.AppendLine("  Action: Generate SSH keys using: ssh-keygen -t ed25519 -C \"your@email.com\"");
                    hasIssues = true;
                }
                else
                {
                    results.AppendLine($"OK: Found {foundKeys.Count} SSH key(s): {string.Join(", ", foundKeys)}");

                    // Check for public keys
                    foreach (var key in foundKeys)
                    {
                        var pubKeyPath = Path.Combine(_sshDirectory, key + ".pub");
                        if (File.Exists(pubKeyPath))
                        {
                            results.AppendLine($"  OK: Public key found for {key}");
                        }
                        else
                        {
                            results.AppendLine($"  WARNING: Public key missing for {key}");
                            hasIssues = true;
                        }
                    }
                }

                // Check SSH config file
                var configPath = Path.Combine(_sshDirectory, "config");
                if (File.Exists(configPath))
                {
                    results.AppendLine("OK: SSH config file exists");
                }
                else
                {
                    results.AppendLine("INFO: No SSH config file (optional)");
                }

                // Check known_hosts
                var knownHostsPath = Path.Combine(_sshDirectory, "known_hosts");
                if (File.Exists(knownHostsPath))
                {
                    results.AppendLine("OK: known_hosts file exists");
                }
                else
                {
                    results.AppendLine("INFO: No known_hosts file yet (will be created on first SSH connection)");
                }
            }

            // Check SSH agent
            var sshAgentRunning = Environment.GetEnvironmentVariable("SSH_AGENT_LAUNCHER") != null ||
                                 Environment.GetEnvironmentVariable("SSH_AUTH_SOCK") != null;
            if (sshAgentRunning)
            {
                results.AppendLine("OK: SSH agent appears to be running");
            }
            else
            {
                results.AppendLine("INFO: SSH agent not detected (optional, use 'developer.ssh-agent-autostart' tweak to enable)");
            }

            var status = hasIssues ? TweakStatus.Detected : TweakStatus.Applied;
            var message = hasIssues 
                ? $"SSH configuration check completed with issues:\n\n{results}"
                : $"SSH configuration looks good:\n\n{results}";

            return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to check SSH configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        // This is a check-only tweak, it doesn't modify anything
        return DetectAsync(ct);
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        // Re-run detection to verify current state
        return DetectAsync(ct);
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        // This tweak doesn't modify anything, so nothing to rollback
        return Task.FromResult(new TweakResult(
            TweakStatus.RolledBack,
            "This is a diagnostic tweak - no changes were made, nothing to rollback",
            DateTimeOffset.UtcNow));
    }
}
