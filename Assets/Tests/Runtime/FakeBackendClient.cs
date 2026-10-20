#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Backend giả lập offline mô phỏng luật slot_game.lua, cho phép ép kết quả để test chạy ổn định và không tốn xu thật
public class FakeBackendClient : IBackendClient
{
    // Kết quả ép cho các lượt quay
    public enum Outcome
    {
        Random,
        Lose,
        Win,
        Jackpot,
    }

    [Tooltip("Kết quả ép cho các lượt quay tiếp theo")]
    public Outcome NextOutcome = Outcome.Lose;

    [Tooltip("Độ trễ mạng giả lập (ms) cho mỗi lệnh")]
    public int LatencyMs = 60;

    [Tooltip("Thời gian tự dừng (giây) trả về khi bắt đầu quay, giống server")]
    public float AutoStopSeconds = 5f;

    [Tooltip("Hôm nay người chơi đã nhận thưởng điểm danh hay chưa")]
    public bool DailyClaimedToday = true;

    [Tooltip("Số xu thưởng điểm danh hôm nay")]
    public long DailyRewardCoins = 200;

    [Tooltip("Số xu hiện có trên server giả lập")]
    public long Coins;

    [Tooltip("Số kim cương hiện có trên server giả lập")]
    public long Diamonds;

    [Tooltip("Tiến trình người chơi lưu trên server giả lập")]
    public PlayerData Player;

    [Tooltip("Số lượt quay đã bắt đầu thành công")]
    public int SpinStartCount;

    [Tooltip("Số lệnh dừng đã xử lý")]
    public int SpinStopCount;

    [Tooltip("Số lần client lưu tiến trình")]
    public int SaveCount;

    [Tooltip("Bộ sinh số ngẫu nhiên có seed cố định để mỗi lần chạy test giống nhau")]
    readonly System.Random rng = new System.Random(20260101);

    [Tooltip("Lượt quay đang mở")]
    ActiveSpin active;

    // Trạng thái một lượt quay đang mở
    class ActiveSpin
    {
        public string id;
        public int level;
        public int reelCount;
        public long cost;
        public int[][] layouts;
        public int[] startFaces;
    }

    // Thông số quay khớp hằng số trong slot_game.lua
    const double SpinSpeed = 5.0;
    const double SpinUp = 0.3;
    const double StartInterval = 0.03;
    const double StopInterval = 0.2;
    const double SevenHitChance = 0.10;

    [Tooltip("Gói đổi kim cương sang xu, khớp handle_shop_exchange")]
    static readonly Dictionary<string, (long diamonds, long coins)> ShopPacks = new()
    {
        { "coin_pack_small", (10, 500) },
        { "coin_pack_medium", (50, 3000) },
        { "coin_pack_large", (100, 7500) },
    };

    [Tooltip("Gói IAP kim cương, khớp handle_iap_grant")]
    static readonly Dictionary<string, long> IapPacks = new()
    {
        { "diamond_100", 100 },
        { "diamond_550", 550 },
        { "diamond_1200", 1200 },
        { "diamond_2600", 2600 },
        { "diamond_7000", 7000 },
    };

    // Tạo server giả lập với số dư và tiến trình ban đầu
    public FakeBackendClient(long coins = 1000, long diamonds = 50, int level = 1, int highestLevel = 1)
    {
        Coins = coins;
        Diamonds = diamonds;
        level = Mathf.Max(1, level);
        Player = new PlayerData
        {
            playerId = "autotest",
            coins = coins,
            diamonds = diamonds,
            currentLevel = level,
            highestLevel = Mathf.Max(level, highestLevel),
        };
    }

