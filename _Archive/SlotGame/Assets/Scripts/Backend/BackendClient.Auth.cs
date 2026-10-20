using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SaiGame.Services;
using UnityEngine;

// Kiểu tài khoản người chơi đã chọn ở màn đăng nhập
public enum AuthMode
{
    None,
    Guest,
    Account,
    Google,
}

// Kết quả một lần đăng nhập hoặc đăng ký
public class AuthResult
{
    public bool Success { get; private set; } // Thành công hay không
    public string Error { get; private set; } // Thông báo lỗi hiển thị cho người chơi

    public static AuthResult Ok() => new AuthResult { Success = true };
    public static AuthResult Fail(string error) => new AuthResult { Error = error };
}

// Giao diện đăng nhập SaiServer cho màn Login, tách khỏi IBackendClient để backend giả lập của Auto Test không phải cài đặt
public interface IAuthBackend
{
    // Khôi phục phiên đã lưu, trả về false nếu cần hiện màn đăng nhập
    Task<bool> TryRestoreSessionAsync();
    // Đăng nhập bằng tên tài khoản và mật khẩu
    Task<AuthResult> LoginAsync(string username, string password);
    // Đăng ký tài khoản mới rồi đăng nhập luôn
    Task<AuthResult> RegisterAsync(string email, string username, string password);
    // Đăng nhập bằng tài khoản khách theo thiết bị
    Task<AuthResult> LoginGuestAsync();
    // Đăng nhập Google qua trình duyệt
    Task<AuthResult> LoginGoogleAsync();
    // Hủy đăng nhập Google đang chờ
    void CancelGoogleLogin();
    // Đăng xuất và xóa phiên đã lưu
    Task LogoutAsync();
}

// Phần BackendClient xử lý đăng nhập, đăng ký và khôi phục phiên qua SaiAuth
public partial class BackendClient : IAuthBackend
{
    const string AuthModePrefKey = "saigame.auth.mode"; // Khóa lưu kiểu tài khoản đã đăng nhập
    const int AuthTimeoutMs = 15000; // Thời gian chờ tối đa một lệnh xác thực

