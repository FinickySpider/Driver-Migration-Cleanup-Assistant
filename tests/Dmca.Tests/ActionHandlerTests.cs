using Dmca.Core.Execution.Actions;
using Dmca.Core.Models;
using Xunit;

namespace Dmca.Tests;

public sealed class ActionHandlerTests
{
    // --- DriverUninstallAction tests ---

    [Theory]
    [InlineData("drv:oem42.inf", "oem42.inf")]
    [InlineData("pkg:oem10.inf", "oem10.inf")]
    [InlineData("drv: spaced.inf ", "spaced.inf")]
    public void DriverUninstall_ExtractPublishedName_Parses_Correctly(string targetId, string expected)
    {
        Assert.Equal(expected, DriverUninstallAction.ExtractPublishedName(targetId));
    }

    [Theory]
    [InlineData("nocolon")]
    [InlineData("drv:")]
    public void DriverUninstall_ExtractPublishedName_Returns_Null_For_Invalid(string targetId)
    {
        Assert.Null(DriverUninstallAction.ExtractPublishedName(targetId));
    }

    [Fact]
    public async Task DriverUninstall_DryRun_Returns_Success_Without_Executing()
    {
        var handler = new DriverUninstallAction();
        var action = MakeAction(ActionType.UNINSTALL_DRIVER_PACKAGE, "drv:oem42.inf");

        var result = await handler.ExecuteAsync(action, ExecutionMode.DRY_RUN);

        Assert.True(result.Success);
        Assert.Contains("[DRY RUN]", result.Output);
        Assert.Contains("pnputil", result.Command);
    }

    [Fact]
    public async Task DriverUninstall_Returns_Error_For_Invalid_Target()
    {
        var handler = new DriverUninstallAction();
        var action = MakeAction(ActionType.UNINSTALL_DRIVER_PACKAGE, "nocolon");

        var result = await handler.ExecuteAsync(action, ExecutionMode.LIVE);

        Assert.False(result.Success);
        Assert.Contains("Cannot extract", result.ErrorMessage);
    }

    // --- ServiceDisableAction tests ---

    [Theory]
    [InlineData("svc:MyService", "MyService")]
    [InlineData("svc:Intel(R) MEI", "Intel(R) MEI")]
    public void ServiceDisable_ExtractServiceName_Parses_Correctly(string targetId, string expected)
    {
        Assert.Equal(expected, ServiceDisableAction.ExtractServiceName(targetId));
    }

    [Fact]
    public async Task ServiceDisable_DryRun_Returns_Success()
    {
        var handler = new ServiceDisableAction();
        var action = MakeAction(ActionType.DISABLE_SERVICE, "svc:TestSvc");

        var result = await handler.ExecuteAsync(action, ExecutionMode.DRY_RUN);

        Assert.True(result.Success);
        Assert.Contains("[DRY RUN]", result.Output);
        Assert.Contains("sc.exe", result.Command);
    }

    [Fact]
    public async Task ServiceDisable_Returns_Error_For_Invalid_Target()
    {
        var handler = new ServiceDisableAction();
        var action = MakeAction(ActionType.DISABLE_SERVICE, "nocolon");

        var result = await handler.ExecuteAsync(action, ExecutionMode.LIVE);

        Assert.False(result.Success);
        Assert.Contains("Cannot extract", result.ErrorMessage);
    }

    // --- ProgramUninstallAction tests ---

    [Theory]
    [InlineData("\"C:\\Program Files\\App\\uninstall.exe\" /S", "C:\\Program Files\\App\\uninstall.exe", "/S")]
    [InlineData("C:\\uninstall.exe /quiet", "C:\\uninstall.exe", "/quiet")]
    [InlineData("msiexec.exe", "msiexec.exe", "")]
    public void ProgramUninstall_ParseCommand_Works(string commandLine, string expectedFile, string expectedArgs)
    {
        var (fileName, args) = ProgramUninstallAction.ParseCommand(commandLine);
        Assert.Equal(expectedFile, fileName);
        Assert.Equal(expectedArgs, args);
    }

    [Fact]
    public async Task ProgramUninstall_DryRun_Returns_Success()
    {
        var handler = new ProgramUninstallAction();
        var action = MakeAction(ActionType.UNINSTALL_PROGRAM, "app:TestApp");
        action.Command = "msiexec.exe /x {guid} /qn";

        var result = await handler.ExecuteAsync(action, ExecutionMode.DRY_RUN);

        Assert.True(result.Success);
        Assert.Contains("[DRY RUN]", result.Output);
    }

    [Fact]
    public async Task ProgramUninstall_Returns_Error_When_No_Command()
    {
        var handler = new ProgramUninstallAction();
        var action = MakeAction(ActionType.UNINSTALL_PROGRAM, "app:TestApp");
        // No command set

        var result = await handler.ExecuteAsync(action, ExecutionMode.LIVE);

        Assert.False(result.Success);
        Assert.Contains("No uninstall command", result.ErrorMessage);
    }

    // --- RestorePointAction tests ---

    [Fact]
    public async Task RestorePoint_DryRun_Returns_Success()
    {
        var handler = new RestorePointAction();
        var action = MakeAction(ActionType.CREATE_RESTORE_POINT, "session:test");

        var result = await handler.ExecuteAsync(action, ExecutionMode.DRY_RUN);

        Assert.True(result.Success);
        Assert.Contains("[DRY RUN]", result.Output);
        Assert.Contains("powershell", result.Command);
    }

    private static ExecutionAction MakeAction(ActionType type, string targetId) => new()
    {
        Id = Guid.NewGuid(),
        Order = 0,
        ActionType = type,
        TargetId = targetId,
        DisplayName = $"Test {type}",
    };
}