    // Trừ phí, xáo hình từng reel giữ hình đang hiện và trả quỹ đạo quay như handle_spin_start
    public async Task<SpinStartResult> SpinStartAsync(int level, string sessionId, int[] faces, int[] symbols)
    {
        await Delay();
        level = Mathf.Max(1, level);
        int reelCount = LevelGenerator.GetSlotCount(level);
        long cost = LevelGenerator.GetSpinCost(level);
        if (Coins < cost) return null;
        Coins -= cost;
        SpinStartCount++;

        int[][] layouts = new int[reelCount][];
        int[] startFaces = new int[reelCount];
        for (int r = 0; r < reelCount; r++)
        {
            int face = faces != null && r < faces.Length ? Mathf.Clamp(faces[r], 0, SymbolData.Count - 1) : rng.Next(0, SymbolData.Count);
            int keep = symbols != null && r < symbols.Length ? symbols[r] : -1;
            layouts[r] = ShuffledLayout(face, keep);
            startFaces[r] = face;
        }

        active = new ActiveSpin
        {
            id = Guid.NewGuid().ToString("N"),
            level = level,
            reelCount = reelCount,
            cost = cost,
            layouts = layouts,
            startFaces = startFaces,
        };

        return new SpinStartResult
        {
            spinId = active.id,
            sessionId = active.id,
            sessionExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1800,
            reelLayouts = layouts,
            startFaces = startFaces,
            speed = SpinSpeed,
            spinUp = SpinUp,
            startInterval = StartInterval,
            stopInterval = StopInterval,
            autoStopSeconds = AutoStopSeconds,
            coins = Coins,
        };
    }

    // Chốt mặt dừng từng reel theo thời điểm bấm như handle_spin_stop, NextOutcome khác Random thì ép kết quả cho test
    public async Task<SpinStopResult> SpinStopAsync(string spinId, int reelIndex, int elapsedMs, bool isAuto = false)
    {
        await Delay();
        if (active == null || active.id != spinId) throw new InvalidOperationException("session_not_found");
        SpinStopCount++;

        int count = active.reelCount;
        int[] faces = new int[count];
        int forced = NextOutcome switch
        {
            Outcome.Win => rng.Next(0, SymbolData.SevenId),
            Outcome.Jackpot => SymbolData.SevenId,
            _ => -1,
        };
        for (int r = 0; r < count; r++)
            faces[r] = forced >= 0 ? Array.IndexOf(active.layouts[r], forced) : FaceAtStop(r, elapsedMs / 1000.0);

        int[] symbols = new int[count];
        for (int r = 0; r < count; r++) symbols[r] = SymbolOf(r, faces[r]);

        if (NextOutcome == Outcome.Lose && count > 1 && Array.TrueForAll(symbols, s => s == symbols[0]))
        {
            faces[count - 1] = (faces[count - 1] + 1) % SymbolData.Count;
            symbols[count - 1] = SymbolOf(count - 1, faces[count - 1]);
        }

        bool allMatch = Array.TrueForAll(symbols, s => s == symbols[0]);
        SpinOutcome outcome = !allMatch ? SpinOutcome.None : symbols[0] == SymbolData.SevenId ? SpinOutcome.Bingo777 : SpinOutcome.BingoXXX;
        long reward = outcome switch
        {
            SpinOutcome.Bingo777 => active.cost * LevelGenerator.JackpotMultiplier,
            SpinOutcome.BingoXXX => active.cost * LevelGenerator.BingoMultiplier,
            _ => 0,
        };
        Coins += reward;
        int nextLevel = allMatch ? active.level + 1 : active.level;
        active = null;

        return new SpinStopResult
        {
            reelIndex = 0,
            symbolId = symbols[0],
            faces = faces,
            symbols = symbols,
            isFinal = true,
            result = new SpinResult
            {
                outcome = outcome,
                reward = reward,
                coins = Coins,
                level = nextLevel,
                levelCleared = allMatch,
            },
        };
    }

    // Hủy lượt quay đang mở
    public async Task FleeSessionAsync(string sessionId = null)
    {
        await Delay();
        active = null;
    }

    // Thu hoạch máy sinh xu (giả lập không cộng thêm)
    public async Task<WalletResult> GeneratorClaimAsync()
    {
        await Delay();
        return WalletNow();
    }

    // Cộng 100 xu khi xem quảng cáo như handle_ad_reward
    public async Task<WalletResult> AdRewardAsync(string adToken)
    {
        await Delay();
        Coins += 100;
        return WalletNow();
    }

    // Đổi kim cương sang xu như handle_shop_exchange
    public async Task<WalletResult> ShopExchangeAsync(string packId)
    {
        await Delay();
        if (!ShopPacks.TryGetValue(packId ?? "", out var pack)) throw new InvalidOperationException("invalid_shop_pack");
        if (Diamonds < pack.diamonds) throw new InvalidOperationException("not_enough_diamonds");
        Diamonds -= pack.diamonds;
        Coins += pack.coins;
        return WalletNow();
    }

