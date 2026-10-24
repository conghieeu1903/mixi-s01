using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Bộ máy trình duyệt web thật nhúng trực tiếp vào màn hình 3D qua giao thức Chromium DevTools (tương đương Vuplex WebView của SLS2)
public class InGameWebView : MonoBehaviour
{
    public int ViewportWidth { get; private set; } = 1280;
    public int ViewportHeight { get; private set; } = 720;
    const string DesktopUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    public Texture2D Texture { get; private set; }
    public string CurrentUrl { get; private set; } = "https://www.google.com";
    public string CurrentTitle { get; private set; } = "Google";
    public bool IsReady { get; private set; }
    public bool IsLoading { get; private set; }
    public string StatusMessage { get; private set; } = "Đang khởi tạo trình duyệt Chromium...";

    public event Action<Texture2D> FrameUpdated;
    public event Action<string> UrlChanged;

    readonly Texture2D[] frameTextures = new Texture2D[2];
    int activeTextureIndex;
    Process browserProcess;
    ClientWebSocket webSocket;
    CancellationTokenSource cts;
    readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
    int debugPort;
    int nextCmdId = 1;
    bool isMousePressed;
    float lastMouseMoveSendTime;
    int lastSentMouseX = -1;
    int lastSentMouseY = -1;
    long lastFrameReceivedTick;

    readonly ConcurrentQueue<byte[]> pendingFrames = new ConcurrentQueue<byte[]>();
    readonly ConcurrentQueue<string> pendingUrls = new ConcurrentQueue<string>();
    string pendingInitialUrl = "https://www.google.com";
    bool isInitializing;

