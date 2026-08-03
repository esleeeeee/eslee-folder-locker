using FolderGate.Core.Models;

namespace FolderGate.Core.Security;

public enum MasterSecurityState
{
    /// <summary>
    /// No master credential has ever been configured for this data root.
    /// First-run setup is allowed. Locking is blocked until setup completes.
    /// </summary>
    NotConfigured,

    /// <summary>
    /// A valid master credential exists and matches the id stamped in the config.
    /// </summary>
    Configured,

    /// <summary>
    /// Security data is damaged or missing while evidence of a previous
    /// configuration exists (stamped id without a matching credential file,
    /// an unreadable credential file, or a credential file whose id does not
    /// match the stamp). In this state: no new locks, no recovery tool access,
    /// no automatic re-initialization; unlocking and ACL backups are preserved.
    /// </summary>
    Corrupted
}

public static class MasterSecurityEvaluator
{
    public static MasterSecurityState Evaluate(MasterCredentialLoadResult file, FolderGateConfig config)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(config);

        bool stamped = !string.IsNullOrWhiteSpace(config.MasterCredentialId);

        return file.Status switch
        {
            MasterCredentialFileStatus.Valid when !stamped => MasterSecurityState.Configured,
            MasterCredentialFileStatus.Valid when string.Equals(
                config.MasterCredentialId,
                file.Credential!.CredentialId,
                StringComparison.OrdinalIgnoreCase) => MasterSecurityState.Configured,
            MasterCredentialFileStatus.Valid => MasterSecurityState.Corrupted,
            MasterCredentialFileStatus.Missing when stamped => MasterSecurityState.Corrupted,
            MasterCredentialFileStatus.Missing => MasterSecurityState.NotConfigured,
            _ => MasterSecurityState.Corrupted
        };
    }
}
