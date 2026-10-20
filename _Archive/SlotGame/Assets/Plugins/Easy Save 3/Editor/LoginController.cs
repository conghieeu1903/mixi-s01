using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

// Điều khiển màn Login: khôi phục phiên cũ hoặc chờ người chơi đăng nhập trước khi GameManager tải dữ liệu
[RequireComponent(typeof(UIDocument))]
public class LoginController : MonoBehaviour
{
    const string HiddenClass = "login--hidden"; // Class USS ẩn cả màn
    const string ActiveTabClass = "login-tab--active"; // Class USS của tab đang chọn
    const string HiddenPageClass = "login__page--hidden"; // Class USS ẩn trang nhập liệu
    const string ErrorClass = "login__status--error"; // Class USS tô đỏ dòng lỗi
    const string SuccessHiddenClass = "login__success--hidden"; // Class USS ẩn trang đăng ký thành công
    const string LastUsernameKey = "login.lastUsername"; // Khóa PlayerPrefs lưu tên đăng nhập gần nhất
    const int MinUsernameLength = 3; // Độ dài tối thiểu tên tài khoản
    const int MinPasswordLength = 6; // Độ dài tối thiểu mật khẩu

    VisualElement loginRoot; // Phần tử gốc của màn Login
    VisualElement tabLogin; // Tab đăng nhập
    VisualElement tabRegister; // Tab đăng ký
    VisualElement loginPage; // Trang nhập tài khoản đăng nhập
    VisualElement registerPage; // Trang nhập thông tin đăng ký
    TextField loginUsername; // Ô tên đăng nhập
    TextField loginPassword; // Ô mật khẩu đăng nhập
    TextField registerEmail; // Ô email đăng ký
    TextField registerUsername; // Ô tên tài khoản đăng ký
    TextField registerPassword; // Ô mật khẩu đăng ký
    Label statusLabel; // Dòng trạng thái hoặc lỗi
    VisualElement submitButton; // Nút LOGIN / CREATE ACCOUNT
    Label submitLabel; // Chữ trên nút chính
    VisualElement guestButton; // Nút chơi bằng tài khoản khách
    VisualElement googleButton; // Nút đăng nhập Google
    Label googleLabel; // Chữ trên nút Google
    VisualElement successPanel; // Trang báo đăng ký thành công
    Label successMessage; // Lời chào kèm tên tài khoản vừa tạo
    VisualElement continueButton; // Nút CONTINUE vào game sau khi đăng ký
    Clickable tabLoginClick; // Sự kiện bấm tab đăng nhập
    Clickable tabRegisterClick; // Sự kiện bấm tab đăng ký
    Clickable submitClick; // Sự kiện bấm nút chính
    Clickable guestClick; // Sự kiện bấm nút khách
    Clickable googleClick; // Sự kiện bấm nút Google
    Clickable continueClick; // Sự kiện bấm nút CONTINUE

    readonly TaskCompletionSource<bool> loggedIn = new TaskCompletionSource<bool>(); // Hoàn tất khi đã đăng nhập
    IAuthBackend auth; // Backend đăng nhập, null khi chạy backend giả lập
    bool isRegister; // Đang ở tab đăng ký
    bool busy; // Đang chờ server trả lời
    bool googleWaiting; // Đang chờ xác nhận Google trên trình duyệt

    public bool IsVisible => loginRoot != null && !loginRoot.ClassListContains(HiddenClass); // Màn Login đang hiện

    // Đăng ký với GameManager để việc tải dữ liệu chờ màn Login
    void Awake()
    {
        auth = GameManager.Instance.Backend as IAuthBackend;
        GameManager.Instance.SetLoginTask(loggedIn.Task);
    }

    // Lấy phần tử giao diện và gắn sự kiện
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        loginRoot = root.Q<VisualElement>("loginRoot");
        if (loginRoot == null)
        {
            Debug.LogError("[LoginController] 'loginRoot' not found in the UIDocument.");
            return;
        }