    void Awake()
    {
        for (int i = 0; i < frameTextures.Length; i++)
        {
            frameTextures[i] = new Texture2D(ViewportWidth, ViewportHeight, TextureFormat.RGB24, false)
            {
                name = $"InGameWebView_Texture_{i}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }
        Texture = frameTextures[0];
        ClearTextureToDark();
        Application.quitting += ShutdownProcess;
    }

    void OnDestroy()
    {
        Application.quitting -= ShutdownProcess;
        ShutdownProcess();
        for (int i = 0; i < frameTextures.Length; i++)
        {
            if (frameTextures[i] != null)
                Destroy(frameTextures[i]);
        }
    }

    void ClearTextureToDark()
    {
        Color32 bg = new Color32(24, 28, 36, 255);
        Color32[] pixels = new Color32[ViewportWidth * ViewportHeight];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bg;
        for (int t = 0; t < frameTextures.Length; t++)
        {
            if (frameTextures[t] == null) continue;
            frameTextures[t].SetPixels32(pixels);
            frameTextures[t].Apply(false);
        }
    }

    // Đảm bảo tiến trình Chromium ngầm đã được khởi chạy và kết nối
    public void EnsureStarted(string initialUrl = null)
    {
        if (!string.IsNullOrEmpty(initialUrl))
            pendingInitialUrl = NormalizeUrlOrSearch(initialUrl);

        if (IsReady)
        {
            if (!string.IsNullOrEmpty(initialUrl) && pendingInitialUrl != CurrentUrl)
                LoadUrl(pendingInitialUrl);
            return;
        }

        if (isInitializing) return;
        isInitializing = true;
        IsLoading = true;
        StatusMessage = "Đang khởi động nhân Chromium...";
        _ = StartBrowserAsync(pendingInitialUrl);
    }

    void Update()
    {
        byte[] lastFrame = null;
        while (pendingFrames.TryDequeue(out byte[] frameBytes))
            lastFrame = frameBytes;

        if (lastFrame != null)
        {
            activeTextureIndex = 1 - activeTextureIndex;
            Texture2D targetTex = frameTextures[activeTextureIndex];
            if (targetTex != null && targetTex.LoadImage(lastFrame, false))
            {
                Texture = targetTex;
                IsLoading = false;
                FrameUpdated?.Invoke(Texture);
            }
        }

        while (pendingUrls.TryDequeue(out string newUrl))
        {
            CurrentUrl = newUrl;
            UrlChanged?.Invoke(newUrl);
        }
    }

    // Chuẩn hóa chuỗi người chơi nhập thành URL hợp lệ hoặc truy vấn tìm kiếm Google
    public static string NormalizeUrlOrSearch(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return "https://www.google.com";

        string trimmed = rawInput.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (!trimmed.Contains(" ") && trimmed.Contains("."))
            return "https://" + trimmed;

        return "https://www.google.com/search?q=" + Uri.EscapeDataString(trimmed);
    }

    // Tải một địa chỉ URL hoặc từ khóa tìm kiếm mới trên trình duyệt thật
    public void LoadUrl(string rawUrl)
    {
        string target = NormalizeUrlOrSearch(rawUrl);
        CurrentUrl = target;
        pendingInitialUrl = target;
        IsLoading = true;
        StatusMessage = $"Đang tải {target}...";
        UrlChanged?.Invoke(target);

        if (!IsReady)
        {
            EnsureStarted(target);
            return;
        }

        string escaped = EscapeJsonString(target);
        _ = SendCdpCommandAsync("Page.navigate", $"{{\"url\":\"{escaped}\"}}");
    }

    // Quay lại trang trước giống BrowserPanel.BackButton của SLS2
    public void GoBack()
    {
        if (!IsReady) return;
        _ = SendCdpCommandAsync("Runtime.evaluate", "{\"expression\":\"history.back()\"}");
    }

    // Tiến tới trang sau giống BrowserPanel.ForwardButton của SLS2
    public void GoForward()
    {
        if (!IsReady) return;
        _ = SendCdpCommandAsync("Runtime.evaluate", "{\"expression\":\"history.forward()\"}");
    }

    // Tải lại trang hiện tại
    public void Reload()
    {
        if (!IsReady) return;
        IsLoading = true;
        _ = SendCdpCommandAsync("Page.reload", "{}");
    }

    // Tạm dừng toàn bộ video và âm thanh trên trang giống BrowserPanel.PauseJava của SLS2
    public void PauseMedia()
    {
        if (!IsReady) return;
        _ = SendCdpCommandAsync("Runtime.evaluate",
            "{\"expression\":\"document.querySelectorAll('video, audio').forEach(el => el.pause())\"}");
    }

    // Đồng bộ tỷ lệ khung hình của nhân Chromium khớp với khung hiển thị thực tế khi thu nhỏ hoặc phóng to cửa sổ Browser
    public void SyncViewportToPanelSize(float panelWidth, float panelHeight)
    {
        if (panelWidth < 60f || panelHeight < 60f) return;
        float aspect = Mathf.Clamp(panelWidth / panelHeight, 0.8f, 3.2f);
        int targetW = 1280;
        int targetH = Mathf.Clamp(Mathf.RoundToInt(targetW / aspect), 400, 960);

        if (Mathf.Abs(targetW - ViewportWidth) < 6 && Mathf.Abs(targetH - ViewportHeight) < 6)
            return;

        ViewportWidth = targetW;
        ViewportHeight = targetH;

        if (!IsReady) return;
        _ = ApplyViewportSizeAsync(targetW, targetH);
    }

    async Task ApplyViewportSizeAsync(int w, int h)
    {
        await SendCdpCommandAsync("Emulation.setDeviceMetricsOverride",
            $"{{\"width\":{w},\"height\":{h},\"deviceScaleFactor\":1,\"mobile\":false}}");
        await SendCdpCommandAsync("Page.startScreencast",
            $"{{\"format\":\"jpeg\",\"quality\":82,\"maxWidth\":{w},\"maxHeight\":{h},\"everyNthFrame\":1}}");
        await SendCdpCommandAsync("Page.captureScreenshot",
            "{\"format\":\"jpeg\",\"quality\":82,\"optimizeForSpeed\":true}");
    }

    // Tiếp tục phát video và âm thanh giống BrowserPanel.UnPauseJava của SLS2
    public void ResumeMedia()
    {
        if (!IsReady) return;
        _ = SendCdpCommandAsync("Runtime.evaluate",
            "{\"expression\":\"document.querySelectorAll('video, audio').forEach(el => el.play())\"}");
    }

    // Chuyển tiếp sự kiện nhấn chuột trái từ màn hình 3D vào trang web
    public void SendMouseDown(float normX, float normY)
    {
        if (!IsReady) return;
        isMousePressed = true;
        int x = Mathf.Clamp(Mathf.RoundToInt(normX * ViewportWidth), 0, ViewportWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(normY * ViewportHeight), 0, ViewportHeight - 1);
        _ = SendCdpCommandAsync("Input.dispatchMouseEvent",
            $"{{\"type\":\"mousePressed\",\"x\":{x},\"y\":{y},\"button\":\"left\",\"buttons\":1,\"clickCount\":1}}");
    }

    // Chuyển tiếp sự kiện nhả chuột trái từ màn hình 3D vào trang web
    public void SendMouseUp(float normX, float normY)
    {
        if (!IsReady) return;
        isMousePressed = false;
        int x = Mathf.Clamp(Mathf.RoundToInt(normX * ViewportWidth), 0, ViewportWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(normY * ViewportHeight), 0, ViewportHeight - 1);
        _ = SendCdpCommandAsync("Input.dispatchMouseEvent",
            $"{{\"type\":\"mouseReleased\",\"x\":{x},\"y\":{y},\"button\":\"left\",\"buttons\":0,\"clickCount\":1}}");
    }

    // Chuyển tiếp vị trí di chuyển chuột vào trang web
    public void SendMouseMove(float normX, float normY)
    {
        if (!IsReady) return;
        if (Time.unscaledTime - lastMouseMoveSendTime < 0.05f && !isMousePressed) return;

        int x = Mathf.Clamp(Mathf.RoundToInt(normX * ViewportWidth), 0, ViewportWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(normY * ViewportHeight), 0, ViewportHeight - 1);
        if (Mathf.Abs(x - lastSentMouseX) < 2 && Mathf.Abs(y - lastSentMouseY) < 2 && !isMousePressed)
            return;

        lastMouseMoveSendTime = Time.unscaledTime;
        lastSentMouseX = x;
        lastSentMouseY = y;
        string btn = isMousePressed ? "left" : "none";
        int buttons = isMousePressed ? 1 : 0;
        _ = SendCdpCommandAsync("Input.dispatchMouseEvent",
            $"{{\"type\":\"mouseMoved\",\"x\":{x},\"y\":{y},\"button\":\"{btn}\",\"buttons\":{buttons}}}");
    }

    // Chuyển tiếp con lăn cuộn trang vào trang web thật
    public void SendMouseWheel(float normX, float normY, float deltaX, float deltaY)
    {
        if (!IsReady) return;
        int x = Mathf.Clamp(Mathf.RoundToInt(normX * ViewportWidth), 0, ViewportWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(normY * ViewportHeight), 0, ViewportHeight - 1);
        int dx = Mathf.RoundToInt(deltaX);
        int dy = Mathf.RoundToInt(deltaY);
        _ = SendCdpCommandAsync("Input.dispatchMouseEvent",
            $"{{\"type\":\"mouseWheel\",\"x\":{x},\"y\":{y},\"deltaX\":{dx},\"deltaY\":{dy}}}");
    }

    // Chuyển tiếp ký tự bàn phím người chơi gõ vào ô tìm kiếm hoặc biểu mẫu trên trang web thật
    public void SendCharInput(char ch)
    {
        if (!IsReady) return;

        if (ch == '\b')
        {
            SendVirtualKey("Backspace", "Backspace", 8);
            return;
        }

        if (ch == '\n' || ch == '\r')
        {
            _ = SendEnterKeyAsync();
            return;
        }

        if (char.IsControl(ch)) return;

        string escaped = EscapeJsonString(ch.ToString());
        _ = SendCdpCommandAsync("Input.insertText", $"{{\"text\":\"{escaped}\"}}");
    }

    // Gửi phím chức năng điều hướng (mũi tên, xóa, tab) vào trang web
    public void SendVirtualKey(string key, string code, int vkCode)
    {
        if (!IsReady) return;
        _ = SendVirtualKeySequenceAsync(key, code, vkCode);
    }

    async Task SendEnterKeyAsync()
    {
        await SendCdpCommandAsync("Input.dispatchKeyEvent",
            "{\"type\":\"rawKeyDown\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13,\"text\":\"\\r\",\"unmodifiedText\":\"\\r\"}");
        await SendCdpCommandAsync("Input.dispatchKeyEvent",
            "{\"type\":\"char\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13,\"text\":\"\\r\",\"unmodifiedText\":\"\\r\"}");
        await SendCdpCommandAsync("Input.dispatchKeyEvent",
            "{\"type\":\"keyUp\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}");
    }

    async Task SendVirtualKeySequenceAsync(string key, string code, int vkCode)
    {
        await SendCdpCommandAsync("Input.dispatchKeyEvent",
            $"{{\"type\":\"rawKeyDown\",\"key\":\"{key}\",\"code\":\"{code}\",\"windowsVirtualKeyCode\":{vkCode},\"nativeVirtualKeyCode\":{vkCode}}}");
        await SendCdpCommandAsync("Input.dispatchKeyEvent",
            $"{{\"type\":\"keyUp\",\"key\":\"{key}\",\"code\":\"{code}\",\"windowsVirtualKeyCode\":{vkCode},\"nativeVirtualKeyCode\":{vkCode}}}");
    }

    static string FindChromiumExecutable()
    {
        string prog86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string prog64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] candidates =
        {
            Path.Combine(prog64, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(prog86, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(localApp, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(prog86, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(prog64, "Microsoft", "Edge", "Application", "msedge.exe")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
                return candidates[i];
        }
        return null;
    }

    static int GetFreeTcpPort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    async Task StartBrowserAsync(string initialUrl)
    {
        try
        {
            string exePath = FindChromiumExecutable();
            if (string.IsNullOrEmpty(exePath))
            {
                StatusMessage = "Không tìm thấy Google Chrome hoặc Microsoft Edge trên máy.";
                IsLoading = false;
                isInitializing = false;
                return;
            }

            ShutdownProcess();
            isInitializing = true;
            cts = new CancellationTokenSource();
            debugPort = GetFreeTcpPort();

            string profileDir = Path.Combine(Application.temporaryCachePath, $"MixiBrowserCDP_{debugPort}");
            Directory.CreateDirectory(profileDir);

            string args =
                $"--headless=new --remote-debugging-port={debugPort} --remote-allow-origins=* " +
                $"--window-size={ViewportWidth},{ViewportHeight} " +
                "--force-device-scale-factor=1 --high-dpi-support=1 " +
                $"--user-agent=\"{DesktopUserAgent}\" " +
                "--disable-blink-features=AutomationControlled " +
                "--autoplay-policy=no-user-gesture-required " +
                "--no-first-run --no-default-browser-check --disable-first-run-ui " +
                "--disable-popup-blocking --disable-extensions --disable-sync --disable-translate " +
                "--disable-background-timer-throttling --disable-backgrounding-occluded-windows --disable-renderer-backgrounding " +
                $"--user-data-dir=\"{profileDir}\" \"{initialUrl}\"";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            browserProcess = Process.Start(psi);

            string wsUrl = await WaitForDebuggerWebSocketUrlAsync(debugPort, cts.Token);
            if (string.IsNullOrEmpty(wsUrl))
            {
                StatusMessage = "Không kết nối được cổng điều khiển trình duyệt.";
                IsLoading = false;
                isInitializing = false;
                return;
            }

            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri(wsUrl), cts.Token);

            IsReady = true;
            isInitializing = false;
            StatusMessage = "Đã kết nối trình duyệt thật.";

            _ = ReceiveLoopAsync(cts.Token);

            await SendCdpCommandAsync("Page.enable", "{}");
            await SendCdpCommandAsync("Runtime.enable", "{}");
            await SendCdpCommandAsync("Emulation.setUserAgentOverride",
                $"{{\"userAgent\":\"{DesktopUserAgent}\",\"acceptLanguage\":\"vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7\",\"platform\":\"Win32\"}}");
            await SendCdpCommandAsync("Emulation.setDeviceMetricsOverride",
                $"{{\"width\":{ViewportWidth},\"height\":{ViewportHeight},\"deviceScaleFactor\":1,\"mobile\":false}}");
            await SendCdpCommandAsync("Page.startScreencast",
                $"{{\"format\":\"jpeg\",\"quality\":80,\"maxWidth\":{ViewportWidth},\"maxHeight\":{ViewportHeight},\"everyNthFrame\":1}}");

            if (!string.IsNullOrEmpty(pendingInitialUrl) && pendingInitialUrl != initialUrl)
            {
                string escaped = EscapeJsonString(pendingInitialUrl);
                await SendCdpCommandAsync("Page.navigate", $"{{\"url\":\"{escaped}\"}}");
            }

            _ = ScreenshotLoopAsync(cts.Token);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi khởi tạo trình duyệt: {ex.Message}";
            IsLoading = false;
            isInitializing = false;
        }
    }

    async Task ScreenshotLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                await Task.Delay(110, token);
                long elapsedMs = (DateTime.UtcNow.Ticks - Interlocked.Read(ref lastFrameReceivedTick)) / TimeSpan.TicksPerMillisecond;
                if (elapsedMs >= 95)
                {
                    await SendCdpCommandAsync("Page.captureScreenshot",
                        "{\"format\":\"jpeg\",\"quality\":78,\"optimizeForSpeed\":true}");
                }
            }
            catch
            {
                break;
            }
        }
    }

    static async Task<string> WaitForDebuggerWebSocketUrlAsync(int port, CancellationToken token)
    {
        using HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        string endpoint = $"http://127.0.0.1:{port}/json/list";

        for (int attempt = 0; attempt < 35; attempt++)
        {
            if (token.IsCancellationRequested) return null;
            try
            {
                string json = await http.GetStringAsync(endpoint);
                Match pageMatch = Regex.Match(json, "\"type\"\\s*:\\s*\"page\"[\\s\\S]*?\"webSocketDebuggerUrl\"\\s*:\\s*\"([^\"]+)\"");
                if (!pageMatch.Success)
                    pageMatch = Regex.Match(json, "\"webSocketDebuggerUrl\"\\s*:\\s*\"([^\"]+)\"");
                if (pageMatch.Success)
                    return pageMatch.Groups[1].Value;
            }
            catch
            {
                // Chờ tiến trình Chromium mở cổng HTTP
            }
            await Task.Delay(150, token);
        }
        return null;
    }

    async Task SendCdpCommandAsync(string method, string paramsJson)
    {
        ClientWebSocket ws = webSocket;
        CancellationTokenSource localCts = cts;
        if (ws == null || ws.State != WebSocketState.Open || localCts == null || localCts.IsCancellationRequested)
            return;

        bool locked = false;
        try
        {
            await sendLock.WaitAsync(localCts.Token);
            locked = true;
            if (ws.State != WebSocketState.Open) return;

            int id = Interlocked.Increment(ref nextCmdId);
            string payload = $"{{\"id\":{id},\"method\":\"{method}\",\"params\":{paramsJson}}}";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, localCts.Token);
        }
        catch
        {
            // Bỏ qua nếu kết nối WebSocket đã đóng
        }
        finally
        {
            if (locked)
                sendLock.Release();
        }
    }

