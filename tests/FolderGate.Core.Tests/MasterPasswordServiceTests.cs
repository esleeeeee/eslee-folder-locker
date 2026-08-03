using FolderGate.Core.Models;
using FolderGate.Core.Security;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class MasterPasswordServiceTests
{
    private readonly MasterPasswordService _service = new();

    [TestMethod]
    public void CreateAndVerify_Succeeds()
    {
        MasterCredential credential = _service.CreateCredential("test-master-password", null);

        Assert.IsTrue(_service.Verify("test-master-password", credential));
        Assert.IsFalse(string.IsNullOrWhiteSpace(credential.CredentialId));
        Assert.AreEqual("PBKDF2-SHA256", credential.Algorithm);
        Assert.IsTrue(credential.Iterations >= 100_000);
    }

    [TestMethod]
    public void Verify_WrongPassword_Fails()
    {
        MasterCredential credential = _service.CreateCredential("correct-value", null);

        Assert.IsFalse(_service.Verify("wrong-value", credential));
    }

    [TestMethod]
    public void Create_SingleAsciiChar_Succeeds()
    {
        MasterCredential credential = _service.CreateCredential("a", null);

        Assert.IsTrue(_service.Verify("a", credential));
        Assert.IsFalse(_service.Verify("b", credential));
    }

    [TestMethod]
    public void Create_SingleKoreanChar_Succeeds()
    {
        MasterCredential credential = _service.CreateCredential("가", null);

        Assert.IsTrue(_service.Verify("가", credential));
        Assert.IsFalse(_service.Verify("나", credential));
    }

    [TestMethod]
    public void Create_DigitsOnly_Succeeds()
    {
        MasterCredential credential = _service.CreateCredential("12345", null);

        Assert.IsTrue(_service.Verify("12345", credential));
    }

    [TestMethod]
    public void Create_SymbolsOnly_Succeeds()
    {
        MasterCredential credential = _service.CreateCredential("!@#$%^&*()", null);

        Assert.IsTrue(_service.Verify("!@#$%^&*()", credential));
    }

    [TestMethod]
    public void Create_WhitespaceOnly_Succeeds()
    {
        // Whitespace-only is not empty, so it is a valid password by product decision.
        MasterCredential credential = _service.CreateCredential("   ", null);

        Assert.IsTrue(_service.Verify("   ", credential));
        Assert.IsFalse(_service.Verify("  ", credential));
        Assert.IsFalse(_service.Verify(string.Empty, credential));
    }

    [TestMethod]
    public void Create_PreservesLeadingAndTrailingSpaces()
    {
        MasterCredential credential = _service.CreateCredential(" spaced ", null);

        Assert.IsTrue(_service.Verify(" spaced ", credential));
        Assert.IsFalse(_service.Verify("spaced", credential), "Surrounding whitespace must never be trimmed silently.");
    }

    [TestMethod]
    public void Create_PastedLongPassword_Succeeds()
    {
        string longPassword = new('한', 10_000);
        MasterCredential credential = _service.CreateCredential(longPassword, null);

        Assert.IsTrue(_service.Verify(longPassword, credential));
    }

    [TestMethod]
    public void Create_MixedKoreanSpacesSymbols_Succeeds()
    {
        const string password = "우리 집 강아지는 #1!";
        MasterCredential credential = _service.CreateCredential(password, null);

        Assert.IsTrue(_service.Verify(password, credential));
    }

    [TestMethod]
    public void Create_EmptyString_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() => _service.CreateCredential(string.Empty, null));
    }

    [TestMethod]
    public void Create_OverTechnicalLimit_Throws()
    {
        string oversized = new('a', MasterPasswordService.MaxPasswordChars + 1);

        Assert.ThrowsException<ArgumentException>(() => _service.CreateCredential(oversized, null));
    }

    [TestMethod]
    public void Verify_CaseDifference_Fails()
    {
        MasterCredential credential = _service.CreateCredential("Password", null);

        Assert.IsFalse(_service.Verify("password", credential), "Passwords differing only in case are different passwords.");
    }

    [TestMethod]
    public void Verify_OneCharDifference_Fails()
    {
        MasterCredential credential = _service.CreateCredential("abcdef", null);

        Assert.IsFalse(_service.Verify("abcdeg", credential));
    }

    [TestMethod]
    public void Verify_UnlimitedRetries_NoLockoutState()
    {
        MasterCredential credential = _service.CreateCredential("final-answer", null);

        // Confirmed product decision: no failure counter, delay, or lockout.
        // Twenty consecutive failures must not affect the next correct attempt.
        for (int i = 0; i < 20; i++)
        {
            Assert.IsFalse(_service.Verify($"wrong-{i}", credential));
        }

        Assert.IsTrue(_service.Verify("final-answer", credential));
    }

    [TestMethod]
    public void Create_HintRoundtrip()
    {
        MasterCredential withHint = _service.CreateCredential("pw", "우리 강아지 이름");
        MasterCredential withoutHint = _service.CreateCredential("pw", null);
        MasterCredential blankHint = _service.CreateCredential("pw", "   ");

        Assert.AreEqual("우리 강아지 이름", withHint.Hint);
        Assert.IsNull(withoutHint.Hint);
        Assert.IsNull(blankHint.Hint, "A blank hint is stored as no hint.");
    }

    [TestMethod]
    public void ChangePassword_Succeeds_KeepsCredentialId()
    {
        MasterCredential original = _service.CreateCredential("old-password", "hint");

        MasterCredential updated = _service.ChangePassword(original, "old-password", "new-password");

        Assert.AreEqual(original.CredentialId, updated.CredentialId);
        Assert.AreEqual("hint", updated.Hint);
        Assert.IsTrue(_service.Verify("new-password", updated));
        Assert.IsFalse(_service.Verify("old-password", updated));
    }

    [TestMethod]
    public void ChangePassword_WrongCurrent_Throws()
    {
        MasterCredential original = _service.CreateCredential("old-password", null);

        Assert.ThrowsException<InvalidOperationException>(
            () => _service.ChangePassword(original, "not-the-password", "new-password"));
    }

    [TestMethod]
    public void ChangePassword_SameAsCurrent_Throws()
    {
        MasterCredential original = _service.CreateCredential("same-password", null);

        Assert.ThrowsException<InvalidOperationException>(
            () => _service.ChangePassword(original, "same-password", "same-password"));
    }

    [TestMethod]
    public void ChangePassword_CaseVariantOfCurrent_Succeeds()
    {
        // Only an exactly identical new password is rejected; a case variant is a
        // different password and must be accepted.
        MasterCredential original = _service.CreateCredential("password", null);

        MasterCredential updated = _service.ChangePassword(original, "password", "PASSWORD");

        Assert.IsTrue(_service.Verify("PASSWORD", updated));
    }

    [TestMethod]
    public void ChangePassword_EmptyNew_Throws()
    {
        MasterCredential original = _service.CreateCredential("old-password", null);

        Assert.ThrowsException<ArgumentException>(
            () => _service.ChangePassword(original, "old-password", string.Empty));
    }

    [TestMethod]
    public void ChangeHint_RequiresCurrentPassword()
    {
        MasterCredential original = _service.CreateCredential("pw", "old hint");

        Assert.ThrowsException<InvalidOperationException>(
            () => _service.ChangeHint(original, "wrong", "new hint"));

        MasterCredential updated = _service.ChangeHint(original, "pw", "new hint");
        Assert.AreEqual("new hint", updated.Hint);
        Assert.IsTrue(_service.Verify("pw", updated), "Changing the hint must not change the password.");
    }
}
