using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.Core.Security;

public enum MasterCredentialFileStatus
{
    Missing,
    Valid,
    Corrupted
}

public sealed record MasterCredentialLoadResult(MasterCredentialFileStatus Status, MasterCredential? Credential)
{
    public static MasterCredentialLoadResult Missing { get; } = new(MasterCredentialFileStatus.Missing, null);

    public static MasterCredentialLoadResult Corrupted { get; } = new(MasterCredentialFileStatus.Corrupted, null);
}

/// <summary>
/// Loads and saves the master credential file with atomic replacement.
/// A file that exists but cannot be parsed or fails structural validation is
/// reported as Corrupted, never silently treated as missing — treating it as
/// missing would re-open first-run setup and overwrite the recovery gate.
/// </summary>
public sealed class MasterCredentialStore
{
    private readonly AppPaths _paths;
    private readonly JsonSerializerOptions _jsonOptions = JsonOptionsFactory.Create();

    public MasterCredentialStore(AppPaths paths)
    {
        _paths = paths;
    }

    public string FilePath => _paths.MasterCredentialFilePath;

    public MasterCredentialLoadResult Load()
    {
        if (!File.Exists(_paths.MasterCredentialFilePath))
        {
            return MasterCredentialLoadResult.Missing;
        }

        try
        {
            string json = File.ReadAllText(_paths.MasterCredentialFilePath);
            MasterCredential? credential = JsonSerializer.Deserialize<MasterCredential>(json, _jsonOptions);
            return IsStructurallyValid(credential)
                ? new MasterCredentialLoadResult(MasterCredentialFileStatus.Valid, credential)
                : MasterCredentialLoadResult.Corrupted;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return MasterCredentialLoadResult.Corrupted;
        }
    }

    public void Save(MasterCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        Directory.CreateDirectory(_paths.SecurityDirectory);

        string tempPath = _paths.MasterCredentialFilePath + ".tmp";
        string json = JsonSerializer.Serialize(credential, _jsonOptions);
        File.WriteAllText(tempPath, json);
        TryRestrictFileAcl(tempPath);

        if (File.Exists(_paths.MasterCredentialFilePath))
        {
            File.Replace(tempPath, _paths.MasterCredentialFilePath, null);
        }
        else
        {
            File.Move(tempPath, _paths.MasterCredentialFilePath);
        }

        TryRestrictFileAcl(_paths.MasterCredentialFilePath);
    }

    private static bool IsStructurallyValid(MasterCredential? credential)
    {
        if (credential is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(credential.CredentialId) ||
            string.IsNullOrWhiteSpace(credential.SaltBase64) ||
            string.IsNullOrWhiteSpace(credential.VerifierBase64) ||
            credential.Iterations < 100_000)
        {
            return false;
        }

        try
        {
            _ = Convert.FromBase64String(credential.SaltBase64);
            _ = Convert.FromBase64String(credential.VerifierBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Best-effort DACL restriction to current user + SYSTEM + Administrators.
    /// Failure is non-fatal: the verifier is a one-way PBKDF2 value, the ACL is
    /// defense in depth against casual reads of the hint, not a security boundary.
    /// </summary>
    private static void TryRestrictFileAcl(string path)
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            SecurityIdentifier? user = identity.User;
            if (user is null)
            {
                return;
            }

            FileSecurity security = new();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                FileSystemRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl,
                AccessControlType.Allow));

            new FileInfo(path).SetAccessControl(security);
        }
        catch (Exception)
        {
            // Best effort only.
        }
    }
}
