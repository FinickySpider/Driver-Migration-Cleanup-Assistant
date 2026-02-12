using System.Windows;
using Dmca.App.Collectors;
using Dmca.App.ViewModels;
using Dmca.Core.AI;
using Dmca.Core.Execution;
using Dmca.Core.Execution.Actions;
using Dmca.Core.Interfaces;
using Dmca.Core.Scoring;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.App;

public partial class App : Application
{
    /// <summary>
    /// Application-wide service locator. In a larger app this would be a DI container.
    /// </summary>
    internal static ServiceContainer Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Database ──
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DMCA", "dmca.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

        var db = DmcaDbContext.FromFile(dbPath);
        db.EnsureSchema();

        // ── Repositories ──
        var sessionRepo = new SessionRepository(db);
        var userFactRepo = new UserFactRepository(db);
        var snapshotRepo = new SnapshotRepository(db);
        var planRepo = new PlanRepository(db);
        var proposalRepo = new ProposalRepository(db);
        var queueRepo = new ActionQueueRepository(db);
        var auditLogRepo = new AuditLogRepository(db);

        // ── Services ──
        var sessionService = new SessionService(sessionRepo);
        var userFactsService = new UserFactsService(userFactRepo);

        IInventoryCollector[] collectors =
        [
            new PnpDriverCollector(),
            new DevicePresenceCollector(),
            new DriverStoreCollector(),
            new ServiceCollector(),
            new InstalledProgramsCollector(),
        ];

        var scanService = new ScanService(
            collectors,
            new WmiPlatformInfoCollector(),
            snapshotRepo,
            sessionService);

        // ── Scoring ──
        var rulesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.yml");
        RulesConfig? rulesConfig = null;
        if (System.IO.File.Exists(rulesPath))
        {
            rulesConfig = RulesLoader.LoadFromFile(rulesPath);
        }

        PlanService? planService = rulesConfig is not null
            ? new PlanService(rulesConfig, planRepo, snapshotRepo, userFactRepo, sessionService)
            : null;
        var proposalService = new ProposalService(proposalRepo);

        // ── Execution ──
        var auditLogger = new AuditLogger(auditLogRepo);
        var queueBuilder = new ActionQueueBuilder(planRepo, queueRepo);
        IActionHandler[] actionHandlers =
        [
            new RestorePointAction(),
            new DriverUninstallAction(),
            new ServiceDisableAction(),
            new ProgramUninstallAction(),
        ];
        var executionEngine = new ExecutionEngine(queueRepo, auditLogger, actionHandlers);

        // ── Wire up service container ──
        Services = new ServiceContainer(
            Db: db,
            SessionRepo: sessionRepo,
            UserFactRepo: userFactRepo,
            SnapshotRepo: snapshotRepo,
            PlanRepo: planRepo,
            ProposalRepo: proposalRepo,
            QueueRepo: queueRepo,
            AuditLogRepo: auditLogRepo,
            SessionService: sessionService,
            UserFactsService: userFactsService,
            ScanService: scanService,
            PlanService: planService!,
            ProposalService: proposalService,
            AuditLogger: auditLogger,
            QueueBuilder: queueBuilder,
            ExecutionEngine: executionEngine,
            RulesConfig: rulesConfig);
    }
}

/// <summary>
/// Simple record-based service container. In a real DI scenario you'd use
/// IServiceProvider, but this is explicit and compile-time safe.
/// </summary>
internal sealed record ServiceContainer(
    DmcaDbContext Db,
    SessionRepository SessionRepo,
    UserFactRepository UserFactRepo,
    SnapshotRepository SnapshotRepo,
    PlanRepository PlanRepo,
    ProposalRepository ProposalRepo,
    ActionQueueRepository QueueRepo,
    AuditLogRepository AuditLogRepo,
    SessionService SessionService,
    UserFactsService UserFactsService,
    ScanService ScanService,
    PlanService PlanService,
    ProposalService ProposalService,
    AuditLogger AuditLogger,
    ActionQueueBuilder QueueBuilder,
    ExecutionEngine ExecutionEngine,
    RulesConfig? RulesConfig);
