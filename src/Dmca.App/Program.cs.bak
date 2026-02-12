using Dmca.App.Collectors;
using Dmca.Core.Interfaces;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

// ── Bootstrap ──

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "DMCA", "dmca.db");

Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

var db = DmcaDbContext.FromFile(dbPath);
db.EnsureSchema();

// ── Repositories ──

ISessionRepository sessionRepo = new SessionRepository(db);
IUserFactRepository userFactRepo = new UserFactRepository(db);
ISnapshotRepository snapshotRepo = new SnapshotRepository(db);

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

IPlatformInfoCollector platformCollector = new WmiPlatformInfoCollector();

var scanService = new ScanService(collectors, platformCollector, snapshotRepo, sessionService);

// ── Demo run ──

Console.WriteLine("DMCA — Driver Migration Cleanup Assistant");
Console.WriteLine("=========================================");

var session = await sessionService.CreateSessionAsync();
Console.WriteLine($"Session created: {session.Id} (status: {session.Status})");

Console.WriteLine("Scanning inventory...");
var snapshot = await scanService.ScanAsync(session.Id);

Console.WriteLine($"Scan complete. Snapshot {snapshot.Id}:");
Console.WriteLine($"  Drivers:  {snapshot.Summary.Drivers}");
Console.WriteLine($"  Services: {snapshot.Summary.Services}");
Console.WriteLine($"  Packages: {snapshot.Summary.Packages}");
Console.WriteLine($"  Apps:     {snapshot.Summary.Apps}");
Console.WriteLine($"  Total:    {snapshot.Items.Count}");

if (snapshot.Summary.Platform is { } p)
{
    Console.WriteLine($"  Platform: {p.MotherboardVendor} {p.MotherboardProduct}");
    Console.WriteLine($"  CPU:      {p.Cpu}");
    Console.WriteLine($"  OS:       {p.OsVersion}");
}

Console.WriteLine($"Session status: {(await sessionService.GetSessionAsync(session.Id))?.Status}");
Console.WriteLine("Done.");

