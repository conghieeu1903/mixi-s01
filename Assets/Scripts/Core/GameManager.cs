using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// Quản lý trạng thái chung, kết nối SaiServer và vòng đời trò chơi
public class GameManager : MonoBehaviour
{
    const string LoginScene = "OnStartLoading"; // Scene chứa màn Login và Loading

    static GameManager instance;

    // Trả về singleton GameManager, tự tạo nếu chạy thẳng từ scene lẻ
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    var go = new GameObject(nameof(GameManager));
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    // Đã có GameManager còn sống hay chưa, không tự tạo mới (dùng trong OnDisable/OnDestroy)
    public static bool HasInstance => instance != null;

    public IBackendClient Backend { get; private set; }
    public SessionManager Session { get; private set; }
    public SpinService Spins { get; private set; }
    public CloudSaveService CloudSave { get; private set; }
    public DailyRewardService DailyReward { get; private set; }
    public QuestService Quests { get; private set; }
    public int CurrentLevel { get; private set; } = 1;

    public static event System.Action<int> LevelChanged;
    public static event System.Action<SpinResult> LevelCleared;

    [Tooltip("Hàm tạo backend thay thế (Auto Test dùng backend giả lập), null thì kết nối SaiServer thật")]
    public static System.Func<IBackendClient> BackendFactory;

    [Tooltip("Tiến trình đăng nhập và tải dữ liệu người chơi lần đầu")]
    Task initTask;

    [Tooltip("Tiến trình chờ người chơi qua màn Login, null thì tự đăng nhập Guest")]
    Task loginTask;

    // Khởi tạo singleton và các dịch vụ kết nối SaiServer
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateServices();
    }

    // Tạo mới backend và các dịch vụ dữ liệu người chơi
    void CreateServices()
    {
        Backend = BackendFactory != null ? BackendFactory() : new BackendClient();
        Session = new SessionManager();
        Spins = new SpinService(Backend, Session);
        CloudSave = new CloudSaveService(Backend);
        DailyReward = new DailyRewardService(Backend);
        Quests = new QuestService(Backend as IQuestBackend);
    }

    // Đăng xuất, xóa dữ liệu người chơi cũ và quay về scene Loading để hiện màn Login
    public async Task LogoutAsync()
    {
        if (Backend is IAuthBackend auth) await auth.LogoutAsync();
        CreateServices();
        initTask = null;
        loginTask = null;
        Wallet.Set(0, 0);
        CurrentLevel = 1;
        SceneManager.LoadScene(LoginScene);
    }

    // Tải dữ liệu người chơi từ SaiServer ngay khi khởi động
    async void Start()
    {
        await InitializeAsync();
    }

    // Đăng nhập SaiServer, đồng bộ dữ liệu người chơi và ví tiền (chỉ thực thi một lần)
    public Task InitializeAsync()
    {
        return initTask ??= InitializeInternalAsync();
    }

    // Đăng ký tiến trình màn Login để InitializeAsync chờ đăng nhập xong mới tải dữ liệu
    public void SetLoginTask(Task task) => loginTask = task;

    // Thực thi tải dữ liệu người chơi từ SaiServer
    async Task InitializeInternalAsync()
    {
        if (loginTask != null) await loginTask;
        PlayerData data = await CloudSave.LoadAsync();
        if (data != null)
        {
            Wallet.Set(data.coins, data.diamonds);
            CurrentLevel = Mathf.Max(1, data.currentLevel);
        }
        _ = DailyReward.RefreshAsync();
        _ = Quests.RefreshAsync();
    }

    // Đặt level hiện tại và báo cho màn chơi dựng lại theo LevelGenerator
    public void StartLevel(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
        LevelChanged?.Invoke(CurrentLevel);
    }

    // Cập nhật ví, mở khóa level kế tiếp khi thắng và lưu tiến trình lên SaiServer
    public async void HandleSpinResult(SpinResult result)
    {
        if (result == null) return;
        Wallet.Set(result.coins, Wallet.Diamonds);
        FindFirstObjectByType<PopupController>()?.ShowResult(result);
        if (result.levelCleared)
        {
            CurrentLevel = Mathf.Max(CurrentLevel + 1, result.level);
            LevelCleared?.Invoke(result);
        }
        if (CloudSave != null) await CloudSave.RecordSpinResultAsync(result);
    }

    // Chuyển sang level kế tiếp khi người chơi bấm NEXT
    public void GoToNextLevel() => StartLevel(CurrentLevel);
}
