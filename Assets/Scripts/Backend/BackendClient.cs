using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaiGame.Services;
using UnityEngine;

// Giao diện gọi các dịch vụ trên SaiServer
public interface IBackendClient
{
    // Bắt đầu lượt quay qua Lua script trên server
    Task<SpinStartResult> SpinStartAsync(int level, string sessionId, int[] faces, int[] symbols);
    // Gửi lệnh dừng bước quay kèm thời gian quay (ms) lên Lua script để server tính góc xoay
    Task<SpinStopResult> SpinStopAsync(string spinId, int reelIndex, int elapsedMs, bool isAuto = false);
    // Hủy phiên quay đang mở trên server
    Task FleeSessionAsync(string sessionId = null);
    // Thu hoạch xu từ ItemGenerator trên server
    Task<WalletResult> GeneratorClaimAsync();
    // Nhận thưởng 100 xu sau khi xem quảng cáo qua Lua script
    Task<WalletResult> AdRewardAsync(string adToken);
    // Đổi kim cương sang xu qua module Shop hoặc Lua script của SaiServer
    Task<WalletResult> ShopExchangeAsync(string packId);
    // Xác thực và cấp kim cương IAP qua Lua script
    Task<WalletResult> IapVerifyAsync(string store, string productId, string receipt);
    // Tải tiến trình GamerProgress và số dư vật phẩm từ SaiServer
    Task<PlayerData> LoadPlayerAsync();
    // Lưu tiến trình người chơi lên GamerProgress
    Task SavePlayerAsync(PlayerData data, int xpDelta = 0);
    // Đồng bộ lại số dư xu và kim cương từ kho đồ trên server
    Task<WalletResult> RefreshWalletAsync();
    // Lấy trạng thái điểm danh hôm nay từ Daily Quest pool trên SaiServer
    Task<DailyLoginStatus> GetDailyLoginAsync();
    // Nhận thưởng điểm danh hôm nay qua chuỗi start → check → claim của SaiServer
    Task<DailyRewardItem[]> ClaimDailyLoginAsync(DailyLoginStatus status);
}

// Lớp thực thi kết nối trực tiếp với SaiServer SDK và Lua Script
public partial class BackendClient : IBackendClient
{
    public const string LuaScriptName = "slot_game"; // Tên script Lua trên SaiServer Dashboard
    public const string CoinItemCode = "GOLD"; // Mã Item Definition của vàng/xu trên Dashboard (phân biệt HOA/thường)
    public const string DiamondItemCode = "DIAMOND"; // Mã Item Definition của kim cương trên Dashboard (phân biệt HOA/thường)
    public const string GeneratorItemCode = "coin_generator"; // Mã Item Definition của máy phát xu 100 GOLD/phút
    public const string CoinItemDefId = "01a1478d-ac6a-719a-8fb0-af85cc66d5e2"; // UUID của GOLD trên Dashboard
    public const string DiamondItemDefId = "01a1478d-ac6e-71e3-a249-c4daa04d9825"; // UUID của DIAMOND trên Dashboard
    public const string GeneratorItemDefId = "01a1487f-7201-71b1-99e0-dab8b9c8f54f"; // UUID của coin_generator trên Dashboard
    const int StarterCoins = 300; // Số GOLD cấp lần đầu khi tạo tiến trình mới

    [Tooltip("pool_key của Daily Quest pool điểm danh trên Dashboard")]
    public const string DailyLoginPoolKey = "daily_login";

    [Tooltip("Tiền tố code_name của 7 quest điểm danh (daily_login_d1..d7)")]
    const string DailyLoginQuestPrefix = "daily_login_d";

    [Tooltip("Id của Daily Quest pool điểm danh đã tra được, dùng lại cho các lần gọi sau")]
    static string _dailyPoolId;

    static Task<bool> _authTask; // Tiến trình xác thực đang chạy để tránh gọi trùng lặp

    // Đảm bảo SaiServer đã đăng nhập (khôi phục phiên đã lưu, Google Login hoặc AutoLogin) trước khi gọi API
    async Task<bool> EnsureAuthenticatedAsync()
    {
        if (SaiServer.Instance == null)
        {
            Debug.LogError("[BackendClient] SaiServer prefab is missing in the scene.");
            return false;
        }
        if (SaiServer.Instance.IsAuthenticated)
        {
            await EnsureLuaScriptDeployedAsync();
            return true;
        }

        if (_authTask != null) return await _authTask;
        _authTask = AuthenticateInternalAsync();
        try
        {
            return await _authTask;
        }
        finally
        {
            _authTask = null;
        }
    }

    const string GuestUsernamePrefKey = "saigame.guest.username"; // Khóa lưu tên tài khoản khách tự sinh theo máy
    const string GuestPasswordPrefKey = "saigame.guest.password"; // Khóa lưu mật khẩu tài khoản khách tự sinh theo máy

