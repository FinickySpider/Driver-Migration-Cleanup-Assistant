using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Dmca.App.Api;

/// <summary>
/// Configures and starts the loopback-only API server on 127.0.0.1:17831.
/// </summary>
public static class ApiServerBuilder
{
    public const string DefaultUrl = "http://127.0.0.1:17831";

    /// <summary>
    /// Creates a configured WebApplication builder bound to loopback only.
    /// </summary>
    public static WebApplicationBuilder CreateBuilder(string? url = null)
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseUrls(url ?? DefaultUrl);

        // Minimal logging for loopback API
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        return builder;
    }
}
