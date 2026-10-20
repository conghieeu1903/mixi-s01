using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gốc của game: tạo các hệ thống, nối sự kiện, chạy đồng hồ và lưu game
[DefaultExecutionOrder(-100)]
public class GameWorld : MonoBehaviour
{
    static GameWorld instance;

    // Trả về singleton GameWorld, tự tạo nếu scene chưa có
    public static GameWorld Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameWorld>();
                if (instance == null)
                {
                    var go = new GameObject(nameof(GameWorld));
                    instance = go.AddComponent<GameWorld>();
                }
            }
            if (instance.Clock == null)
                instance.CreateSystems();
            return instance;
        }
    }

    // Đã có GameWorld còn sống hay chưa, không tự tạo mới
    public static bool HasInstance => instance != null;

    [Tooltip("Số phút trong game trôi qua mỗi giây thực")]
    [SerializeField] float minutesPerRealSecond = 5f;

    [Tooltip("Chu kỳ kiểm tra debuff sinh tồn (giây thực)")]
    [SerializeField] float debuffCheckSeconds = 5f;

    [Tooltip("Tự động lưu mỗi khi qua ngày mới")]
    [SerializeField] bool autoSaveEachDay = true;

    public GameClock Clock { get; private set; }
    public MoodSystem Mood { get; private set; }
    public SurvivalNeeds Needs { get; private set; }
    public Economy Economy { get; private set; }
    public StreamSystem Stream { get; private set; }
    public Inventory Inventory { get; private set; }
    public OrderService Orders { get; private set; }
    public SkillSystem Skills { get; private set; }
    public StreamCardDeck Cards { get; private set; }
    public ChatGenerator Chat { get; private set; }
    public ChatModeration Moderation { get; private set; }
    public CryptoMining Crypto { get; private set; }
    public MailJobs Mail { get; private set; }
    public WorkerManager Workers { get; private set; }
    public ElectricitySystem Electricity { get; private set; }
    public VirusSystem Virus { get; private set; }
    public PetSystem Pets { get; private set; }
    public Raffle Raffle { get; private set; }
    public GtuLottery Gtu { get; private set; }
    public List<StreamRecord> History { get; private set; } = new List<StreamRecord>();

    // Độ sạch nhà 0–1, do hệ thống dọn dẹp cập nhật sau này
    public float HouseCleanliness = 1f;

    System.Random rng;
    float debuffTimer;
    SaveData pendingSceneData;

    // Khởi tạo singleton và các hệ thống
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateSystems();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Tạo các hệ thống và nối sự kiện giữa chúng
    void CreateSystems()
    {
        if (Clock != null) return;
        rng = new System.Random();
        Clock = new GameClock { MinutesPerRealSecond = minutesPerRealSecond };
        Mood = new MoodSystem();
        Needs = new SurvivalNeeds(rng);
        Economy = new Economy();
        Stream = new StreamSystem(Clock, Mood, Economy, rng);
        Inventory = new Inventory();
        Orders = new OrderService(Clock, Economy);
        Skills = new SkillSystem();
        Cards = new StreamCardDeck(rng);
        Chat = new ChatGenerator();
        Moderation = new ChatModeration();
        Crypto = new CryptoMining();
        Mail = new MailJobs(rng);
        Workers = new WorkerManager();
        Electricity = new ElectricitySystem();
        Virus = new VirusSystem();
        Pets = new PetSystem();
        Raffle = new Raffle();
        Gtu = new GtuLottery();
        History.Clear();
        debuffTimer = 0f;

        Clock.MinutePassed += Stream.OnMinutePassed;
        Clock.MinutePassed += Orders.OnMinutePassed;
        Clock.MinutePassed += OnMinuteTick;
        Clock.HourPassed += Needs.OnHourPassed;
        Clock.HourPassed += OnHourTick;
        Clock.DayPassed += OnDayPassed;
        Stream.Closed += OnStreamClosed;
        Stream.SkillViewerBonus = cat => Skills.ViewerBonus(cat) + Pets.ViewerBonus();

        // Phát hóa đơn tiền nhà ngày đầu để người chơi có thể kiểm tra và thanh toán ngay trong ngày 1
        Economy.IssueDailyBills(Clock.Day);
    }

    // Đặt lại toàn bộ trạng thái về ngày 1 khi bắt đầu game mới
    public void ResetWorld()
    {
        var data = new SaveData();
        pendingSceneData = null;
        Clock.Set(data.day, data.hour, data.minute);
        Mood.Set(data.mood);
        Needs.Import(data);
        Economy.Import(data);
        Economy.IssueDailyBills(Clock.Day);
        Stream.Import(data);
        Inventory.Clear();
        Orders.Import(data);
        Skills.Import(data);
        Virus.Scan();
        Stream.SkillViewerBonus = cat => Skills.ViewerBonus(cat) + Pets.ViewerBonus();
        History.Clear();
        debuffTimer = 0f;
    }

    // Chạy đồng hồ game và kiểm tra debuff định kỳ (tạm dừng khi đang ở scene Menu)
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Menu") return;

        Clock.MinutesPerRealSecond = minutesPerRealSecond;
        Clock.Advance(Time.deltaTime);
        TickMining(Time.deltaTime);
        debuffTimer += Time.deltaTime;
        if (debuffTimer < debuffCheckSeconds) return;
        debuffTimer = 0f;
        Mood.Change(Needs.MoodDelta());
        if (Economy.IsPoor) Mood.Change(-SurvivalNeeds.DebuffMoodPenalty);
    }

    void TickMining(float dt)
    {
        MiningRig[] rigs = FindObjectsByType<MiningRig>(FindObjectsSortMode.None);
        float power = 0f;
        for (int i = 0; i < rigs.Length; i++)
        {
            if (rigs[i] != null && rigs[i].IsRunning)
                power += rigs[i].MiningPower;
        }
        if (power <= 0f) return;
        Router router = FindFirstObjectByType<Router>();
        bool hasRouter = router != null && router.Data != null && router.Data.uploadMbps > 0f;
        Crypto.Tick(dt, power, hasRouter);
    }

    void OnMinuteTick()
    {
        float kw = Stream.IsOpen ? 0.45f : 0.08f;
        MiningRig[] rigs = FindObjectsByType<MiningRig>(FindObjectsSortMode.None);
        for (int i = 0; i < rigs.Length; i++)
        {
            if (rigs[i] != null && rigs[i].IsRunning)
                kw += 0.75f * rigs[i].MiningPower;
        }
        Electricity.OnMinutePassed(kw);
    }

    void OnHourTick()
    {
        FindFirstObjectByType<CleaningSystem>()?.OnHourPassed();
        Workers.OnWave();
    }

    // Qua ngày mới: phát hóa đơn và tự động lưu
    void OnDayPassed()
    {
        Economy.IssueDailyBills(Clock.Day);
        Electricity.OnDayPassed(Economy, Clock.Day);
        Mail.OnDayPassed();
        Workers.OnDayPassed();
        Raffle.OnDayPassed();
        Gtu.OnDayPassed();
        FindFirstObjectByType<LuckySpin>()?.ResetDailyFreeSpin();
        Achievements.Evaluate();
        if (autoSaveEachDay) Save();
    }

    // Lưu bản ghi stream vào lịch sử khi đóng stream
    void OnStreamClosed(StreamRecord record)
    {
        if (record != null)
        {
            History.Add(record);
            Skills?.AddPoints(1);
            Achievements.Evaluate();
        }
    }

    // Người chơi đi ngủ: tua tới giờ thức dậy, reset giờ thức và kiểm tra bệnh
    public void SleepUntilMorning(int wakeUpHour = 8)
    {
        if (Stream.IsOpen) Stream.Close();
        Needs.Sleep();
        Clock.SkipToHour(wakeUpHour);
        Needs.RollSickness(Clock.Day, HouseCleanliness, Mood.Value);
        if (autoSaveEachDay) Save();
    }

    // Ghi toàn bộ trạng thái game xuống file save
    public void Save()
    {
        var data = new SaveData
        {
            day = Clock.Day,
            hour = Clock.Hour,
            minute = Clock.Minute,
            mood = Mood.Value,
            history = new List<StreamRecord>(History)
        };
        Needs.Export(data);
        Economy.Export(data);
        Stream.Export(data);
        Inventory.Export(data);
        Orders.Export(data);
        Skills.Export(data);

        PcCase pcCase = FindFirstObjectByType<PcCase>();
        if (pcCase != null)
            pcCase.Export(data);
        else if (pendingSceneData?.installedPcPartIds != null)
            data.installedPcPartIds = new List<string>(pendingSceneData.installedPcPartIds);

        WorldItem[] worldItems = FindObjectsByType<WorldItem>(FindObjectsSortMode.None);
        if (worldItems.Length > 0 || SceneManager.GetActiveScene().name != "Menu")
        {
            data.placedItems = new List<PlacedItemRecord>();
            for (int i = 0; i < worldItems.Length; i++)
            {
                WorldItem wi = worldItems[i];
                if (wi == null || wi.IsHeld || wi.Data == null || string.IsNullOrEmpty(wi.Data.id))
                    continue;
                data.placedItems.Add(new PlacedItemRecord
                {
                    itemId = wi.Data.id,
                    position = wi.transform.position,
                    eulerAngles = wi.transform.eulerAngles
                });
            }
        }
        else if (pendingSceneData?.placedItems != null)
        {
            data.placedItems = new List<PlacedItemRecord>(pendingSceneData.placedItems);
        }

        SaveService.Save(data);
    }

    // Tải trạng thái game từ file save, trả về false nếu chưa có save
    public bool Load()
    {
        SaveData data = SaveService.Load();
        if (data == null) return false;
        pendingSceneData = data;
        Clock.Set(data.day, data.hour, data.minute);
        Mood.Set(data.mood);
        Needs.Import(data);
        Economy.Import(data);
        Stream.Import(data);
        Inventory.Import(data);
        Orders.Import(data);
        Skills.Import(data);
        Stream.SkillViewerBonus = Skills.ViewerBonus;
        History = data.history ?? new List<StreamRecord>();
        ApplySceneState(data);
        return true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Menu" && pendingSceneData != null)
            ApplySceneState(pendingSceneData);
    }

    // Khôi phục linh kiện PC và các vật phẩm đã đặt trong phòng
    void ApplySceneState(SaveData data)
    {
        if (data == null || SceneManager.GetActiveScene().name == "Menu") return;

        PcCase pcCase = FindFirstObjectByType<PcCase>();
        if (pcCase != null && data.installedPcPartIds != null && data.installedPcPartIds.Count > 0)
            pcCase.Import(data);

        if (data.placedItems != null)
        {
            WorldItem[] existing = FindObjectsByType<WorldItem>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && !existing[i].IsHeld)
                    Destroy(existing[i].gameObject);
            }

            for (int i = 0; i < data.placedItems.Count; i++)
            {
                PlacedItemRecord rec = data.placedItems[i];
                ItemData itemData = ItemData.FindById(rec.itemId);
                if (itemData != null)
                    WorldItem.Spawn(itemData, rec.position, Quaternion.Euler(rec.eulerAngles));
            }
        }

        FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
    }
}
