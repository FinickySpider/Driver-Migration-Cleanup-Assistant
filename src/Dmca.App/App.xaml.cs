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
using Microsoft.Extensions.Logging;

namespace Dmca.App;

public partial class App : Application
{
    /// <summary>
    /// Application-wide service locator. In a larger app this would be a DI container.
    /// </summary>
    internal static ServiceContainer Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
    using System.IO;
        base.OnStartup(e);

        // ── Global exception handlers ──
        private static int _unhandledDialogGate;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // ── Database ──
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DMCA", "dmca.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

        var db = DmcaDbContext.FromFile(dbPath);
        db.EnsureSchema();

        // ── Logging ──
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole();
        });

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

        var rescanService = new RescanService(
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

        // ── AI Advisor ──
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        IAiModelClient? aiClient = null;
        AiAdvisorService? aiAdvisor = null;
        if (!string.IsNullOrWhiteSpace(apiKey) && planService is not null)
        {
            var toolsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openai_tools.json");
            var toolsJson = System.IO.File.Exists(toolsPath) ? System.IO.File.ReadAllText(toolsPath) : null;
            aiClient = new OpenAiModelClient(apiKey, model: "gpt-4o", toolDefinitionsJson: toolsJson);
            
            var toolDispatcher = new ToolDispatcher(
                sessionService,
                snapshotRepo,
                planService,
                proposalService);
            
            var systemPromptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ai_tool_policy_prompt.txt");
            var systemPrompt = System.IO.File.Exists(systemPromptPath) 
                ? System.IO.File.ReadAllText(systemPromptPath)
                : "You are the DMCA AI Advisor. Help users review driver cleanup recommendations.";
            
            aiAdvisor = new AiAdvisorService(aiClient, toolDispatcher, systemPrompt);
        }

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
            RescanService: rescanService,
            PlanService: planService!,
            ProposalService: proposalService,
            AuditLogger: auditLogger,
            QueueBuilder: queueBuilder,
            ExecutionEngine: executionEngine,
            RulesConfig: rulesConfig,
            AiAdvisorService: aiAdvisor);
    }

    private static void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var message = FormatExceptionMessage(e.Exception);
        MessageBox.Show(message, "DMCA — Unexpected Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

            if (System.Threading.Interlocked.Exchange(ref _unhandledDialogGate, 1) != 0)
            {
                e.Handled = true;
                return;
            }

            var crashPath = TryWriteCrashReport(e.Exception);
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            if (!string.IsNullOrWhiteSpace(crashPath))
            {
                message += $"\n\nCrash log:\n{crashPath}";
            }
    {
        if (e.ExceptionObject is Exception ex)
        {
            var message = FormatExceptionMessage(ex);
            MessageBox.Show(message, "DMCA — Fatal Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

                var crashPath = TryWriteCrashReport(ex);
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
                if (!string.IsNullOrWhiteSpace(crashPath))
                {
                    message += $"\n\nCrash log:\n{crashPath}";
                }
    {
        e.SetObserved();
    }

    private static string FormatExceptionMessage(Exception ex)
    {
        return ex switch
        {
            Core.CollectorException ce =>
                $"A data collector failed: {ce.CollectorName}\n\n{ce.Message}\n\nThe application will continue with partial data.",
            Core.AiClientException ae =>
                $"AI Advisor error{(ae.IsTransient ? " (transient — will retry)" : "")}:\n\n{ae.Message}",
            Core.ExecutionActionException ee =>
                $"Execution action failed on target '{ee.TargetId}':\n\n{ee.Message}",
            Core.DmcaException de =>
                $"Application error:\n\n{de.Message}",
            _ => $"An unexpected error occurred:\n\n{ex.Message}\n\nPlease report this issue.",
        };
    }
}

        private static string? TryWriteCrashReport(Exception ex)
        {
            try
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DMCA",
                    "logs");

                Directory.CreateDirectory(root);

                var fileName = $"dmca-crash-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.txt";
                var path = Path.Combine(root, fileName);

                File.WriteAllText(path, ex.ToString());
                return path;
            }
            catch
            {
                return null;
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
    RescanService RescanService,
    PlanService PlanService,
    ProposalService ProposalService,
    AuditLogger AuditLogger,
    ActionQueueBuilder QueueBuilder,
    ExecutionEngine ExecutionEngine,
    RulesConfig? RulesConfig,
    AiAdvisorService? AiAdvisorService);
