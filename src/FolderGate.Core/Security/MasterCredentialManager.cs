using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.Core.Security;

/// <summary>
/// High-level operations on the master recovery credential, shared by the main
/// app and the recovery tool. Coordinates the credential file, the id stamp in
/// the main config, and non-sensitive audit logging.
///
/// Intentionally absent (confirmed product decisions):
///  - no failed-attempt counter, lockout, or delay of any kind,
///  - no recovery codes, reset paths, backdoor passwords, or developer resets,
///  - no way to remove the credential or return to the not-configured state.
/// </summary>
public sealed class MasterCredentialManager
{
    private readonly MasterCredentialStore _store;
    private readonly ConfigStore _configStore;
    private readonly JsonOperationLogger _logger;
    private readonly MasterPasswordService _passwordService = new();

    public MasterCredentialManager(AppPaths paths)
    {
        _store = new MasterCredentialStore(paths);
        _configStore = new ConfigStore(paths);
        _logger = new JsonOperationLogger(paths);
    }

    public MasterSecurityState EvaluateState()
    {
        return MasterSecurityEvaluator.Evaluate(_store.Load(), _configStore.Load());
    }

    /// <summary>
    /// First-time setup. Only permitted in the NotConfigured state; a Corrupted
    /// state must never be "fixed" by overwriting the previous recovery gate.
    /// Ordering: credential file first, config stamp second — if stamping fails,
    /// the next evaluation still resolves to Configured and heals on a later save.
    /// </summary>
    public void SetupInitial(string password, string? hint)
    {
        MasterSecurityState state = EvaluateState();
        if (state != MasterSecurityState.NotConfigured)
        {
            throw new InvalidOperationException(AppText.MasterSetupNotAllowedInState);
        }

        MasterCredential credential = _passwordService.CreateCredential(password, hint);
        _store.Save(credential);
        StampConfig(credential.CredentialId);
        _logger.Info(Guid.NewGuid().ToString("N"), "master", "MasterSetup", null, AppText.MasterSetupCompletedLog);
    }

    public bool Authenticate(string password)
    {
        MasterCredentialLoadResult load = _store.Load();
        if (load.Status != MasterCredentialFileStatus.Valid)
        {
            return false;
        }

        bool ok = _passwordService.Verify(password, load.Credential);
        if (!ok)
        {
            // Log the failure event only. Never the entered password, its length,
            // any characters, hashes, or salts.
            _logger.Info(Guid.NewGuid().ToString("N"), "master", "MasterAuth", null, AppText.MasterAuthFailedLog);
        }

        return ok;
    }

    public void ChangePassword(string currentPassword, string newPassword)
    {
        MasterCredential credential = RequireValidCredential();
        MasterCredential updated = _passwordService.ChangePassword(credential, currentPassword, newPassword);
        _store.Save(updated);
        _logger.Info(Guid.NewGuid().ToString("N"), "master", "MasterPasswordChange", null, AppText.MasterPasswordChangedLog);
    }

    public void ChangeHint(string currentPassword, string? newHint)
    {
        MasterCredential credential = RequireValidCredential();
        MasterCredential updated = _passwordService.ChangeHint(credential, currentPassword, newHint);
        _store.Save(updated);
        _logger.Info(Guid.NewGuid().ToString("N"), "master", "MasterHintChange", null, AppText.MasterHintChangedLog);
    }

    /// <summary>
    /// Returns the stored hint. Callers must only surface this after an explicit
    /// "show hint" action by the user; it is never logged.
    /// </summary>
    public string? GetHint()
    {
        MasterCredentialLoadResult load = _store.Load();
        return load.Status == MasterCredentialFileStatus.Valid ? load.Credential!.Hint : null;
    }

    public void LogCorruptedStateDetected(string context)
    {
        _logger.Info(Guid.NewGuid().ToString("N"), "master", "MasterSecurityCorrupted", null,
            $"{AppText.MasterSecurityCorruptedLog} ({context})");
    }

    private MasterCredential RequireValidCredential()
    {
        MasterCredentialLoadResult load = _store.Load();
        if (load.Status != MasterCredentialFileStatus.Valid)
        {
            throw new InvalidOperationException(AppText.MasterCredentialUnavailable);
        }

        return load.Credential!;
    }

    private void StampConfig(string credentialId)
    {
        FolderGateConfig config = _configStore.Load();
        config.MasterCredentialId = credentialId;
        _configStore.Save(config);
    }
}