    // Thực thi luồng xác thực với SaiServer (tự động khôi phục token hoặc tự tạo tài khoản Guest ngầm)
    async Task<bool> AuthenticateInternalAsync()
    {
        SaiAuth auth = SaiServer.Instance.SaiAuth;
        if (auth == null) return false;

        var tcs = new TaskCompletionSource<bool>();
        void OnOk(LoginResponse _) => tcs.TrySetResult(true);
        void OnProfileOk(UserData _) => tcs.TrySetResult(true);

        auth.OnLoginSuccess += OnOk;
        auth.OnGetProfileSuccess += OnProfileOk;

        int timeoutMs = 15000;
        try
        {
            if (auth.HasAccessToken)
            {
                auth.GetMyProfile(
                    _ => tcs.TrySetResult(true),
                    _ => auth.RefreshAuthToken(
                        __ => auth.GetMyProfile(___ => tcs.TrySetResult(true), ___ => FallbackLogin(auth, tcs)),
                        __ => FallbackLogin(auth, tcs)));
            }
            else if (!string.IsNullOrEmpty(auth.RefreshToken))
            {
                auth.RefreshAuthToken(
                    _ => auth.GetMyProfile(__ => tcs.TrySetResult(true), __ => FallbackLogin(auth, tcs)),
                    _ => FallbackLogin(auth, tcs));
            }
            else if (!string.IsNullOrEmpty(auth.GetUsername()) && !string.IsNullOrEmpty(auth.GetPassword()))
            {
                auth.Login(
                    auth.GetUsername(),
                    auth.GetPassword(),
                    _ => tcs.TrySetResult(true),
                    _ => FallbackLogin(auth, tcs));
            }
            else
            {
                FallbackLogin(auth, tcs);
            }

            Task completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            bool ok = completed == tcs.Task && tcs.Task.Result && SaiServer.Instance.IsAuthenticated;
            if (ok) await EnsureLuaScriptDeployedAsync();
            return ok;
        }
        finally
        {
            auth.OnLoginSuccess -= OnOk;
            auth.OnGetProfileSuccess -= OnProfileOk;
        }
    }

    // Hết cách khôi phục phiên: tài khoản thật thì báo lỗi, còn lại đăng nhập Guest theo thiết bị
    void FallbackLogin(SaiAuth auth, TaskCompletionSource<bool> tcs)
    {
        if (SavedAuthMode == AuthMode.Account || SavedAuthMode == AuthMode.Google) tcs.TrySetResult(false);
        else LoginOrRegisterGuest(auth, tcs);
    }

    // Tự động đăng nhập hoặc đăng ký ngầm tài khoản Guest theo thiết bị (người chơi không cần tự đăng ký)
    void LoginOrRegisterGuest(SaiAuth auth, TaskCompletionSource<bool> tcs)
    {
        string savedUser = PlayerPrefs.GetString(GuestUsernamePrefKey, "");
        string savedPass = PlayerPrefs.GetString(GuestPasswordPrefKey, "");

        if (!string.IsNullOrEmpty(savedUser) && !string.IsNullOrEmpty(savedPass))
        {
            auth.Login(
                savedUser,
                savedPass,
                _ => tcs.TrySetResult(true),
                _ => RegisterNewGuest(auth, tcs));
        }
        else
        {
            RegisterNewGuest(auth, tcs);
        }
    }

    // Sinh thông tin tài khoản Guest mới, đăng ký lên SaiServer rồi đăng nhập tự động
    void RegisterNewGuest(SaiAuth auth, TaskCompletionSource<bool> tcs)
    {
        string suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
        string guestUser = $"player_{suffix}";
        // Server từ chối đuôi .local, example.com là tên miền dành riêng (RFC 2606) nên không gửi nhầm mail cho ai
        string guestEmail = $"{guestUser}@example.com";
        string guestPass = $"Sg!{Guid.NewGuid():N}".Substring(0, 16);

        auth.Register(
            guestEmail,
            guestUser,
            guestPass,
            _ =>
            {
                PlayerPrefs.SetString(GuestUsernamePrefKey, guestUser);
                PlayerPrefs.SetString(GuestPasswordPrefKey, guestPass);
                PlayerPrefs.Save();
                auth.Login(
                    guestUser,
                    guestPass,
                    __ => tcs.TrySetResult(true),
                    __ => tcs.TrySetResult(false));
            },
            _ => tcs.TrySetResult(false));
    }

    static bool _luaScriptSynced; // Đánh dấu đã đồng bộ script Lua lên SaiServer trong phiên chạy Editor
    static Task _luaSyncTask; // Tiến trình đồng bộ script Lua đang chạy để tránh gọi trùng lặp

