using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace SubTyping;

public partial class YouTubePlayerHost : UserControl
{
    private const string VirtualHostName = "subtyping.local";
    private bool isInitialized;
    private TaskCompletionSource<bool>? navigationReady;

    public YouTubePlayerHost()
    {
        InitializeComponent();
    }

    public async Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SubTyping",
            "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await Player.EnsureCoreWebView2Async(environment);
        var playerFolder = Path.Combine(AppContext.BaseDirectory, "Player");
        Player.CoreWebView2.SetVirtualHostNameToFolderMapping(
            VirtualHostName,
            playerFolder,
            CoreWebView2HostResourceAccessKind.Allow);
        Player.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        navigationReady = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Player.NavigationCompleted += OnNavigationCompleted;
        Player.Source = new Uri($"https://{VirtualHostName}/index.html");
        await navigationReady.Task;
        isInitialized = true;
    }

    public async Task LoadVideoAsync(string videoId)
    {
        await InitializeAsync();
        await SendCommandAsync(new { command = "load", videoId });
    }

    public Task PlayAsync() => SendCommandAsync(new { command = "play" });

    public Task PauseAsync() => SendCommandAsync(new { command = "pause" });

    public Task SeekAsync(TimeSpan position) =>
        SendCommandAsync(new { command = "seek", seconds = position.TotalSeconds });

    public event EventHandler<double>? CurrentTimeChanged;

    private async Task SendCommandAsync(object command)
    {
        if (Player.CoreWebView2 is null)
        {
            await InitializeAsync();
        }

        var message = JsonSerializer.Serialize(command);
        await Player.CoreWebView2!.ExecuteScriptAsync(
            $"window.postMessage({message}, '*');");
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var document = JsonDocument.Parse(e.WebMessageAsJson);
        var root = document.RootElement;
        if (root.TryGetProperty("type", out var type) &&
            type.GetString() == "time" &&
            root.TryGetProperty("value", out var value))
        {
            CurrentTimeChanged?.Invoke(this, value.GetDouble());
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                navigationReady?.TrySetResult(true);
            }
            else
            {
                navigationReady?.TrySetException(
                    new InvalidOperationException("The embedded YouTube player page could not be loaded."));
        }
    }
}
