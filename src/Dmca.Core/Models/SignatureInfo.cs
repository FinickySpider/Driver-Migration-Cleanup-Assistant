namespace Dmca.Core.Models;

/// <summary>
/// Signature information for a driver or executable.
/// </summary>
public sealed class SignatureInfo
{
    public bool Signed { get; init; }
    public string? Signer { get; init; }
    public bool IsMicrosoft { get; init; }
    public bool IsWHQL { get; init; }
}