    // Kiểu tài khoản đã lưu, None khi chưa từng qua màn đăng nhập
    static AuthMode SavedAuthMode
    {
        get => (AuthMode)PlayerPrefs.GetInt(AuthModePrefKey, 0);
        set
        {
            PlayerPrefs.SetInt(AuthModePrefKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    static SaiAuth Auth => SaiServer.Instance != null ? SaiServer.Instance.SaiAuth : null; // Module SaiAuth hiện tại

    // Khôi phục phiên bằng token đã lưu, tài khoản khách thì tự đăng nhập lại bằng thông tin trên máy
    public async Task<bool> TryRestoreSessionAsync()
    {
        SaiAuth auth = Auth;
        if (auth == null || SavedAuthMode == AuthMode.None) return false;

        bool ok = auth.HasAccessToken
            && (await CallAuthAsync<UserData>((done, fail) => auth.GetMyProfile(done, fail))).Success;
        if (!ok && !string.IsNullOrEmpty(auth.RefreshToken))
        {
            ok = (await CallAuthAsync<LoginResponse>((done, fail) => auth.RefreshAuthToken(done, fail))).Success
                && (await CallAuthAsync<UserData>((done, fail) => auth.GetMyProfile(done, fail))).Success;
        }
        if (!ok && SavedAuthMode == AuthMode.Guest) ok = (await LoginGuestAsync()).Success;
        return ok && SaiServer.Instance.IsAuthenticated;
    }

    // Đăng nhập bằng tài khoản và đánh dấu là tài khoản thật
    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        SaiAuth auth = Auth;
        if (auth == null) return AuthResult.Fail("Server is not ready.");

        AuthResult result = await CallAuthAsync<LoginResponse>((done, fail) => auth.Login(username, password, done, fail));
        if (result.Success) SavedAuthMode = AuthMode.Account;
        return result;
    }

    // Đăng ký tài khoản mới rồi đăng nhập
    public async Task<AuthResult> RegisterAsync(string email, string username, string password)
    {
        SaiAuth auth = Auth;
        if (auth == null) return AuthResult.Fail("Server is not ready.");

        AuthResult result = await CallAuthAsync<RegisterResponse>((done, fail) => auth.Register(email, username, password, done, fail));
        return result.Success ? await LoginAsync(username, password) : result;
    }

    // Đăng nhập hoặc tự tạo tài khoản khách theo thiết bị
    public async Task<AuthResult> LoginGuestAsync()
    {
        SaiAuth auth = Auth;
        if (auth == null) return AuthResult.Fail("Server is not ready.");

        var tcs = new TaskCompletionSource<bool>();
        LoginOrRegisterGuest(auth, tcs);
        Task completed = await Task.WhenAny(tcs.Task, Task.Delay(AuthTimeoutMs));
        if (completed != tcs.Task || !tcs.Task.Result) return AuthResult.Fail("Could not sign in as guest. Please try again.");

        SavedAuthMode = AuthMode.Guest;
        return AuthResult.Ok();
    }

    // Mở trình duyệt đăng nhập Google và chờ server xác nhận
    public async Task<AuthResult> LoginGoogleAsync()
    {
        GoogleBackendLogin google = SaiServer.Instance != null ? SaiServer.Instance.GoogleBackendLogin : null;
        if (google == null) return AuthResult.Fail("Google sign-in is not available.");

        var tcs = new TaskCompletionSource<string>();
        void OnFailure(string error) => tcs.TrySetResult(error ?? "");

        google.OnLoginFailure += OnFailure;
        try
        {
            google.StartLogin(_ => tcs.TrySetResult(null), OnFailure);
            string error = await tcs.Task;
            if (error == "cancelled") return AuthResult.Fail("Google sign-in cancelled.");
            if (error != null) return AuthResult.Fail(ReadableAuthError(error));
        }
        finally
        {
            google.OnLoginFailure -= OnFailure;
        }

        SavedAuthMode = AuthMode.Google;
        return AuthResult.Ok();
    }

    // Hủy đăng nhập Google đang chờ trên trình duyệt
    public void CancelGoogleLogin()
    {
        if (SaiServer.Instance != null && SaiServer.Instance.GoogleBackendLogin != null)
            SaiServer.Instance.GoogleBackendLogin.CancelLogin();
    }

    // Đăng xuất SaiServer, xóa token và kiểu tài khoản đã lưu để lần sau hiện màn Login
    public async Task LogoutAsync()
    {
        SavedAuthMode = AuthMode.None;
        GoogleBackendLogin google = SaiServer.Instance != null ? SaiServer.Instance.GoogleBackendLogin : null;
        if (google != null) google.ClearAuthData();

        SaiAuth auth = Auth;
        if (auth == null) return;

        var tcs = new TaskCompletionSource<bool>();
        void OnSuccess() => tcs.TrySetResult(true);
        void OnFailure(string _) => tcs.TrySetResult(false);
        auth.OnLogoutSuccess += OnSuccess;
        auth.OnLogoutFailure += OnFailure;
        try
        {
            auth.Logout();
            await tcs.Task;
        }
        finally
        {
            auth.OnLogoutSuccess -= OnSuccess;
            auth.OnLogoutFailure -= OnFailure;
        }
    }

    // Gọi một API xác thực của SDK, đổi lỗi và timeout thành AuthResult
    static async Task<AuthResult> CallAuthAsync<T>(Action<Action<T>, Action<string>> call)
    {
        Task<T> task = CallSdk(call);
        if (await Task.WhenAny(task, Task.Delay(AuthTimeoutMs)) != task)
            return AuthResult.Fail("Server is not responding. Please try again.");
        return task.IsFaulted
            ? AuthResult.Fail(ReadableAuthError(task.Exception?.InnerException?.Message))
            : AuthResult.Ok();
    }

    // Rút gọn lỗi HTTP của SDK thành câu dễ hiểu cho người chơi
    static string ReadableAuthError(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Something went wrong. Please try again.";

        Match code = Regex.Match(raw, @"Response Code: (\d+)");
        Match message = Regex.Match(raw, "\"(?:message|detail|error)\"\\s*:\\s*\"([^\"]+)\"");
        switch (code.Success ? int.Parse(code.Groups[1].Value) : -1)
        {
            case 0: return "Cannot connect to server. Check your network.";
            case 401: return "Wrong username or password.";
            case 409: return "Username or email is already taken.";
            case 429: return "Too many attempts. Please wait a moment.";
        }
        return message.Success ? message.Groups[1].Value : "Something went wrong. Please try again.";
    }
}
