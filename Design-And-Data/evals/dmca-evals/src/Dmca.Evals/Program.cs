using Dmca.Evals;
using Dmca.Evals.ModelClients;
using Dmca.Evals.Models;
using Dmca.Evals.Util;

var fixturePath = args.Length > 0 ? args[0] : "";
if (string.IsNullOrWhiteSpace(fixturePath))
{
    Console.WriteLine("Usage: dotnet run --project src/Dmca.Evals -- <fixture.json>");
    return;
}

var fixture = JsonUtil.LoadFromFile<Fixture>(fixturePath);
var runner = new EvalRunner();
var model = new OfflineModelClient();

var result = await runner.RunAsync(fixture, model, CancellationToken.None);
ConsoleReport.Print(result);
