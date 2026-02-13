using System.Windows;
using System.ComponentModel;
using Dmca.App.ViewModels;
using Dmca.Core.Services;

namespace Dmca.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var svc = App.Services;
        var vm = new MainViewModel();

        // Build page view models
        var interviewVm = new InterviewViewModel(svc.SessionService, svc.UserFactsService);
        var inventoryVm = new InventoryViewModel(svc.ScanService, svc.SnapshotRepo, svc.SessionService);
        var itemDetailVm = new ItemDetailViewModel();
        var aiChatVm = new AiChatViewModel(svc.SessionService, svc.AiAdvisorService);
        var proposalReviewVm = new ProposalReviewViewModel(svc.ProposalService,
            svc.RulesConfig is not null
                ? new PlanMergeService(svc.RulesConfig, svc.PlanRepo, svc.ProposalRepo)
                : null!);
        var executeVm = new ExecuteViewModel(svc.QueueBuilder, svc.ExecutionEngine, svc.AuditLogger);
        var deltaReportVm = new DeltaReportViewModel(svc.RescanService, svc.SnapshotRepo);

        inventoryVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(InventoryViewModel.SelectedItem)) return;

            if (inventoryVm.SelectedItem is null)
            {
                itemDetailVm.Item = null;
                itemDetailVm.PlanItem = null;
                return;
            }

            itemDetailVm.ShowItem(inventoryVm.SelectedItem);
        };

        vm.Initialize(
        [
            new NavigationItem { Label = "📋  Interview",  Icon = "📋", ViewModel = interviewVm },
            new NavigationItem { Label = "🔍  Inventory",  Icon = "🔍", ViewModel = inventoryVm },
            new NavigationItem { Label = "📄  Details",     Icon = "📄", ViewModel = itemDetailVm },
            new NavigationItem { Label = "🤖  AI Advisor", Icon = "🤖", ViewModel = aiChatVm },
            new NavigationItem { Label = "📑  Proposals",  Icon = "📑", ViewModel = proposalReviewVm },
            new NavigationItem { Label = "▶️  Execute",     Icon = "▶️", ViewModel = executeVm },
            new NavigationItem { Label = "📊  Delta Report", Icon = "📊", ViewModel = deltaReportVm },
        ]);

        DataContext = vm;
    }
}
