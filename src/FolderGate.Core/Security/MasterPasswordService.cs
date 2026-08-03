using System.Security.Cryptography;
using System.Text;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.Core.Security;

/// <summary>
/// Creates and verifies master recovery password credentials.
///
/// Policy (confirmed product decision): the master password has NO length,
/// character-class, or strength requirements. The only rules are:
///  - it must not be the empty string,
///  - it is compared as an exact character sequence (no trimming, no case folding,
///    no Unicode normalization). Whitespace-only passwords are valid.
/// There is also intentionally NO failed-attempt limit, lockout, delay, recovery
/// code, or reset path anywhere in this class.
/// </summary>
public sealed class MasterPasswordService
{
    public const int DefaultIterations = 210_000;

    /// <summary>
    /// Technical input-size guard only, not a password policy: bounds PBKDF2 work
    /// and memory for pathological inputs (e.g. megabytes pasted by accident).
    /// Deliberately far larger than any human-chosen password so it never acts
    /// as a user-visible length rule.
    /// </summary>
    public const int MaxPasswordChars = 65_536;

    private const int SaltSize = 32;
    private const int VerifierSize = 32;

    public MasterCredential CreateCredential(string password, string? hint)
    {
        ValidatePassword(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new MasterCredential
        {
            FormatVersion = 1,
            CredentialId = Guid.NewGuid().ToString("N"),
            Algorithm = "PBKDF2-SHA256",
            Iterations = DefaultIterations,
            SaltBase64 = Convert.ToBase64String(salt),
            VerifierBase64 = Convert.ToBase64String(DeriveVerifier(password, salt, DefaultIterations)),
            Hint = NormalizeHint(hint),
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }

    public bool Verify(string password, MasterCredential? credential)
    {
        if (credential is null ||
            string.IsNullOrWhiteSpace(credential.SaltBase64) ||
            string.IsNullOrWhiteSpace(credential.VerifierBase64))
        {
            return false;
        }

        if (password is null || password.Length == 0 || password.Length > MaxPasswordChars)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(credential.SaltBase64);
            byte[] expected = Convert.FromBase64String(credential.VerifierBase64);
            byte[] actual = DeriveVerifier(password, salt, credential.Iterations);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns a re-keyed credential (new salt, new verifier, same CredentialId and
    /// CreatedUtc). Throws if the current password does not verify, if the new
    /// password is empty, or if the new password is exactly identical to the current
    /// one (ordinal comparison — case and whitespace differences count as different).
    /// </summary>
    public MasterCredential ChangePassword(MasterCredential credential, string currentPassword, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (!Verify(currentPassword, credential))
        {
            throw new InvalidOperationException(AppText.MasterCurrentPasswordInvalid);
        }

        ValidatePassword(newPassword);

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(AppText.MasterNewPasswordSameAsCurrent);
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        return new MasterCredential
        {
            FormatVersion = credential.FormatVersion,
            CredentialId = credential.CredentialId,
            Algorithm = "PBKDF2-SHA256",
            Iterations = DefaultIterations,
            SaltBase64 = Convert.ToBase64String(salt),
            VerifierBase64 = Convert.ToBase64String(DeriveVerifier(newPassword, salt, DefaultIterations)),
            Hint = credential.Hint,
            CreatedUtc = credential.CreatedUtc,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Returns a credential with an updated hint. The current password is required
    /// even for hint-only changes.
    /// </summary>
    public MasterCredential ChangeHint(MasterCredential credential, string currentPassword, string? newHint)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (!Verify(currentPassword, credential))
        {
            throw new InvalidOperationException(AppText.MasterCurrentPasswordInvalid);
        }

        return new MasterCredential
        {
            FormatVersion = credential.FormatVersion,
            CredentialId = credential.CredentialId,
            Algorithm = credential.Algorithm,
            Iterations = credential.Iterations,
            SaltBase64 = credential.SaltBase64,
            VerifierBase64 = credential.VerifierBase64,
            Hint = NormalizeHint(newHint),
            CreatedUtc = credential.CreatedUtc,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    public static void ValidatePassword(string password)
    {
        // Intentionally IsNullOrEmpty and not IsNullOrWhiteSpace: whitespace-only
        // master passwords are allowed by product decision.
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(AppText.MasterPasswordEmpty, nameof(password));
        }

        if (password.Length > MaxPasswordChars)
        {
            throw new ArgumentException(AppText.MasterPasswordTooLong, nameof(password));
        }
    }

    private static string? NormalizeHint(string? hint)
    {
        return string.IsNullOrWhiteSpace(hint) ? null : hint;
    }

    private static byte[] DeriveVerifier(string password, byte[] salt, int iterations)
    {
        if (iterations < 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "PBKDF2 iteration count is too low.");
        }

        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            VerifierSize);
    }
}