    async Task ReceiveLoopAsync(CancellationToken token)
    {
        byte[] buffer = new byte[65536];
        using MemoryStream ms = new MemoryStream();

        while (!token.IsCancellationRequested && webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                ms.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                string msg = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                HandleCdpEventMessage(msg);
            }
            catch
            {
                break;
            }
        }
    }

    void HandleCdpEventMessage(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        if (json.Contains("\"Page.screencastFrame\""))
        {
            Match sessionMatch = Regex.Match(json, "\"sessionId\"\\s*:\\s*(\\d+)");
            if (sessionMatch.Success)
            {
                string sid = sessionMatch.Groups[1].Value;
                _ = SendCdpCommandAsync("Page.screencastFrameAck", $"{{\"sessionId\":{sid}}}");
            }
        }

        if (json.Contains("\"data\":\""))
        {
            Match dataMatch = Regex.Match(json, "\"data\"\\s*:\\s*\"([A-Za-z0-9+/=]+)\"");
            if (dataMatch.Success)
            {
                try
                {
                    byte[] rawBytes = Convert.FromBase64String(dataMatch.Groups[1].Value);
                    if (rawBytes.Length > 128)
                    {
                        Interlocked.Exchange(ref lastFrameReceivedTick, DateTime.UtcNow.Ticks);
                        pendingFrames.Enqueue(rawBytes);
                    }
                }
                catch
                {
                    // Bỏ qua khung hình lỗi
                }
            }
        }

        if (json.Contains("\"Page.frameNavigated\"") && !json.Contains("\"parentId\""))
        {
            Match urlMatch = Regex.Match(json, "\"url\"\\s*:\\s*\"(https?:[^\"]+)\"");
            if (urlMatch.Success)
            {
                string navUrl = urlMatch.Groups[1].Value;
                if (!navUrl.Contains("about:blank"))
                    pendingUrls.Enqueue(navUrl);
            }
        }
    }

    static string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
    }

    // Đóng tiến trình trình duyệt ngầm khi tắt máy tính hoặc thoát game
    public void ShutdownProcess()
    {
        IsReady = false;
        isInitializing = false;

        try
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        catch { }
        cts = null;

        try
        {
            webSocket?.Dispose();
        }
        catch { }
        webSocket = null;

        if (browserProcess != null)
        {
            try
            {
                if (!browserProcess.HasExited)
                    browserProcess.Kill();
            }
            catch { }
            try
            {
                browserProcess.Dispose();
            }
            catch { }
            browserProcess = null;
        }
    }
}
