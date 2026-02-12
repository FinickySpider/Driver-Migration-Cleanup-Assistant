using Dmca.Evals.Models;

namespace Dmca.Evals.Util;

public static class ConsoleReport
{
    public static void Print(EvalResult r)
    {
        Console.WriteLine($"[{r.ScenarioId}] {(r.Pass ? "PASS" : "FAIL")}");
        if (!r.Pass)
            foreach (var f in r.FailReasons) Console.WriteLine($"  - {f}");

        foreach (var kv in r.Metrics)
            Console.WriteLine($"  {kv.Key}: {kv.Value:0.###}");

        Console.WriteLine();
    }
}
