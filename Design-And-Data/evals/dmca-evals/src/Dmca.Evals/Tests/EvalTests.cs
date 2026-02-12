using Dmca.Evals.ModelClients;
using Dmca.Evals.Models;
using Dmca.Evals.Util;
using FluentAssertions;
using Xunit;

namespace Dmca.Evals.Tests;

public sealed class EvalTests
{
    private static string FixturesDir => Path.Combine(AppContext.BaseDirectory, "fixtures");

    [Theory]
    [MemberData(nameof(AllFixtures))]
    public async Task Offline_Fixtures_Should_Pass(string fixtureFile)
    {
        var fixture = JsonUtil.LoadFromFile<Fixture>(Path.Combine(FixturesDir, fixtureFile));
        var runner = new EvalRunner();
        var model = new OfflineModelClient();

        var result = await runner.RunAsync(fixture, model, CancellationToken.None);

        if (fixture.Expect.MustFailIfNoEvidence)
        {
            result.Pass.Should().BeFalse();
            return;
        }

        result.Pass.Should().BeTrue(because: $"Fixture {fixture.Id} should pass offline baseline.");
    }

    [Fact(Skip = "Wire OpenAIModelClient first. Then remove Skip and run with --filter Category=Live")]
    [Trait("Category", "Live")]
    public async Task Live_S1_Should_Pass()
    {
        var fixture = JsonUtil.LoadFromFile<Fixture>(Path.Combine(FixturesDir, "S1_intel_to_amd_mei.json"));
        var runner = new EvalRunner();
        var model = new OpenAIModelClient();

        var result = await runner.RunAsync(fixture, model, CancellationToken.None);
        result.Pass.Should().BeTrue();
    }

    public static IEnumerable<object[]> AllFixtures()
    {
        var files = Directory.GetFiles(FixturesDir, "*.json")
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x!)
            .ToList();

        foreach (var f in files)
            yield return new object[] { f! };
    }
}