        tabLogin = loginRoot.Q<VisualElement>("tabLogin");
        tabRegister = loginRoot.Q<VisualElement>("tabRegister");
        loginPage = loginRoot.Q<VisualElement>("loginPage");
        registerPage = loginRoot.Q<VisualElement>("registerPage");
        loginUsername = loginRoot.Q<TextField>("loginUsername");
        loginPassword = loginRoot.Q<TextField>("loginPassword");
        registerEmail = loginRoot.Q<TextField>("registerEmail");
        registerUsername = loginRoot.Q<TextField>("registerUsername");
        registerPassword = loginRoot.Q<TextField>("registerPassword");
        statusLabel = loginRoot.Q<Label>("statusLabel");
        submitButton = loginRoot.Q<VisualElement>("submitButton");
        submitLabel = loginRoot.Q<Label>("submitLabel");
        guestButton = loginRoot.Q<VisualElement>("guestButton");
        googleButton = loginRoot.Q<VisualElement>("googleButton");
        googleLabel = loginRoot.Q<Label>("googleLabel");
        successPanel = loginRoot.Q<VisualElement>("successPanel");
        successMessage = loginRoot.Q<Label>("successMessage");
        continueButton = loginRoot.Q<VisualElement>("continueButton");

        tabLoginClick = new Clickable(() => SelectTab(false));
        tabRegisterClick = new Clickable(() => SelectTab(true));
        submitClick = new Clickable(Submit);
        guestClick = new Clickable(PlayAsGuest);
        googleClick = new Clickable(ToggleGoogle);
        continueClick = new Clickable(Hide);
        tabLogin.AddManipulator(tabLoginClick);
        tabRegister.AddManipulator(tabRegisterClick);
        submitButton.AddManipulator(submitClick);
        guestButton.AddManipulator(guestClick);
        googleButton.AddManipulator(googleClick);
        continueButton.AddManipulator(continueClick);
        loginRoot.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

