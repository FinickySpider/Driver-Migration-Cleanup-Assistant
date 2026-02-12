using Dmca.App.ViewModels;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;
using Xunit;

namespace Dmca.Tests;

/// <summary>
/// ViewModel unit tests for Sprint-06 WPF UI layer.
/// These test the MVVM logic without actually rendering WPF controls.
/// </summary>
public sealed class ViewModelTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly SessionService _sessionService;
    private readonly UserFactsService _userFactsService;
    private readonly ProposalService _proposalService;

    public ViewModelTests()
    {
        _db = DmcaDbContext.InMemory($"vm_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();

        ISessionRepository sessionRepo = new SessionRepository(_db);
        IUserFactRepository userFactRepo = new UserFactRepository(_db);
        IProposalRepository proposalRepo = new ProposalRepository(_db);

        _sessionService = new SessionService(sessionRepo);
        _userFactsService = new UserFactsService(userFactRepo);
        _proposalService = new ProposalService(proposalRepo);
    }

    public void Dispose() => _keepAlive.Dispose();

    // --- MainViewModel Tests ---

    [Fact]
    public void MainViewModel_Initialize_Sets_CurrentPage_To_First()
    {
        var vm = new MainViewModel();
        var page1 = new StubPageViewModel("Page1");
        var page2 = new StubPageViewModel("Page2");

        vm.Initialize(
        [
            new NavigationItem { Label = "P1", Icon = "🔍", ViewModel = page1 },
            new NavigationItem { Label = "P2", Icon = "📋", ViewModel = page2 },
        ]);

        Assert.Equal(page1, vm.CurrentPage);
        Assert.Equal(2, vm.NavigationItems.Count);
    }

    [Fact]
    public void MainViewModel_NavigateTo_Changes_CurrentPage()
    {
        var vm = new MainViewModel();
        var page1 = new StubPageViewModel("Page1");
        var page2 = new StubPageViewModel("Page2");

        vm.Initialize(
        [
            new NavigationItem { Label = "P1", Icon = "🔍", ViewModel = page1 },
            new NavigationItem { Label = "P2", Icon = "📋", ViewModel = page2 },
        ]);

        vm.NavigateToCommand.Execute(vm.NavigationItems[1]);

        Assert.Equal(page2, vm.CurrentPage);
        Assert.True(vm.NavigationItems[1].IsActive);
        Assert.False(vm.NavigationItems[0].IsActive);
    }

    // --- InterviewViewModel Tests ---

    [Fact]
    public void InterviewViewModel_Step_Navigation()
    {
        var vm = new InterviewViewModel(_sessionService, _userFactsService);

        Assert.Equal(0, vm.CurrentStep);

        vm.NextStepCommand.Execute(null);
        Assert.Equal(1, vm.CurrentStep);

        vm.NextStepCommand.Execute(null);
        Assert.Equal(2, vm.CurrentStep);

        // Can't go past last step
        vm.NextStepCommand.Execute(null);
        Assert.Equal(2, vm.CurrentStep);

        vm.PreviousStepCommand.Execute(null);
        Assert.Equal(1, vm.CurrentStep);

        vm.PreviousStepCommand.Execute(null);
        Assert.Equal(0, vm.CurrentStep);

        // Can't go below 0
        vm.PreviousStepCommand.Execute(null);
        Assert.Equal(0, vm.CurrentStep);
    }

    [Fact]
    public async Task InterviewViewModel_Finish_Collects_Facts()
    {
        var vm = new InterviewViewModel(_sessionService, _userFactsService);
        await vm.OnNavigatedToAsync();

        Assert.NotNull(vm.Session);

        vm.PreviousHardware = "Intel i7-9700K";
        vm.NewHardware = "AMD Ryzen 9 7950X";
        vm.AdditionalNotes = "Keeping NVIDIA GPU";

        await vm.FinishCommand.ExecuteAsync(null);

        Assert.True(vm.IsComplete);
        Assert.Equal(3, vm.CollectedFacts.Count);
    }

    // --- InventoryViewModel Tests ---

    [Fact]
    public void InventoryViewModel_Filter_By_Text()
    {
        var vm = new InventoryViewModel(null!, null!);

        vm.LoadItems(CreateTestSnapshot());

        Assert.Equal(3, vm.FilteredItems.Count);

        vm.FilterText = "Intel";
        Assert.Single(vm.FilteredItems);
        Assert.Equal("Intel GPU Driver", vm.FilteredItems[0].DisplayName);
    }

    [Fact]
    public void InventoryViewModel_Filter_By_Type()
    {
        var vm = new InventoryViewModel(null!, null!);

        vm.LoadItems(CreateTestSnapshot());

        vm.FilterType = InventoryItemType.SERVICE;
        Assert.Single(vm.FilteredItems);
        Assert.Equal(InventoryItemType.SERVICE, vm.FilteredItems[0].ItemType);
    }

    // --- ItemDetailViewModel Tests ---

    [Fact]
    public void ItemDetailViewModel_ShowItem_Sets_Properties()
    {
        var vm = new ItemDetailViewModel();
        var item = CreateTestItem("drv:test", InventoryItemType.DRIVER, "Test Driver");
        var planItem = new PlanItem
        {
            ItemId = "drv:test",
            BaselineScore = 75,
            FinalScore = 75,
            Recommendation = Recommendation.REMOVE_STAGE_1,
            HardBlocks = [],
            EngineRationale = ["Old vendor"],
        };

        vm.ShowItem(item, planItem);

        Assert.Equal(item, vm.Item);
        Assert.Equal(planItem, vm.PlanItem);
    }

    // --- ProposalReviewViewModel Tests ---

    [Fact]
    public async Task ProposalReviewViewModel_Approve_Changes_Status()
    {
        var session = await _sessionService.CreateSessionAsync();
        var proposal = await _proposalService.CreateAsync(
            session.Id,
            "Remove Intel MEI",
            [new ProposalChange { Type = "SCORE_DELTA", TargetId = "drv:mei", Delta = 10, Reason = "Old" }]);

        var vm = new ProposalReviewViewModel(_proposalService, null!);
        vm.SessionId = session.Id;
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Single(vm.Proposals);

        await vm.ApproveCommand.ExecuteAsync(proposal);

        var updated = await _proposalService.GetByIdAsync(proposal.Id);
        Assert.Equal(ProposalStatus.APPROVED, updated!.Status);
    }

    [Fact]
    public async Task ProposalReviewViewModel_Reject_Changes_Status()
    {
        var session = await _sessionService.CreateSessionAsync();
        var proposal = await _proposalService.CreateAsync(
            session.Id,
            "Remove NVIDIA",
            [new ProposalChange { Type = "SCORE_DELTA", TargetId = "drv:nv", Delta = 5, Reason = "Old" }]);

        var vm = new ProposalReviewViewModel(_proposalService, null!);
        vm.SessionId = session.Id;

        await vm.RejectCommand.ExecuteAsync(proposal);

        var updated = await _proposalService.GetByIdAsync(proposal.Id);
        Assert.Equal(ProposalStatus.REJECTED, updated!.Status);
    }

    // --- ExecuteViewModel Tests ---

    [Fact]
    public void ExecuteViewModel_TwoStepConfirmation_RequiresAll()
    {
        var vm = new ExecuteViewModel(null!, null!, null!);

        Assert.False(vm.CanExecuteLive); // nothing set

        vm.AcknowledgeRisk = true;
        Assert.False(vm.CanExecuteLive); // missing EXECUTE and dry run

        vm.ConfirmationText = "EXECUTE";
        Assert.False(vm.CanExecuteLive); // missing dry run

        vm.IsDryRunComplete = true;
        Assert.True(vm.CanExecuteLive); // all conditions met
    }

    [Fact]
    public void ExecuteViewModel_WrongConfirmationText_Blocks()
    {
        var vm = new ExecuteViewModel(null!, null!, null!)
        {
            IsDryRunComplete = true,
            AcknowledgeRisk = true,
            ConfirmationText = "execute"  // lowercase — must be exact
        };

        Assert.False(vm.CanExecuteLive);
    }

    // --- Helpers ---

    private static InventorySnapshot CreateTestSnapshot() => new()
    {
        Id = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        Summary = new SnapshotSummary { Drivers = 1, Services = 1, Packages = 0, Apps = 1 },
        Items =
        [
            CreateTestItem("drv:oem1.inf", InventoryItemType.DRIVER, "Intel GPU Driver"),
            CreateTestItem("svc:TestSvc", InventoryItemType.SERVICE, "TestService"),
            CreateTestItem("app:SomeApp", InventoryItemType.APP, "SomeApp"),
        ],
    };

    private static InventoryItem CreateTestItem(string id, InventoryItemType type, string name) => new()
    {
        ItemId = id,
        ItemType = type,
        DisplayName = name,
        Vendor = name.Contains("Intel") ? "Intel" : "Other",
    };
}

file sealed class StubPageViewModel : PageViewModel
{
    public StubPageViewModel(string title) => _title = title;
    private readonly string _title;
    public override string Title => _title;
}
