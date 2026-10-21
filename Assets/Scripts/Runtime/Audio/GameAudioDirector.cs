using UnityEngine;
using UnityEngine.SceneManagement;

// Nối sự kiện gameplay với âm thanh: nhạc và âm môi trường theo scene, khu vực, trạng thái stream,
// cùng các SFX cho stream, donate, tiền, hóa đơn, giao hàng, kỹ năng. Tự gắn kèm AudioManager.
public class GameAudioDirector : MonoBehaviour
{
    const string MenuScene = "Menu";
    const string MenuMusic = "bgm_menu_theme";
    const string HomeMusic = "bgm_home_theme";
    const string StreetMusic = "bgm_downtown";
    const string RoomAmbience = "amb_room";
    const string StreetAmbience = "amb_street";
    const string StreamAmbience = "amb_pc_fan";

    static readonly string[] DonateSounds =
    {
        "donate_boink", "donate_huh", "donate_knock", "donate_laugh", "donate_meow",
        "donate_oof", "donate_sus", "donate_usb", "donate_winerror", "donate_woof"
    };

    const float AreaCheckSeconds = 0.5f; // Chu kỳ kiểm tra người chơi ở trong nhà hay ngoài phố

    bool outdoors;
    bool inCasinoArea;
    float areaTimer;
    Transform door;
    Transform player;
    Transform casinoAnchor;