    // Cấp kim cương IAP như handle_iap_grant
    public async Task<WalletResult> IapVerifyAsync(string store, string productId, string receipt)
    {
        await Delay();
        if (!IapPacks.TryGetValue(productId ?? "", out long amount)) throw new InvalidOperationException("invalid_iap_product");
        Diamonds += amount;
        return WalletNow();
    }

    // Trả bản sao tiến trình người chơi kèm số dư hiện tại
    public async Task<PlayerData> LoadPlayerAsync()
    {
        await Delay();
        Player.coins = Coins;
        Player.diamonds = Diamonds;
        return JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(Player));
    }

    // Lưu bản sao tiến trình người chơi
    public async Task SavePlayerAsync(PlayerData data, int xpDelta = 0)
    {
        await Delay();
        if (data != null) Player = JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(data));
        SaveCount++;
    }

    // Đồng bộ ví từ server giả lập
    public async Task<WalletResult> RefreshWalletAsync()
    {
        await Delay();
        WalletResult wallet = WalletNow();
        Wallet.Set(wallet);
        return wallet;
    }

    // Trả trạng thái điểm danh hôm nay
    public async Task<DailyLoginStatus> GetDailyLoginAsync()
    {
        await Delay();
        return new DailyLoginStatus
        {
            assignmentId = "autotest-daily",
            day = 1,
            status = DailyClaimedToday ? "claimed" : "assigned",
            claimedToday = DailyClaimedToday,
            streak = 1,
            resetAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
            rewards = new[] { new DailyRewardItem { currency = CurrencyType.Coins, amount = DailyRewardCoins } },
        };
    }

    // Nhận thưởng điểm danh hôm nay và cập nhật ví
    public async Task<DailyRewardItem[]> ClaimDailyLoginAsync(DailyLoginStatus status)
    {
        await Delay();
        if (DailyClaimedToday) throw new InvalidOperationException("already_claimed");
        DailyClaimedToday = true;
        Coins += DailyRewardCoins;
        Wallet.Set(WalletNow());
        return new[] { new DailyRewardItem { currency = CurrencyType.Coins, amount = DailyRewardCoins } };
    }

    // Chờ độ trễ mạng giả lập
    Task Delay() => LatencyMs > 0 ? Task.Delay(LatencyMs) : Task.CompletedTask;

    // Số dư hiện tại dạng WalletResult
    WalletResult WalletNow() => new WalletResult { coins = Coins, diamonds = Diamonds };

    // Xáo trộn 7 biểu tượng, giữ keepSymbol ở mặt keepFace như build_reel_layout
    int[] ShuffledLayout(int keepFace, int keepSymbol)
    {
        int[] faces = new int[SymbolData.Count];
        for (int i = 0; i < faces.Length; i++) faces[i] = i;
        for (int i = faces.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (faces[i], faces[j]) = (faces[j], faces[i]);
        }
        int at = Array.IndexOf(faces, keepSymbol);
        if (at >= 0) (faces[at], faces[keepFace]) = (faces[keepFace], faces[at]);
        return faces;
    }

    // Mặt dừng của reel r khi bấm ở giây elapsed, số 7 có thể trượt sang ô bên cạnh như handle_spin_stop
    int FaceAtStop(int r, double elapsed)
    {
        double activeTime = elapsed + r * StopInterval - r * StartInterval;
        double distance = activeTime <= 0 ? 0
            : activeTime <= SpinUp ? SpinSpeed * 0.5 * activeTime * activeTime / SpinUp
            : SpinSpeed * (0.5 * SpinUp + (activeTime - SpinUp));
        int face = (int)(((long)Math.Floor(active.startFaces[r] + distance + 0.5)) % SymbolData.Count);
        if (SymbolOf(r, face) == SymbolData.SevenId && rng.NextDouble() >= SevenHitChance)
            face = (face + (rng.Next(0, 2) == 0 ? SymbolData.Count - 1 : 1)) % SymbolData.Count;
        return face;
    }

    // Biểu tượng ở mặt face của reel r trong lượt đang mở
    int SymbolOf(int r, int face) => active.layouts[r][face];
}
#endif