    // Tự động kiểm tra và đẩy file Server/lua/slot_game.lua lên SaiServer khi chạy trong Unity Editor
    async Task EnsureLuaScriptDeployedAsync()
    {
#if UNITY_EDITOR
        if (_luaScriptSynced) return;
        if (_luaSyncTask != null)
        {
            await _luaSyncTask;
            return;
        }
        _luaSyncTask = SyncLuaScriptInternalAsync();
        try
        {
            await _luaSyncTask;
        }
        finally
        {
            // Chỉ thử một lần mỗi phiên, tài khoản người chơi thường không có quyền đọc danh sách script
            _luaScriptSynced = true;
            _luaSyncTask = null;
        }
#else
        await Task.CompletedTask;
#endif
    }

#if UNITY_EDITOR
    // Đọc file Server/lua/slot_game.lua và tạo mới hoặc cập nhật lên SaiServer nếu nội dung thay đổi
    async Task SyncLuaScriptInternalAsync()
    {
        string scriptPath = System.IO.Path.GetFullPath($"Server/lua/{LuaScriptName}.lua");
        if (!System.IO.File.Exists(scriptPath)) return;

        string localBody = System.IO.File.ReadAllText(scriptPath);
        string gameId = SaiServer.Instance != null ? SaiServer.Instance.GameId : "";
        if (string.IsNullOrWhiteSpace(gameId)) return;

        string listEndpoint = $"/api/v1/games/{gameId}/scripts";
        var listTcs = new TaskCompletionSource<string>();
        SaiServer.Instance.StartCoroutine(SaiServer.Instance.GetRequest(
            listEndpoint,
            res => listTcs.TrySetResult(res),
            err => listTcs.TrySetResult(null)));

        string listJson = await listTcs.Task;
        if (string.IsNullOrWhiteSpace(listJson)) return;

        string existingId = null;
        string existingBody = null;
        bool existingActive = true;

        try
        {
            JToken parsed = JToken.Parse(listJson);
            JObject obj = parsed as JObject;
            JArray arr = parsed as JArray
                ?? obj?["scripts"] as JArray
                ?? obj?["data"] as JArray
                ?? obj?["items"] as JArray;

            if (arr != null)
            {
                foreach (JToken item in arr)
                {
                    if (item is not JObject itemObj) continue;
                    if (string.Equals(itemObj["name"]?.ToString(), LuaScriptName, StringComparison.Ordinal))
                    {
                        existingId = itemObj["id"]?.ToString();
                        existingBody = itemObj["script_body"]?.ToString();
                        existingActive = itemObj["is_active"]?.Value<bool>() ?? true;
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendClient] Parse script list warning: {e.Message}");
        }

        if (string.IsNullOrEmpty(existingId))
        {
            string createBody = JsonConvert.SerializeObject(new
            {
                name = LuaScriptName,
                description = "Server-authoritative Slot & Bingo game script",
                script_body = localBody
            });
            var createTcs = new TaskCompletionSource<bool>();
            SaiServer.Instance.StartCoroutine(SaiServer.Instance.PostRequest(
                listEndpoint,
                createBody,
                _ => createTcs.TrySetResult(true),
                err =>
                {
                    Debug.LogWarning($"[BackendClient] Auto-create Lua script '{LuaScriptName}' warning: {err}");
                    createTcs.TrySetResult(false);
                }));
            if (await createTcs.Task)
            {
                _luaScriptSynced = true;
                Debug.Log($"[BackendClient] Auto-created Lua script '{LuaScriptName}' on SaiServer.");
            }
        }
        else if (!string.Equals(existingBody, localBody, StringComparison.Ordinal) || !existingActive)
        {
            string updateEndpoint = $"{listEndpoint}/{existingId}";
            string updateBody = JsonConvert.SerializeObject(new
            {
                description = "Server-authoritative Slot & Bingo game script",
                script_body = localBody,
                is_active = true,
                is_library = false
            });
            var updateTcs = new TaskCompletionSource<bool>();
            SaiServer.Instance.StartCoroutine(SaiServer.Instance.PatchRequest(
                updateEndpoint,
                updateBody,
                _ => updateTcs.TrySetResult(true),
                err =>
                {
                    Debug.LogWarning($"[BackendClient] Auto-update Lua script '{LuaScriptName}' warning: {err}");
                    updateTcs.TrySetResult(false);
                }));
            if (await updateTcs.Task)
            {
                _luaScriptSynced = true;
                Debug.Log($"[BackendClient] Auto-updated Lua script '{LuaScriptName}' on SaiServer.");
            }
        }
        else
        {
            _luaScriptSynced = true;
        }
    }
#endif

    // Gọi một action trong script Lua trên SaiServer và trích xuất bảng output
    async Task<JObject> RunLuaActionAsync(object payloadObject)
    {
        if (!await EnsureAuthenticatedAsync())
            throw new InvalidOperationException("Not authenticated with SaiServer.");

        await EnsureLuaScriptDeployedAsync();

        BattleScript battleScript = SaiServer.Instance.BattleScript;
        if (battleScript == null)
            throw new InvalidOperationException("BattleScript component is missing on SaiServer.");

        string requestJson = JsonConvert.SerializeObject(new { payload = payloadObject });
        var tcs = new TaskCompletionSource<string>();

        battleScript.RunScript(
            overrideScriptName: LuaScriptName,
            overrideRequestBody: requestJson,
            onSuccess: res => tcs.TrySetResult(res),
            onError: err => tcs.TrySetException(new Exception(err)));

        string rawJson = await tcs.Task;
        if (string.IsNullOrWhiteSpace(rawJson))
            throw new Exception("Empty response from Lua script.");

        JObject root = JObject.Parse(rawJson);
        JToken outputToken = root["output"] ?? root["data"]?["output"] ?? root["data"] ?? root;
        JObject output = outputToken as JObject ?? root;

        string errMsg = output["error"]?.ToString() ?? root["error"]?.ToString();
        if (!string.IsNullOrEmpty(errMsg))
            throw new Exception(errMsg);

        return output;
    }

    // Đồng bộ số lượng GOLD và DIAMOND từ kho đồ PlayerItem trên SaiServer
    public async Task<WalletResult> RefreshWalletAsync()
    {
        if (!await EnsureAuthenticatedAsync() || SaiServer.Instance.PlayerItem == null)
            return new WalletResult { coins = Wallet.Coins, diamonds = Wallet.Diamonds };

        var tcs = new TaskCompletionSource<InventoryResponse>();
        SaiServer.Instance.PlayerItem.GetItems(
            limit: 100,
            offset: 0,
            category: "",
            onSuccess: inv => tcs.TrySetResult(inv),
            onError: err => tcs.TrySetException(new Exception(err)));

        InventoryResponse inventory = await tcs.Task;
        long coins = 0;
        long diamonds = 0;

        if (inventory?.items != null)
        {
            foreach (InventoryItemData item in inventory.items)
            {
                if (item == null) continue;
                string defId = item.item_definition_id ?? "";
                string code = item.definition?.item_code ?? "";
                if (defId == CoinItemDefId || string.Equals(code, CoinItemCode, StringComparison.OrdinalIgnoreCase))
                    coins += item.quantity;
                else if (defId == DiamondItemDefId || string.Equals(code, DiamondItemCode, StringComparison.OrdinalIgnoreCase))
                    diamonds += item.quantity;
            }
        }

        var wallet = new WalletResult { coins = coins, diamonds = diamonds };
        Wallet.Set(wallet);
        return wallet;
    }

    // Kiểm tra người chơi đã sở hữu máy phát xu coin_generator trong kho đồ chưa
    static bool HasGeneratorInLoadedInventory()
    {
        InventoryItemData[] items = SaiServer.Instance?.PlayerItem?.CurrentInventory?.items;
        if (items == null) return false;
        foreach (InventoryItemData item in items)
        {
            if (item == null) continue;
            if (item.item_definition_id == GeneratorItemDefId
                || string.Equals(item.definition?.item_code, GeneratorItemCode, StringComparison.Ordinal))
                return item.quantity > 0;
        }
        return false;
    }

    // Tải hoặc khởi tạo GamerProgress trên SaiServer và đồng bộ ví
    public async Task<PlayerData> LoadPlayerAsync()
    {
        if (!await EnsureAuthenticatedAsync()) return null;

        GamerProgress gp = SaiServer.Instance.GamerProgress;
        GamerProgressData progress = await GetProgressOrNullAsync(gp);
        bool isNewPlayer = false;

        if (progress == null || string.IsNullOrEmpty(progress.id))
        {
            progress = await CreateProgressAsync(gp);
            isNewPlayer = true;
        }

        WalletResult wallet = await RefreshWalletAsync();
        bool missingGenerator = !HasGeneratorInLoadedInventory();

        if (isNewPlayer || missingGenerator)
        {
            try
            {
                await RunLuaActionAsync(new
                {
                    action = "init_player",
                    coin_code = CoinItemCode,
                    generator_code = GeneratorItemCode,
                    starter_coins = isNewPlayer ? StarterCoins : 0,
                    grant_generator = missingGenerator
                });
                wallet = await RefreshWalletAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendClient] init_player script warning: {e.Message}");
            }
        }

        PlayerData data = ParsePlayerData(progress);
        data.coins = wallet.coins;
        data.diamonds = wallet.diamonds;

        if (SaiServer.Instance.ItemGenerator != null)
            SaiServer.Instance.ItemGenerator.GetGenerators();

        return data;
    }

    // Lưu dữ liệu PlayerData vào trường game_data của GamerProgress trên SaiServer
    public async Task SavePlayerAsync(PlayerData data, int xpDelta = 0)
    {
        if (data == null || !await EnsureAuthenticatedAsync()) return;
        GamerProgress gp = SaiServer.Instance.GamerProgress;
        if (gp == null || !gp.HasProgress) return;

        string json = JsonUtility.ToJson(data);
        var tcs = new TaskCompletionSource<GamerProgressData>();
        gp.UpdateProgress(
            experienceDelta: xpDelta,
            goldDelta: 0,
            newGameData: json,
            onSuccess: res => tcs.TrySetResult(res),
            onError: err => tcs.TrySetException(new Exception(err)));

        await tcs.Task;
    }

    // Gọi Lua action spin_start: trừ GOLD, tạo phiên 30 phút và lấy layout cùng thông số quay của từng reel từ SaiServer
    public async Task<SpinStartResult> SpinStartAsync(int level, string sessionId, int[] faces, int[] symbols)
    {
        JObject output = await RunLuaActionAsync(new
        {
            action = "spin_start",
            level = Mathf.Max(1, level),
            faces = faces ?? Array.Empty<int>(),
            symbols = symbols ?? Array.Empty<int>(),
            coin_code = CoinItemCode,
            coin_def_id = CoinItemDefId,
            prev_session_id = sessionId ?? ""
        });

        long cost = output["cost"]?.Value<long>() ?? LevelGenerator.GetSpinCost(level);
        long estimatedCoins = Math.Max(0L, Wallet.Coins - cost);
        _ = RefreshWalletAsync();

        string sid = output["session_id"]?.ToString() ?? output["spin_id"]?.ToString();

        return new SpinStartResult
        {
            spinId = sid,
            sessionId = sid,
            sessionExpiresAtUnix = output["session_expires_at_unix"]?.Value<long>() ?? 0L,
            reelLayouts = ReadReelLayouts(output["layouts"]),
            startFaces = output["start_faces"]?.ToObject<int[]>(),
            speed = output["speed"]?.Value<double>() ?? 5.0,
            spinUp = output["spin_up"]?.Value<double>() ?? 0.3,
            startInterval = output["start_interval"]?.Value<double>() ?? 0.03,
            stopInterval = output["stop_interval"]?.Value<double>() ?? 0.2,
            autoStopSeconds = output["auto_stop_seconds"]?.Value<float>() ?? 5f,
            coins = estimatedCoins
        };
    }

    // Đọc layout từng reel, bỏ qua nếu server còn chạy script cũ trả mảng phẳng
    static int[][] ReadReelLayouts(JToken token)
    {
        if (token is not JArray arr || arr.Count == 0 || arr[0] is not JArray) return null;
        return arr.ToObject<int[][]>();
    }

    // Gửi thời điểm bấm dừng elapsedMs để Lua script chốt mặt dừng từng reel và chấm Bingo
    public async Task<SpinStopResult> SpinStopAsync(string spinId, int reelIndex, int elapsedMs, bool isAuto = false)
    {
        JObject output = await RunLuaActionAsync(new
        {
            action = "spin_stop",
            session_id = spinId ?? "",
            spin_id = spinId ?? "",
            reel_index = reelIndex,
            elapsed_ms = Mathf.Max(0, elapsedMs),
            is_auto = isAuto,
            coin_code = CoinItemCode,
            coin_def_id = CoinItemDefId
        });

        bool isFinal = output["is_final"]?.Value<bool>() ?? true;
        int[] symbols = output["symbols"]?.ToObject<int[]>();
        int symbolId = output["symbol_id"]?.Value<int>()
            ?? (symbols != null && symbols.Length > 0 ? symbols[0] : 0);
        var stopResult = new SpinStopResult
        {
            reelIndex = output["reel_index"]?.Value<int>() ?? reelIndex,
            symbolId = symbolId,
            faces = output["faces"]?.ToObject<int[]>(),
            symbols = symbols,
            isFinal = isFinal
        };

        if (isFinal)
        {
            long reward = output["reward"]?.Value<long>() ?? 0L;
            long estimatedCoins = Wallet.Coins + reward;
            _ = RefreshWalletAsync();

            int outcomeInt = output["outcome"]?.Value<int>() ?? 0;
            stopResult.result = new SpinResult
            {
                outcome = (SpinOutcome)Mathf.Clamp(outcomeInt, 0, 2),
                reward = reward,
                coins = estimatedCoins,
                level = output["level"]?.Value<int>() ?? 1,
                levelCleared = output["level_cleared"]?.Value<bool>() ?? false
            };
        }

        return stopResult;
    }

    // Đóng phiên quay hiện tại trên SaiServer khi hết giờ hoặc thoát màn
    public async Task FleeSessionAsync(string sessionId = null)
    {
        try
        {
            await RunLuaActionAsync(new
            {
                action = "flee",
                session_id = sessionId ?? ""
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendClient] FleeSession warning: {e.Message}");
        }
    }

    // Thu hoạch GOLD định kỳ từ module ItemGenerator của SaiServer
    public async Task<WalletResult> GeneratorClaimAsync()
    {
        if (!await EnsureAuthenticatedAsync() || SaiServer.Instance.ItemGenerator == null)
            return new WalletResult { coins = Wallet.Coins, diamonds = Wallet.Diamonds };

        SaiGame.Services.ItemGenerator genModule = SaiServer.Instance.ItemGenerator;
        var loadTcs = new TaskCompletionSource<GeneratorsResponse>();
        genModule.GetGenerators(
            onSuccess: res => loadTcs.TrySetResult(res),
            onError: _ => loadTcs.TrySetResult(null));
        await loadTcs.Task;

        GeneratorData[] gens = genModule.GetGeneratorsByOutputItemCode(CoinItemCode);
        if ((gens == null || gens.Length == 0) && genModule.CurrentGenerators?.generators != null)
            gens = genModule.CurrentGenerators.generators;

        if (gens != null && gens.Length > 0 && gens[0].ticket_count > 0)
        {
            var collectTcs = new TaskCompletionSource<GeneratorCollectResponse>();
            genModule.CollectGenerator(
                inventoryItemId: gens[0].inventory_item_id,
                onSuccess: res => collectTcs.TrySetResult(res),
                onError: err => collectTcs.TrySetException(new Exception(err)));
            await collectTcs.Task;
        }

        return await RefreshWalletAsync();
    }

    // Nhận 100 GOLD xem quảng cáo qua Lua script trên SaiServer
    public async Task<WalletResult> AdRewardAsync(string adToken)
    {
        await RunLuaActionAsync(new
        {
            action = "ad_reward",
            ad_token = adToken ?? "",
            coin_code = CoinItemCode
        });
        return await RefreshWalletAsync();
    }

    // Đổi DIAMOND sang GOLD qua Lua script (hoặc module Shop của SaiServer)
    public async Task<WalletResult> ShopExchangeAsync(string packId)
    {
        await RunLuaActionAsync(new
        {
            action = "shop_exchange",
            pack_id = packId ?? "",
            diamond_code = DiamondItemCode,
            coin_code = CoinItemCode
        });
        return await RefreshWalletAsync();
    }

    // Xác thực gói IAP và cộng DIAMOND trên SaiServer qua Lua script
    public async Task<WalletResult> IapVerifyAsync(string store, string productId, string receipt)
    {
        await RunLuaActionAsync(new
        {
            action = "iap_grant",
            store = store ?? "",
            product_id = productId ?? "",
            receipt = receipt ?? "",
            diamond_code = DiamondItemCode
        });
        return await RefreshWalletAsync();
    }

    // Lấy trạng thái quest điểm danh hôm nay và streak từ Daily Quest pool
    public async Task<DailyLoginStatus> GetDailyLoginAsync()
    {
        if (!await EnsureAuthenticatedAsync()) return null;
        DailyQuest dailyQuest = SaiServer.Instance.DailyQuest;
        if (dailyQuest == null)
            throw new InvalidOperationException("DailyQuest component is missing on SaiServer.");

        string poolId = await ResolveDailyPoolIdAsync(dailyQuest);
        TodayQuestResponse today = await CallSdk<TodayQuestResponse>((ok, fail) => dailyQuest.GetTodayQuests(poolId, ok, fail));
        DailyQuestEntryData entry = today?.entries != null && today.entries.Length > 0 ? today.entries[0] : null;
        if (entry == null) return null;

        string status = entry.status ?? "";
        string assignedDate = !string.IsNullOrEmpty(entry.assignment?.assigned_date) ? entry.assignment.assigned_date : today.assigned_date;
        bool hasDate = TryParseDate(assignedDate, out DateTime todayDate);
        return new DailyLoginStatus
        {
            assignmentId = entry.assignment?.id ?? "",
            day = hasDate ? WeekdayIndex(todayDate) : ParseLoginDay(entry.quest),
            status = status,
            claimedToday = status == "claimed",
            streak = today.streak != null ? today.streak.current_streak : 0,
            resetAtUnix = ParseUnixSeconds(entry.assignment?.expires_at),
            rewards = ToRewardItems(entry.rewards),
            week = hasDate ? await LoadLoginWeekAsync(todayDate) : null
        };
    }

    // Lấy trạng thái 7 ngày (Thứ 2 → Chủ nhật) của tuần chứa hôm nay qua assigned-timeframe, null nếu lỗi
    static async Task<DailyLoginDay[]> LoadLoginWeekAsync(DateTime todayDate)
    {
        DailyTimeframe timeframe = SaiServer.Instance.GetComponentInChildren<DailyTimeframe>(true);
        if (timeframe == null) return null;

        DateTime monday = todayDate.AddDays(1 - WeekdayIndex(todayDate));
        string start = monday.ToString("yyyy-MM-dd");
        string end = monday.AddDays(6).ToString("yyyy-MM-dd");
        DailyTimeframeResponse res;
        try
        {
            res = await CallSdk<DailyTimeframeResponse>((ok, fail) => timeframe.GetTimeframe(DailyLoginPoolKey, start, end, ok, fail));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendClient] Daily login timeframe warning: {e.Message}");
            return null;
        }

        var week = new DailyLoginDay[7];
        for (int i = 0; i < 7; i++)
        {
            DateTime date = monday.AddDays(i);
            week[i] = new DailyLoginDay
            {
                day = i + 1,
                date = date.ToString("yyyy-MM-dd"),
                state = date < todayDate ? DailyLoginDayState.Missed : date == todayDate ? DailyLoginDayState.Today : DailyLoginDayState.Upcoming,
                rewards = new DailyRewardItem[0]
            };
        }

        if (res?.days == null) return week;
        foreach (DailyDayData d in res.days)
        {
            if (d == null || !TryParseDate(d.date, out DateTime date)) continue;
            int index = (int)(date - monday).TotalDays;
            if (index < 0 || index > 6) continue;
            DailyQuestEntryData e = d.quests != null && d.quests.Length > 0 ? d.quests[0] : null;
            if (e == null) continue;
            week[index].rewards = ToRewardItems(e.rewards);
            if (e.status == "claimed") week[index].state = DailyLoginDayState.Claimed;
        }
        return week;
    }

    // Đọc ngày dạng yyyy-MM-dd (bỏ phần giờ nếu có)
    static bool TryParseDate(string value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrEmpty(value) || value.Length < 10) return false;
        return DateTime.TryParseExact(value.Substring(0, 10), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out date);
    }

