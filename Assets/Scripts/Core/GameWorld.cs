using System.Collections.Generic;
using UnityEngine;

// Gốc của game: tạo các hệ thống, nối sự kiện, chạy đồng hồ và lưu game
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
            return instance;
        }
    }

    // Đã có GameWorld còn sống hay chưa, không tự tạo mới
    public static bool HasInstance => instance != null;

    [Tooltip("Số phút trong game trôi qua mỗi giây thực")]
    [SerializeField] float minutesPerRealSecond = 1f;

    [Tooltip("Chu kỳ kiểm tra debuff sinh tồn (giây thực)")]
    [SerializeField] float debuffCheckSeconds = 5f;

    [Tooltip("Tự động lưu mỗi khi qua ngày mới")]
    [SerializeField] bool autoSaveEachDay = true;

    public GameClock Clock { get; private set; }
    public MoodSystem Mood { get; private set; }
    public SurvivalNeeds Needs { get; private set; }
    public Economy Economy { get; private set; }
    public StreamSystem Stream { get; private set; }
    public List<StreamRecord> History { get; private set; } = new List<StreamRecord>();

    // Độ sạch nhà 0–1, do hệ thống dọn dẹp cập nhật sau này
    public float HouseCleanliness = 1f;

    System.Random rng;
    float debuffTimer;

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
    }

    // Tạo các hệ thống và nối sự kiện giữa chúng
    void CreateSystems()
    {
        rng = new System.Random();
        Clock = new GameClock { MinutesPerRealSecond = minutesPerRealSecond };
        Mood = new MoodSystem();
        Needs = new SurvivalNeeds(rng);
        Economy = new Economy();
        Stream = new StreamSystem(Clock, Mood, Economy, rng);

        Clock.MinutePassed += Stream.OnMinutePassed;
        Clock.HourPassed += Needs.OnHourPassed;
        Clock.DayPassed += OnDayPassed;
        Stream.Closed += OnStreamClosed;
    }

    // Chạy đồng hồ game và kiểm tra debuff định kỳ
    void Update()
    {
        Clock.Advance(Time.deltaTime);
        debuffTimer += Time.deltaTime;
        if (debuffTimer < debuffCheckSeconds) return;
        debuffTimer = 0f;
        Mood.Change(Needs.MoodDelta());
        if (Economy.IsPoor) Mood.Change(-SurvivalNeeds.DebuffMoodPenalty);
    }

    // Qua ngày mới: phát hóa đơn và tự động lưu
    void OnDayPassed()
    {
        Economy.IssueDailyBills(Clock.Day);
        if (autoSaveEachDay) Save();
    }

    // Lưu bản ghi stream vào lịch sử khi đóng stream
    void OnStreamClosed(StreamRecord record)
    {
        if (record != null) History.Add(record);
    }

    // Người chơi đi ngủ: tua tới 8h sáng, reset giờ thức và kiểm tra bệnh
    public void SleepUntilMorning()
    {
        if (Stream.IsOpen) Stream.Close();
        Needs.Sleep();
        Clock.SkipToHour(8);
        Needs.RollSickness(Clock.Day, HouseCleanliness, Mood.Value);
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
        SaveService.Save(data);
    }

    // Tải trạng thái game từ file save, trả về false nếu chưa có save
    public bool Load()
    {
        SaveData data = SaveService.Load();
        if (data == null) return false;
        Clock.Set(data.day, data.hour, data.minute);
        Mood.Set(data.mood);
        Needs.Import(data);
        Economy.Import(data);
        Stream.Import(data);
        History = data.history ?? new List<StreamRecord>();
        return true;
    }
}