        loginUsername.value = PlayerPrefs.GetString(LastUsernameKey, "");
        loginRoot.AddToClassList(HiddenClass);
        successPanel.AddToClassList(SuccessHiddenClass);
        SelectTab(false);
    }

    // Gỡ sự kiện khi tắt
    void OnDisable()
    {
        if (loginRoot == null) return;
        tabLogin.RemoveManipulator(tabLoginClick);
        tabRegister.RemoveManipulator(tabRegisterClick);
        submitButton.RemoveManipulator(submitClick);
        guestButton.RemoveManipulator(guestClick);
        googleButton.RemoveManipulator(googleClick);
        continueButton.RemoveManipulator(continueClick);
        loginRoot.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        if (googleWaiting) auth?.CancelGoogleLogin();
    }

    // Thử khôi phục phiên cũ, không được thì hiện màn đăng nhập
    async void Start()
    {
        if (auth == null || await auth.TryRestoreSessionAsync())
        {
            Complete();
            return;
        }
        if (loginRoot == null) return;
        loginRoot.RemoveFromClassList(HiddenClass);
        FocusFirstField();
    }

    // Chuyển giữa tab đăng nhập và đăng ký
    void SelectTab(bool register)
    {
        if (busy) return;
        isRegister = register;
        tabLogin.EnableInClassList(ActiveTabClass, !register);
        tabRegister.EnableInClassList(ActiveTabClass, register);
        loginPage.EnableInClassList(HiddenPageClass, register);
        registerPage.EnableInClassList(HiddenPageClass, !register);
        submitLabel.text = register ? "CREATE ACCOUNT" : "LOGIN";
        SetStatus("", false);
        FocusFirstField();
    }

    // Đưa con trỏ vào ô trống đầu tiên của trang hiện tại
    void FocusFirstField()
    {
        if (isRegister) registerEmail.Focus();
        else if (string.IsNullOrEmpty(loginUsername.value)) loginUsername.Focus();
        else loginPassword.Focus();
    }

    // Kiểm tra dữ liệu rồi gửi đăng nhập hoặc đăng ký
    async void Submit()
    {
        if (busy) return;
        string error = isRegister ? ValidateRegister() : ValidateLogin();
        if (error != null)
        {
            SetStatus(error, true);
            return;
        }

        string username = (isRegister ? registerUsername.value : loginUsername.value).Trim();
        SetBusy(true, isRegister ? "CREATING ACCOUNT..." : "SIGNING IN...");
        AuthResult result = isRegister
            ? await auth.RegisterAsync(registerEmail.value.Trim(), username, registerPassword.value)
            : await auth.LoginAsync(username, loginPassword.value);
        SetBusy(false, "");

        if (!result.Success)
        {
            SetStatus(result.Error, true);
            return;
        }
        PlayerPrefs.SetString(LastUsernameKey, username);
        PlayerPrefs.Save();
        if (isRegister) ShowRegisterSuccess(username);
        else Complete();
    }

    // Báo đăng ký thành công, cho GameManager tải dữ liệu ngầm trong lúc chờ người chơi bấm CONTINUE
    void ShowRegisterSuccess(string username)
    {
        busy = true;
        successMessage.text = $"Welcome, {username}!";
        successPanel.RemoveFromClassList(SuccessHiddenClass);
        loggedIn.TrySetResult(true);
    }

    // Kiểm tra ô nhập của tab đăng nhập, trả về lỗi hoặc null
    string ValidateLogin()
    {
        if (string.IsNullOrWhiteSpace(loginUsername.value)) return "Please enter your username.";
        if (string.IsNullOrEmpty(loginPassword.value)) return "Please enter your password.";
        return null;
    }

    // Kiểm tra ô nhập của tab đăng ký, trả về lỗi hoặc null
    string ValidateRegister()
    {
        string email = registerEmail.value.Trim();
        int at = email.IndexOf('@');
        if (at < 1 || email.LastIndexOf('.') < at) return "Please enter a valid email.";
        if (registerUsername.value.Trim().Length < MinUsernameLength) return $"Username needs at least {MinUsernameLength} characters.";
        if (registerPassword.value.Length < MinPasswordLength) return $"Password needs at least {MinPasswordLength} characters.";
        return null;
    }

    // Đăng nhập bằng tài khoản khách theo thiết bị
    async void PlayAsGuest()
    {
        if (busy) return;
        SetBusy(true, "SIGNING IN AS GUEST...");
        AuthResult result = await auth.LoginGuestAsync();
        SetBusy(false, "");
        if (result.Success) Complete();
        else SetStatus(result.Error, true);
    }

    // Bắt đầu đăng nhập Google, bấm lần nữa khi đang chờ thì hủy
    async void ToggleGoogle()
    {
        if (googleWaiting)
        {
            auth.CancelGoogleLogin();
            return;
        }
        if (busy) return;

        googleWaiting = true;
        SetBusy(true, "CONTINUE SIGN-IN IN YOUR BROWSER...");
        googleButton.SetEnabled(true);
        googleLabel.text = "CANCEL";
        AuthResult result = await auth.LoginGoogleAsync();
        googleWaiting = false;
        googleLabel.text = "GOOGLE";
        SetBusy(false, "");
        if (result.Success) Complete();
        else SetStatus(result.Error, true);
    }

    // Enter để gửi, Tab giữ mặc định
    void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
        evt.StopPropagation();
        if (!successPanel.ClassListContains(SuccessHiddenClass)) Hide();
        else Submit();
    }

    // Khóa hoặc mở các ô nhập và nút khi chờ server
    void SetBusy(bool value, string message)
    {
        busy = value;
        tabLogin.SetEnabled(!value);
        tabRegister.SetEnabled(!value);
        loginPage.SetEnabled(!value);
        registerPage.SetEnabled(!value);
        submitButton.SetEnabled(!value);
        guestButton.SetEnabled(!value);
        googleButton.SetEnabled(!value);
        SetStatus(message, false);
    }

    // Hiện dòng trạng thái hoặc lỗi
    void SetStatus(string message, bool isError)
    {
        statusLabel.text = message;
        statusLabel.EnableInClassList(ErrorClass, isError);
    }

    // Ẩn màn Login và cho GameManager tiếp tục tải dữ liệu
    void Complete()
    {
        Hide();
        loggedIn.TrySetResult(true);
    }

    // Ẩn màn Login để LoadingScreen chuyển scene
    void Hide()
    {
        if (loginRoot != null) loginRoot.AddToClassList(HiddenClass);
    }
}
