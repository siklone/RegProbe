using System;
using System.Windows;
using System.Windows.Input;
using RegProbe.Application.Services;

namespace RegProbe.App.ViewModels;

public sealed class ContributorCommandPackViewModel
{
    private readonly ContributorCommandPack _pack;

    public ContributorCommandPackViewModel(ContributorCommandPack pack)
    {
        _pack = pack;
        CopyCommand = new RelayCommand(_ => CopyToClipboard(Command));
    }

    public string Title => _pack.Title;

    public string Purpose => _pack.Purpose;

    public string Command => _pack.Command;

    public string Tier => _pack.Tier;

    public bool RequiresCertifiedVm => _pack.RequiresCertifiedVm;

    public bool MutatesGuest => _pack.MutatesGuest;

    public string SafetyLabel => MutatesGuest
        ? "VM mutation: snapshot required"
        : RequiresCertifiedVm
            ? "VM evidence lane"
            : "Read-only";

    public string ExecutionPolicyLabel => MutatesGuest
        ? "Copy-only in WPF v1; run manually after certified VM health and per-run confirmation."
        : RequiresCertifiedVm
            ? "Copy-only unless exposed by a dedicated safe Run button; verify certified VM/QGA/snapshot health first."
            : "Read-only contributor command.";

    public ICommand CopyCommand { get; }

    private static void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text ?? string.Empty);
        }
        catch
        {
            // Clipboard access can fail in headless tests or locked desktops; the command text stays visible.
        }
    }
}
