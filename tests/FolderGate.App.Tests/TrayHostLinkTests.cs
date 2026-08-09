using System.IO;
using System.IO.Pipes;
using System.Text;
using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TrayHostLinkTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [TestMethod]
    public void BuildPipeNameMatchesTrayFolderConvention()
    {
        // Tray Folder 저장소의 TrayPipeProtocolTests와 같은 기대값을 사용해
        // 저장소 간 파이프 이름 규약이 일치하는지 확인합니다.
        Assert.AreEqual(
            "eslee.trayfolder.tray-host.v1.user-1_a--",
            TrayHostLink.BuildPipeName("user 1_a!한"));
    }

    [TestMethod]
    public async Task RegistersAppliesHostedModeAndAnswersMenuRequests()
    {
        string pipeName = CreatePipeName();
        TaskCompletionSource<bool> hiddenSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> visibleSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<string> executed = [];
        using TrayHostLink link = new(
            pipeName,
            processId: 4321,
            visible =>
            {
                if (visible)
                {
                    visibleSignal.TrySetResult(true);
                }
                else
                {
                    hiddenSignal.TrySetResult(true);
                }

                return Task.CompletedTask;
            },
            () => Task.CompletedTask,
            () => Task.FromResult<IReadOnlyList<TrayHostMenuItem>>(
            [
                TrayHostMenuItem.Action("open-app", "eslee폴더잠금기 열기"),
                TrayHostMenuItem.Separator,
                TrayHostMenuItem.Action(@"unlock:C:\Example\문서", "문서 (잠김)"),
            ]),
            actionId =>
            {
                lock (executed)
                {
                    executed.Add(actionId);
                }

                return Task.FromResult(actionId.StartsWith("unlock:", StringComparison.Ordinal));
            },
            (_, _) => { },
            (_, _) => { },
            reconnectDelay: TimeSpan.FromMilliseconds(100),
            connectTimeout: TimeSpan.FromMilliseconds(500));
        link.Start();

        NamedPipeServerStream server = CreateServer(pipeName);
        await using (server.ConfigureAwait(false))
        {
            await server.WaitForConnectionAsync().WaitAsync(TestTimeout);
            using StreamReader reader = CreateReader(server);
            StreamWriter writer = CreateWriter(server);
            await using (writer.ConfigureAwait(false))
            {
                string? registerLine = await reader.ReadLineAsync().WaitAsync(TestTimeout);
                Assert.IsNotNull(registerLine);
                StringAssert.Contains(registerLine, "\"type\":\"register\"");
                StringAssert.Contains(registerLine, "\"protocolVersion\":1");
                StringAssert.Contains(registerLine, "\"appId\":\"eslee.folderlocker\"");

                await writer.WriteLineAsync("""{"type":"set-tray-mode","mode":"hosted"}""");
                Assert.IsTrue(await hiddenSignal.Task.WaitAsync(TestTimeout));

                await writer.WriteLineAsync("""{"type":"get-menu","id":5}""");
                string? menuLine = await reader.ReadLineAsync().WaitAsync(TestTimeout);
                Assert.IsNotNull(menuLine);
                StringAssert.Contains(menuLine, "\"type\":\"menu\"");
                StringAssert.Contains(menuLine, "\"id\":5");
                StringAssert.Contains(menuLine, "\"separator\":true");
                StringAssert.Contains(menuLine, "unlock:");

                await writer.WriteLineAsync(
                    """{"type":"command","id":6,"command":"menu-action","actionId":"unlock:C:\\Example\\문서"}""");
                string? resultLine = await reader.ReadLineAsync().WaitAsync(TestTimeout);
                Assert.IsNotNull(resultLine);
                StringAssert.Contains(resultLine, "\"id\":6");
                StringAssert.Contains(resultLine, "\"succeeded\":true");
                lock (executed)
                {
                    Assert.AreEqual(1, executed.Count);
                    Assert.AreEqual(@"unlock:C:\Example\문서", executed[0]);
                }
            }
        }

        // 호스트가 종료되면 클라이언트는 자체 트레이 아이콘을 다시 표시해야 합니다.
        Assert.IsTrue(await visibleSignal.Task.WaitAsync(TestTimeout));
    }

    private static string CreatePipeName() =>
        "eslee.folderlocker.link-test." + Guid.NewGuid().ToString("N");

    private static NamedPipeServerStream CreateServer(string pipeName) => new(
        pipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous,
        inBufferSize: 4096,
        outBufferSize: 4096);

    private static StreamReader CreateReader(Stream stream) => new(
        stream,
        Encoding.UTF8,
        detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024,
        leaveOpen: true);

    private static StreamWriter CreateWriter(Stream stream) => new(
        stream,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        bufferSize: 1024,
        leaveOpen: true)
    {
        AutoFlush = true,
    };
}
