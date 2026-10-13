using System.Security.Cryptography;
using System.Text;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;

namespace FolderGate.Core.Operations;

/// <summary>
/// One ACL operation per registered folder at a time, across every elevated
/// process (helper, recovery tool). The state check and the ACL change happen
/// while holding it, so two processes can never both decide to lock, re-lock or
/// restore the same folder. Take it before <see cref="ConfigStore.Update"/>, never
/// the other way round, and never across a wait for user input.
/// The mutex is thread-affine: dispose it on the thread that acquired it.
/// </summary>
public sealed class FolderOperationLock : IDisposable
{
    private readonly Mutex _mutex;
    private bool _released;

    private FolderOperationLock(Mutex mutex)
    {
        _mutex = mutex;
    }

    public static FolderOperationLock Acquire(AppPaths paths, string folderId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{paths.DataRoot}|{folderId}".ToUpperInvariant()));
        Mutex mutex = new(initiallyOwned: false, $@"Local\eslee-folder-locker-folder-{Convert.ToHexString(hash)[..16]}");
        try
        {
            if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
            {
                mutex.Dispose();
                throw new InvalidOperationException(AppText.FolderBusyRefused);
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous holder ended without releasing it; this process owns it now.
        }

        return new FolderOperationLock(mutex);
    }

    public void Dispose()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