    // Số thứ tự trong tuần: Thứ 2 = 1 … Chủ nhật = 7
    static int WeekdayIndex(DateTime date) => ((int)date.DayOfWeek + 6) % 7 + 1;

    // Start (nếu chưa bắt đầu) → Check → Claim quest điểm danh hôm nay rồi đồng bộ ví
    public async Task<DailyRewardItem[]> ClaimDailyLoginAsync(DailyLoginStatus status)
    {
        if (status == null || string.IsNullOrEmpty(status.assignmentId))
            throw new ArgumentException("Daily login assignment is missing.");
        if (!await EnsureAuthenticatedAsync())
            throw new InvalidOperationException("Not authenticated with SaiServer.");
        QuestProgressor progressor = SaiServer.Instance.QuestProgressor;
        if (progressor == null)
            throw new InvalidOperationException("QuestProgressor component is missing on SaiServer.");

        string id = status.assignmentId;
        if (status.status != "completed")
        {
            if (status.status != "in_progress")
            {
                try { await CallSdk<StartQuestResponse>((ok, fail) => progressor.StartDailyQuestAssignment(id, ok, fail)); }
                catch (Exception e) { Debug.LogWarning($"[BackendClient] Daily login start warning: {e.Message}"); }
            }

            CheckQuestResponse check = await CallSdk<CheckQuestResponse>((ok, fail) => progressor.CheckDailyQuestAssignment(id, ok, fail));
            string checkStatus = check?.progress?.status ?? check?.status ?? "";
            if (checkStatus != "completed" && checkStatus != "claimed")
                throw new Exception($"Daily login quest not completed (status: {checkStatus}).");
        }

        ClaimQuestResponse claim = await CallSdk<ClaimQuestResponse>((ok, fail) => progressor.ClaimDailyQuestAssignment(id, ok, fail));
        await RefreshWalletAsync();
        return ToRewardItems(claim?.rewards_granted);
    }