    GameWorld subscribedWorld;
    float lastMoney;
    int lastSkillPoints;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Unsubscribe();
    }

    void Start()
    {
        ApplySceneAudio(SceneManager.GetActiveScene());
    }

    // GameWorld có thể được tạo sau AudioManager nên chờ tới khi có rồi mới đăng ký sự kiện
    void Update()
    {
        if (subscribedWorld == null && GameWorld.HasInstance)
            Subscribe(GameWorld.Instance);

        areaTimer += Time.unscaledDeltaTime;
        if (areaTimer < AreaCheckSeconds) return;
        areaTimer = 0f;
        CheckArea();
    }

    // Người chơi đi qua cửa nhà (phía +Z của cửa) là ra ngoài phố, hoặc vào khu Casino thì đổi nhạc và âm môi trường
    void CheckArea()
    {
        if (door == null || player == null) return;
        bool isOutdoors = player.position.z > door.position.z + 0.3f;
        bool nearCasino = isOutdoors && casinoAnchor != null && (player.position - casinoAnchor.position).sqrMagnitude < 18f * 18f;
        if (isOutdoors == outdoors && nearCasino == inCasinoArea) return;
        outdoors = isOutdoors;
        inCasinoArea = nearCasino;
        ApplyGameplayAudio();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneAudio(scene);
    }

    void ApplySceneAudio(Scene scene)
    {
        AudioManager audio = AudioManager.Instance;
        audio.SetGameplayPaused(false);
        if (scene.name == MenuScene)
        {
            outdoors = false;
            audio.PlayMusic(MenuMusic);
            audio.StopAmbience();
            return;
        }
        if (scene.name.Contains("Loading")) return;

        door = FindFirstObjectByType<Door>()?.transform;
        CasinoSlotMachine slot = FindFirstObjectByType<CasinoSlotMachine>();
        casinoAnchor = slot != null ? slot.transform : null;
        PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
        player = motor != null ? motor.transform : null;
        outdoors = false;
        inCasinoArea = false;
        CheckArea();

        AddFootsteps();
        ApplyGameplayAudio();
    }

    // Chọn nhạc và âm môi trường theo khu vực và việc đang stream hay không
    void ApplyGameplayAudio()
    {
        if (SceneManager.GetActiveScene().name == MenuScene) return;
        AudioManager audio = AudioManager.Instance;

        if (outdoors)
        {
            audio.PlayMusic(inCasinoArea ? "bgm_casino" : StreetMusic);
            audio.PlayAmbience(inCasinoArea ? "amb_casino" : StreetAmbience);
            return;
        }

        bool streaming = GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen;
        audio.PlayMusic(streaming ? StreamMusic(GameWorld.Instance.Stream.Category) : HomeMusic);
        audio.PlayAmbience(streaming ? StreamAmbience : RoomAmbience);
    }

    static string StreamMusic(StreamCategory category)
    {
        switch (category)
        {
            case StreamCategory.Gaming: return "bgm_cyberpunk";
            case StreamCategory.Asmr: return "bgm_asmr";
            case StreamCategory.Dancing: return "bgm_nightclub";
            case StreamCategory.Pool: return "bgm_downtown";
            default: return "bgm_chill";
        }
    }

    // Gắn âm bước chân cho người chơi nếu scene chưa có
    static void AddFootsteps()
    {
        PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
        if (motor != null && motor.GetComponent<FootstepAudio>() == null)
            motor.gameObject.AddComponent<FootstepAudio>();
    }

    void Subscribe(GameWorld world)
    {
        subscribedWorld = world;
        lastMoney = world.Economy.Money;
        lastSkillPoints = world.Skills.Points;

        world.Stream.Opened += OnStreamOpened;
        world.Stream.Closed += OnStreamClosed;
        world.Stream.Donated += OnDonated;
        world.Economy.MoneyChanged += OnMoneyChanged;
        world.Economy.BillIssued += OnBillIssued;
        world.Orders.Delivered += OnOrderDelivered;
        world.Needs.SickChanged += OnSickChanged;
        world.Skills.PointsChanged += OnSkillPointsChanged;
        world.Skills.SkillLearned += OnSkillLearned;
        world.Cards.CardResolved += OnCardResolved;
    }

    void Unsubscribe()
    {
        if (subscribedWorld == null) return;
        GameWorld world = subscribedWorld;
        world.Stream.Opened -= OnStreamOpened;
        world.Stream.Closed -= OnStreamClosed;
        world.Stream.Donated -= OnDonated;
        world.Economy.MoneyChanged -= OnMoneyChanged;
        world.Economy.BillIssued -= OnBillIssued;
        world.Orders.Delivered -= OnOrderDelivered;
        world.Needs.SickChanged -= OnSickChanged;
        world.Skills.PointsChanged -= OnSkillPointsChanged;
        world.Skills.SkillLearned -= OnSkillLearned;
        world.Cards.CardResolved -= OnCardResolved;
        subscribedWorld = null;
    }

    void OnStreamOpened()
    {
        AudioManager.Play("stream_start");
        ApplyGameplayAudio();
    }

    void OnStreamClosed(StreamRecord record)
    {
        AudioManager.Play("stream_stop");
        ApplyGameplayAudio();
    }

    // Donate: chuông báo kèm một âm meme ngẫu nhiên như các stream thật
    void OnDonated(float amount)
    {
        AudioManager.Play("donate_notify");
        AudioManager.Play(DonateSounds[Random.Range(0, DonateSounds.Length)]);
    }

    // Tiền giảm (mua hàng, trả hóa đơn) thì kêu tiếng máy tính tiền; tiền tăng từ donate đã có âm riêng
    void OnMoneyChanged(float money)
    {
        if (InGameplay && money < lastMoney - 0.01f)
            AudioManager.Play("cash_beep");
        lastMoney = money;
    }

    // Tải save hoặc chơi mới ở Menu cũng đổi tiền, điểm kỹ năng; những lúc đó không phát âm
    static bool InGameplay => SceneManager.GetActiveScene().name != MenuScene;

    void OnBillIssued(Bill bill)
    {
        AudioManager.PlayUi("ui_notification");
    }

    void OnOrderDelivered(Order order)
    {
        AudioManager.Play("door_bell");
    }

    void OnSickChanged(bool sick)
    {
        if (sick) AudioManager.PlayUi("ui_alert");
    }

    void OnSkillPointsChanged()
    {
        int points = subscribedWorld.Skills.Points;
        if (InGameplay && points > lastSkillPoints)
            AudioManager.PlayUi("level_up");
        lastSkillPoints = points;
    }

    void OnSkillLearned(SkillId id)
    {
        AudioManager.PlayUi("skill_unlock");
    }

    void OnCardResolved(StreamCardData card, bool success)
    {
        AudioManager.PlayUi(success ? "ui_success" : "ui_error");
    }
}
