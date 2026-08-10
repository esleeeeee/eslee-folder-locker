using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolderGate.App.Services;

/// <summary>
/// Tray Folder가 렌더링할 트레이 메뉴 항목입니다. 구분선은 Id와 Text가 비어 있습니다.
/// 항목 클릭 시 호스트가 Id를 menu-action 명령으로 되돌려 보냅니다.
/// </summary>
public sealed record TrayHostMenuItem(string? Id, string Text, bool Enabled, bool IsSeparator, bool Checked = false)
{
    public static TrayHostMenuItem Separator { get; } = new(null, string.Empty, false, true);

    public static TrayHostMenuItem Action(string id, string text, bool enabled = true, bool isChecked = false) =>
        new(id, text, enabled, false, isChecked);
}

/// <summary>
/// Tray Folder 호스트(Named Pipe, 프로토콜 v1)와의 연결을 유지하는 클라이언트입니다.
/// 저장소 간 코드 공유 없이 와이어 규약(파이프 이름, JSON 필드)을 Tray Folder의
/// Eslee.TrayIntegration.TrayPipeProtocol과 동일하게 맞춰야 합니다.
/// 호스트가 없으면 조용히 재시도하고, 연결이 끊어지면 트레이 아이콘을 다시 표시해
/// Standalone 상태로 복구합니다. 콜백은 백그라운드 스레드에서 호출되므로
/// 호출자가 UI 스레드 전환을 책임집니다.
/// </summary>
public sealed class TrayHostLink : IDisposable
{
    internal const int ProtocolVersion = 1;
    internal const string AppId = "eslee.folderlocker";

    private const string PipeNamePrefix = "eslee.trayfolder.tray-host.v1.";
    private const string RegisterType = "register";
    private const string SetTrayModeType = "set-tray-mode";
    private const string CommandType = "command";
    private const string CommandResultType = "command-result";
    private const string GetMenuType = "get-menu";
    private const string MenuType = "menu";
    private const string ActivateCommandValue = "activate";
    private const string MenuActionCommandValue = "menu-action";
    private const string HostedModeValue = "hosted";
    private const string StandaloneModeValue = "standalone";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _pipeName;
    private readonly int _processId;
    private readonly Func<bool, Task> _applyTrayIconVisibleAsync;
    private readonly Func<Task> _activateAsync;
    private readonly Func<Task<IReadOnlyList<TrayHostMenuItem>>> _getMenuItemsAsync;
    private readonly Func<string, Task<bool>> _executeMenuActionAsync;
    private readonly Action<string, string> _logInformation;
    private readonly Action<string, Exception> _logError;
    private readonly TimeSpan _reconnectDelay;
    private readonly TimeSpan _connectTimeout;
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _connectionLoop;
    private bool _disposed;

    public TrayHostLink(
        string pipeName,
        int processId,
        Func<bool, Task> applyTrayIconVisibleAsync,
        Func<Task> activateAsync,
        Func<Task<IReadOnlyList<TrayHostMenuItem>>> getMenuItemsAsync,
        Func<string, Task<bool>> executeMenuActionAsync,
        Action<string, string> logInformation,
        Action<string, Exception> logError,
        TimeSpan? reconnectDelay = null,
        TimeSpan? connectTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(applyTrayIconVisibleAsync);
        ArgumentNullException.ThrowIfNull(activateAsync);
        ArgumentNullException.ThrowIfNull(getMenuItemsAsync);
        ArgumentNullException.ThrowIfNull(executeMenuActionAsync);
        ArgumentNullException.ThrowIfNull(logInformation);
        ArgumentNullException.ThrowIfNull(logError);
        _pipeName = pipeName;
        _processId = processId;
        _applyTrayIconVisibleAsync = applyTrayIconVisibleAsync;
        _activateAsync = activateAsync;
        _getMenuItemsAsync = getMenuItemsAsync;
        _executeMenuActionAsync = executeMenuActionAsync;
        _logInformation = logInformation;
        _logError = logError;
        _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(3);
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(2);
    }

