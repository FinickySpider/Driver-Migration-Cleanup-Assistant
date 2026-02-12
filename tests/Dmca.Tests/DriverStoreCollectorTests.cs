using Dmca.App.Collectors;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="DriverStoreCollector"/> pnputil output parsing.
/// </summary>
public sealed class DriverStoreCollectorTests
{
    [Fact]
    public void ParsePnpUtilOutput_ParsesTypicalOutput()
    {
        var output = """
            Microsoft PnP Utility

            Published Name:     oem0.inf
            Original Name:      igdlh64.inf
            Driver package provider:  Intel Corporation
            Class Name:         Display adapters
            Class GUID:         {4d36e968-e325-11ce-bfc1-08002be10318}
            Driver Version:     12/15/2023 31.0.101.4972
            Signer Name:        Microsoft Windows Hardware Compatibility Publisher

            Published Name:     oem1.inf
            Original Name:      rt640x64.inf
            Driver package provider:  Realtek
            Class Name:         Net
            Class GUID:         {4d36e972-e325-11ce-bfc1-08002be10318}
            Driver Version:     05/10/2023 10.60.510.2023
            Signer Name:        Microsoft Windows Hardware Compatibility Publisher

            Published Name:     oem2.inf
            Original Name:      nvami.inf
            Driver package provider:  NVIDIA
            Class Name:         Display adapters
            Class GUID:         {4d36e968-e325-11ce-bfc1-08002be10318}
            Driver Version:     01/20/2024 546.33
            Signer Name:        

            """;

        var items = DriverStoreCollector.ParsePnpUtilOutput(output);

        Assert.Equal(3, items.Count);

        // First item - Intel
        Assert.Equal("pkg:oem0.inf", items[0].ItemId);
        Assert.Equal(Core.Models.InventoryItemType.DRIVER_PACKAGE, items[0].ItemType);
        Assert.Contains("igdlh64.inf", items[0].DisplayName);
        Assert.Equal("Intel Corporation", items[0].Vendor);
        Assert.Equal("oem0.inf", items[0].DriverStorePublishedName);
        Assert.Equal("igdlh64.inf", items[0].DriverInf);
        Assert.NotNull(items[0].Signature);
        var sig0 = items[0].Signature!;
        Assert.True(sig0.Signed);
        Assert.True(sig0.IsMicrosoft);

        // Second item - Realtek
        Assert.Equal("pkg:oem1.inf", items[1].ItemId);
        Assert.Equal("Realtek", items[1].Vendor);

        // Third item - NVIDIA (no signer)
        Assert.Equal("pkg:oem2.inf", items[2].ItemId);
        Assert.Equal("NVIDIA", items[2].Vendor);
        Assert.NotNull(items[2].Signature);
        var sig2 = items[2].Signature!;
        Assert.False(sig2.Signed);
    }

    [Fact]
    public void ParsePnpUtilOutput_EmptyOutput_ReturnsEmpty()
    {
        var items = DriverStoreCollector.ParsePnpUtilOutput("");
        Assert.Empty(items);
    }

    [Fact]
    public void ParsePnpUtilOutput_NoPublishedName_SkipsBlock()
    {
        var output = """
            Microsoft PnP Utility

            Some random text without proper format

            """;

        var items = DriverStoreCollector.ParsePnpUtilOutput(output);
        Assert.Empty(items);
    }
}