    // Tra id của Daily Quest pool điểm danh theo pool_key (chỉ gọi server lần đầu)
    static async Task<string> ResolveDailyPoolIdAsync(DailyQuest dailyQuest)
    {
        if (!string.IsNullOrEmpty(_dailyPoolId)) return _dailyPoolId;

        DailyQuestPoolsResponse res = await CallSdk<DailyQuestPoolsResponse>((ok, fail) => dailyQuest.GetPools(ok, fail));
        if (res?.pools != null)
        {
            foreach (DailyQuestPoolData pool in res.pools)
            {
                if (pool != null && pool.is_active && pool.pool_key == DailyLoginPoolKey)
                    return _dailyPoolId = pool.id;
            }
        }
        throw new Exception($"Daily quest pool '{DailyLoginPoolKey}' not found or inactive.");
    }

    // Đọc số ngày 1..7 từ code_name daily_login_dN, dự phòng bằng sort_order
    static int ParseLoginDay(QuestDefinitionData quest)
    {
        if (quest == null) return 1;
        string code = quest.code_name ?? "";
        if (code.StartsWith(DailyLoginQuestPrefix, StringComparison.Ordinal)
            && int.TryParse(code.Substring(DailyLoginQuestPrefix.Length), out int day))
            return Mathf.Clamp(day, 1, 7);
        return Mathf.Clamp(quest.sort_order, 1, 7);
    }

