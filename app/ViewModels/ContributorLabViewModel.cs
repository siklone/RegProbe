using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using RegProbe.Application.Services;

namespace RegProbe.App.ViewModels;

public sealed class ContributorLabViewModel : ViewModelBase
{
    private bool _riskAcknowledged;
    private bool _areToolsUnlocked;

    public ContributorLabViewModel()
        : this(ContributorLabCatalog.Load())
    {
    }

    public ContributorLabViewModel(ContributorLabSnapshot snapshot)
    {
        Snapshot = snapshot;
        ReadinessItems = new ObservableCollection<ContributorReadinessItem>(snapshot.ReadinessItems);
        CommandPacks = new ObservableCollection<ContributorCommandPackViewModel>(
            snapshot.CommandPacks.Select(pack => new ContributorCommandPackViewModel(pack)));
        Observations = new ObservableCollection<ContributorObservation>(snapshot.Observations);

        EnableContributorToolsCommand = new RelayCommand(
            _ => AreToolsUnlocked = true,
            _ => RiskAcknowledged);
        ResetAcknowledgmentCommand = new RelayCommand(_ =>
        {
            AreToolsUnlocked = false;
            RiskAcknowledged = false;
        });
    }

    public ContributorLabSnapshot Snapshot { get; }

    public string Title => "Contributor Lab";

    public string Subtitle => "Windows-first research workspace for contributors and agentic AI. Normal users should stay in Tweaks.";

    public string VerificationBadge => Snapshot.VerificationBadge;

    public string RunTier => Snapshot.RunTier;

    public bool ReferenceEligible => Snapshot.ReferenceEligible;

    public string RepoRoot => Snapshot.RepoRootFound ? Snapshot.RepoRoot : "Repo root not found";

    public int Operator96RecordCount => Snapshot.Operator96RecordCount;

    public int Operator96ReadyForAppCard => Snapshot.Operator96ReadyForAppCard;

    public int Operator96NeedsLowNoiseRerun => Snapshot.Operator96NeedsLowNoiseRerun;

    public int Operator96NoisyResultCount => Snapshot.Operator96NoisyResultCount;

    public int Operator96NonOkCount => Snapshot.Operator96NonOkCount;

    public int AppCardCandidateCount => Snapshot.AppCardCandidateCount;

    public int AppCardPassCount => Snapshot.AppCardPassCount;

    public string AppSurfacePolicySummary =>
        Operator96NonOkCount > 0 || Operator96NoisyResultCount > 0 || Operator96NeedsLowNoiseRerun > 0
            ? "Operator96 records are blocked from app cards until non_ok, noisy, and low-noise rerun counts are all zero."
            : Operator96ReadyForAppCard == 0
                ? "Operator96 records are clean Contributor Lab research observations. They are not normal optimization cards until default, rollback, explicit app-write, and bounded-claim gates pass."
                : "Only ready_for_bounded_app_card records with known defaults, rollback proof, explicit app writes, and bounded claims may move into normal app cards.";

    public bool RiskAcknowledged
    {
        get => _riskAcknowledged;
        set
        {
            if (SetProperty(ref _riskAcknowledged, value))
            {
                EnableContributorToolsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool AreToolsUnlocked
    {
        get => _areToolsUnlocked;
        private set
        {
            if (SetProperty(ref _areToolsUnlocked, value))
            {
                OnPropertyChanged(nameof(IsGateVisible));
                OnPropertyChanged(nameof(IsToolsVisible));
            }
        }
    }

    public bool IsGateVisible => !AreToolsUnlocked;

    public bool IsToolsVisible => AreToolsUnlocked;

    public RelayCommand EnableContributorToolsCommand { get; }

    public ICommand ResetAcknowledgmentCommand { get; }

    public ObservableCollection<ContributorReadinessItem> ReadinessItems { get; }

    public ObservableCollection<ContributorCommandPackViewModel> CommandPacks { get; }

    public ObservableCollection<ContributorObservation> Observations { get; }
}
