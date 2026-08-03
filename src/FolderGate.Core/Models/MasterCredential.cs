namespace FolderGate.Core.Models;

/// <summary>
/// Stored verifier for the master recovery password. The password itself is never
/// stored; only a PBKDF2 verifier that allows checking an entered password.
/// The hint is stored in plain text by design and the UI warns about that.
/// </summary>
public sealed class MasterCredential
{
    public int FormatVersion { get; set; } = 1;

    public string CredentialId { get; set; } = string.Empty;

    public string Algorithm { get; set; } = "PBKDF2-SHA256";

    public int Iterations { get; set; }

    public string SaltBase64 { get; set; } = string.Empty;

    public string VerifierBase64 { get; set; } = string.Empty;

    public string? Hint { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