    public static string BuildDefaultPipeName() => BuildPipeName(Environment.UserName);

    /// <summary>Tray Folder의 TrayPipeProtocol.BuildPipeName과 동일한 규칙이어야 합니다.</summary>
    public static string BuildPipeName(string userName)
    {
        ArgumentNullException.ThrowIfNull(userName);
        StringBuilder builder = new(PipeNamePrefix, PipeNamePrefix.Length + userName.Length);
        foreach (char character in userName)
        {
            builder.Append(
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-');
        }

        return builder.ToString();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _connectionLoop ??= Task.Run(() => ConnectionLoopAsync(_lifetime.Token));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.Cancel();
        try
        {
            _connectionLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }

        _lifetime.Dispose();
    }

    private async Task ConnectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            bool wasConnected = false;
            try
            {
                NamedPipeClientStream client = new(
                    ".",
                    _pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);
                await using (client.ConfigureAwait(false))
                {
                    await client.ConnectAsync((int)_connectTimeout.TotalMilliseconds, cancellationToken)
                        .ConfigureAwait(false);
                    wasConnected = true;
                    _logInformation("tray-host.connected", "Tray Folder 호스트에 연결했습니다.");
                    await RunSessionAsync(client, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception) when (
                exception is IOException or TimeoutException or UnauthorizedAccessException
                    or ObjectDisposedException or InvalidOperationException)
            {
                if (wasConnected)
                {
                    _logError("tray-host.session-failed", exception);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (wasConnected)
            {
                _logInformation(
                    "tray-host.disconnected",
                    "Tray Folder 연결이 끊어져 트레이 아이콘을 Standalone으로 복구합니다.");
                await SafeApplyTrayIconVisibleAsync(visible: true, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(_reconnectDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunSessionAsync(NamedPipeClientStream client, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(
            client,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        StreamWriter writer = new(client, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
        {
            AutoFlush = true,
        };
        await using (writer.ConfigureAwait(false))
        {
            await WriteMessageAsync(
                writer,
                new TrayHostMessage
                {
                    Type = RegisterType,
                    ProtocolVersion = ProtocolVersion,
                    AppId = AppId,
                    DisplayName = FolderGate.Core.Localization.AppText.ProductName,
                    ProcessId = _processId,
                    Mode = StandaloneModeValue,
                },
                cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                TrayHostMessage? message;
                try
                {
                    message = JsonSerializer.Deserialize<TrayHostMessage>(line, SerializerOptions);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (message is null)
                {
                    continue;
                }

                if (string.Equals(message.Type, SetTrayModeType, StringComparison.Ordinal))
                {
                    await HandleSetTrayModeAsync(message, cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(message.Type, CommandType, StringComparison.Ordinal))
                {
                    await HandleCommandAsync(writer, message, cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(message.Type, GetMenuType, StringComparison.Ordinal))
                {
                    await HandleGetMenuAsync(writer, message, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task HandleSetTrayModeAsync(TrayHostMessage message, CancellationToken cancellationToken)
    {
        bool hosted = string.Equals(message.Mode, HostedModeValue, StringComparison.OrdinalIgnoreCase);
        bool standalone = string.Equals(message.Mode, StandaloneModeValue, StringComparison.OrdinalIgnoreCase);
        if (!hosted && !standalone)
        {
            _logInformation("tray-host.unknown-mode", $"알 수 없는 트레이 모드를 무시했습니다: {message.Mode}");
            return;
        }

        _logInformation(
            "tray-host.mode-applied",
            hosted ? "Hosted 모드: 자체 트레이 아이콘을 숨깁니다." : "Standalone 모드: 자체 트레이 아이콘을 표시합니다.");
        await SafeApplyTrayIconVisibleAsync(visible: !hosted, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleCommandAsync(
        StreamWriter writer,
        TrayHostMessage message,
        CancellationToken cancellationToken)
    {
        bool succeeded;
        string? errorMessage = null;
        if (string.Equals(message.Command, ActivateCommandValue, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await _activateAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                succeeded = true;
            }
            catch (OperationCanceledException)
            {
                succeeded = false;
                errorMessage = "앱이 종료되는 중이라 창을 열 수 없습니다.";
            }
            catch (Exception exception)
            {
                succeeded = false;
                errorMessage = "창을 여는 데 실패했습니다.";
                _logError("tray-host.activate-failed", exception);
            }
        }
        else if (string.Equals(message.Command, MenuActionCommandValue, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(message.ActionId))
            {
                succeeded = false;
                errorMessage = "메뉴 항목 id가 없습니다.";
            }
            else
            {
                try
                {
                    succeeded = await _executeMenuActionAsync(message.ActionId)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                    errorMessage = succeeded ? null : "지원하지 않는 메뉴 항목입니다.";
                }
                catch (OperationCanceledException)
                {
                    succeeded = false;
                    errorMessage = "앱이 종료되는 중이라 메뉴 항목을 실행할 수 없습니다.";
                }
                catch (Exception exception)
                {
                    succeeded = false;
                    errorMessage = "메뉴 항목을 실행하지 못했습니다.";
                    _logError("tray-host.menu-action-failed", exception);
                }
            }
        }
        else
        {
            succeeded = false;
            errorMessage = "지원하지 않는 명령입니다.";
        }

        if (message.Id is int commandId)
        {
            await WriteMessageAsync(
                writer,
                new TrayHostMessage
                {
                    Type = CommandResultType,
                    Id = commandId,
                    Succeeded = succeeded,
                    ErrorMessage = errorMessage,
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleGetMenuAsync(
        StreamWriter writer,
        TrayHostMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Id is not int requestId)
        {
            return;
        }

        IReadOnlyList<TrayHostMenuItem> items;
        try
        {
            items = await _getMenuItemsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            _logError("tray-host.get-menu-failed", exception);
            items = Array.Empty<TrayHostMenuItem>();
        }

        List<TrayMenuItemPayload> payloads = new(items.Count);
        foreach (TrayHostMenuItem item in items)
        {
            payloads.Add(item.IsSeparator
                ? new TrayMenuItemPayload { Separator = true }
                : new TrayMenuItemPayload
                {
                    Id = item.Id,
                    Text = item.Text,
                    Enabled = item.Enabled ? null : false,
                    Checked = item.Checked ? true : null,
                });
        }

        await WriteMessageAsync(
            writer,
            new TrayHostMessage
            {
                Type = MenuType,
                Id = requestId,
                Items = payloads,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task SafeApplyTrayIconVisibleAsync(bool visible, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await _applyTrayIconVisibleAsync(visible).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logError("tray-host.apply-mode-failed", exception);
        }
    }

    private static async Task WriteMessageAsync(
        StreamWriter writer,
        TrayHostMessage message,
        CancellationToken cancellationToken)
    {
        string line = JsonSerializer.Serialize(message, SerializerOptions);
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>프로토콜 v1 공용 메시지 봉투입니다. null 필드는 직렬화에서 생략됩니다.</summary>
    internal sealed class TrayHostMessage
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("protocolVersion")]
        public int? ProtocolVersion { get; set; }

        [JsonPropertyName("appId")]
        public string? AppId { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("processId")]
        public int? ProcessId { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("command")]
        public string? Command { get; set; }

        [JsonPropertyName("actionId")]
        public string? ActionId { get; set; }

        [JsonPropertyName("items")]
        public List<TrayMenuItemPayload>? Items { get; set; }

        [JsonPropertyName("succeeded")]
        public bool? Succeeded { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>menu 메시지의 항목 wire 표현입니다. enabled 생략은 true, separator/checked 생략은 false입니다.</summary>
    internal sealed class TrayMenuItemPayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

        [JsonPropertyName("separator")]
        public bool? Separator { get; set; }

        [JsonPropertyName("checked")]
        public bool? Checked { get; set; }
    }
}