    // Đổi chuỗi thời gian RFC 3339 của server sang Unix giây
    static long ParseUnixSeconds(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return 0;
        return DateTimeOffset.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out DateTimeOffset t) ? t.ToUnixTimeSeconds() : 0;
    }

    // Đổi item_definition_id sang loại tiền tệ, trả về false nếu không phải GOLD/DIAMOND
    static bool TryGetCurrency(string defId, string code, out CurrencyType currency)
    {
        currency = CurrencyType.Coins;
        if (defId == CoinItemDefId || string.Equals(code, CoinItemCode, StringComparison.OrdinalIgnoreCase)) return true;
        currency = CurrencyType.Diamonds;
        return defId == DiamondItemDefId || string.Equals(code, DiamondItemCode, StringComparison.OrdinalIgnoreCase);
    }

    // Chuyển danh sách thưởng cấu hình của quest sang DailyRewardItem
    static DailyRewardItem[] ToRewardItems(DailyRewardData[] rewards)
    {
        var list = new List<DailyRewardItem>();
        if (rewards == null) return list.ToArray();
        foreach (DailyRewardData r in rewards)
        {
            if (r == null || !TryGetCurrency(r.item_definition_id, r.item_definition?.item_code, out CurrencyType currency)) continue;
            list.Add(new DailyRewardItem { currency = currency, amount = Math.Max(r.quantity_min, r.quantity_max) });
        }
        return list.ToArray();
    }

    // Chuyển danh sách thưởng đã cấp khi claim sang DailyRewardItem
    static DailyRewardItem[] ToRewardItems(ClaimQuestGrantedReward[] rewards)
    {
        var list = new List<DailyRewardItem>();
        if (rewards == null) return list.ToArray();
        foreach (ClaimQuestGrantedReward r in rewards)
        {
            if (r == null || !TryGetCurrency(r.item_definition_id, null, out CurrencyType currency)) continue;
            list.Add(new DailyRewardItem { currency = currency, amount = r.quantity > 0 ? r.quantity : r.amount });
        }
        return list.ToArray();
    }

    // Bọc API dạng callback (onSuccess, onError) của SaiServer SDK thành Task
    static Task<T> CallSdk<T>(Action<Action<T>, Action<string>> call)
    {
        var tcs = new TaskCompletionSource<T>();
        call(res => tcs.TrySetResult(res), err => tcs.TrySetException(new Exception(err)));
        return tcs.Task;
    }

    // Lấy tiến trình hiện tại của người chơi hoặc trả về null nếu chưa tạo
    static async Task<GamerProgressData> GetProgressOrNullAsync(GamerProgress gp)
    {
        if (gp == null) return null;
        var tcs = new TaskCompletionSource<GamerProgressData>();
        gp.GetProgress(
            onSuccess: res => tcs.TrySetResult(res),
            onError: _ => tcs.TrySetResult(null));
        return await tcs.Task;
    }

    // Tạo mới bản ghi GamerProgress cho người chơi lần đầu vào game
    static async Task<GamerProgressData> CreateProgressAsync(GamerProgress gp)
    {
        if (gp == null) return null;
        var tcs = new TaskCompletionSource<GamerProgressData>();
        gp.CreateProgress(
            onSuccess: res => tcs.TrySetResult(res),
            onError: err => tcs.TrySetException(new Exception(err)));
        return await tcs.Task;
    }

    // Giải mã JSON game_data từ GamerProgress thành đối tượng PlayerData
    static PlayerData ParsePlayerData(GamerProgressData progress)
    {
        var fallback = new PlayerData
        {
            playerId = SaiServer.Instance?.CurrentUser?.id ?? "",
            currentLevel = progress != null && progress.level > 0 ? progress.level : 1,
            highestLevel = progress != null && progress.level > 0 ? progress.level : 1
        };

        if (progress == null || string.IsNullOrWhiteSpace(progress.game_data) || progress.game_data == "{}")
            return fallback;

        try
        {
            PlayerData parsed = JsonUtility.FromJson<PlayerData>(progress.game_data);
            if (parsed == null) return fallback;
            parsed.playerId = SaiServer.Instance?.CurrentUser?.id ?? parsed.playerId;
            if (parsed.currentLevel < 1) parsed.currentLevel = Mathf.Max(1, progress.level);
            if (parsed.highestLevel < parsed.currentLevel) parsed.highestLevel = parsed.currentLevel;
            return parsed;
        }
        catch
        {
            return fallback;
        }
    }
}
