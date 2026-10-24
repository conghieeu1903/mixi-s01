using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App chat Discat trên Windows Desktop đồng bộ đầy đủ 6 nhân vật từ SLS2: Felix (Tutorial), BitDogo (BitMan), FlashPat (BotMan), soft_star (FanMan), anon_c (HackerMan), d00mer (HaterMan)
public class DiscatApp : PcApp
{
    [SerializeField] List<DiscatContact> contacts = new List<DiscatContact>();

    public override string AppName => "Discat";

    // Có tin nhắn mới chưa đọc hay không (hiển thị chấm thông báo trên icon Discat)
    public bool HasUnreadMessages { get; private set; } = true;

    // Cờ báo hiệu người chơi đã chọc giận anon_c (Hacker) dẫn tới nguy cơ bị SWAT đột kích (dùng cho Bước 6.5)
    public bool PendingSwatFromHacker { get; set; }

    int selectedIndex;

    // Trạng thái hội thoại theo từng nhân vật giống các Manager của SLS2
    int felixStage;
    int bitDogoStage;
    int bitDogoOweAmount;
    int bitDogoOverdueDays;
    int flashPatStage;
    bool flashPatBotPending;
    int softStarStage;
    int anonHackerStage;
    int doomerStage;

    readonly Dictionary<DiscatContactType, List<(bool isPlayer, string text)>> chatLogs =
        new Dictionary<DiscatContactType, List<(bool isPlayer, string text)>>();

    VisualElement activeContent;

    void Awake()
    {
        if (string.IsNullOrEmpty(AppName))
        {
            var field = typeof(PcApp).GetField("appName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, "Discat");
        }
        EnsureDefaultContacts();
        EnsureInitialChatLogs();
    }

    void EnsureDefaultContacts()
    {
        if (contacts != null && contacts.Count >= 6) return;
        contacts = new List<DiscatContact>
        {
            CreateContact("Felix", DiscatContactType.Friend),
            CreateContact("BitDogo", DiscatContactType.BitMan),
            CreateContact("FlashPat", DiscatContactType.Bot),
            CreateContact("soft_star", DiscatContactType.Fan),
            CreateContact("anon_c", DiscatContactType.Hacker),
            CreateContact("d00mer", DiscatContactType.Hater)
        };
    }

    static DiscatContact CreateContact(string name, DiscatContactType type)
    {
        DiscatContact c = ScriptableObject.CreateInstance<DiscatContact>();
        c.displayName = name;
        c.type = type;
        return c;
    }

    void EnsureInitialChatLogs()
    {
        if (chatLogs.Count > 0) return;

        chatLogs[DiscatContactType.Friend] = new List<(bool, string)>
        {
            (false, "Felix: Này ông bạn, có ở đó không? Nhắn lại cho tôi nhé!"),
            (false, "Felix: Tôi mới gửi chút vốn khởi nghiệp, nhớ mở ứng dụng OPS và Donate LAB trên màn hình để bắt đầu buổi Stream đầu tiên!")
        };

        chatLogs[DiscatContactType.BitMan] = new List<(bool, string)>
        {
            (false, "BitDogo: Xin chào. Tôi nhắn vì muốn hợp tác làm ăn với bạn."),
            (false, "BitDogo: Tôi sẽ chuyển $600 qua Donate trên Stream của bạn. Bạn được giữ lại 20% ($120), còn 80% ($480) chuyển lại cho tôi. Nếu quỵt quá 5 ngày thì đừng trách!")
        };

        chatLogs[DiscatContactType.Bot] = new List<(bool, string)>
        {
            (false, "FlashPat: Chào người anh em. Nghe nói bạn mới mở kênh Stream."),
            (false, "FlashPat: Muốn kênh đông nghịt người xem không? Đưa tôi $65, tôi bơm ngay +180 Bot Viewers thần kỳ vào kênh của bạn!")
        };

        chatLogs[DiscatContactType.Fan] = new List<(bool, string)>
        {
            (false, "soft_star: Chào anh, em là fan cứng của kênh từ ngày đầu luôn đó!"),
            (false, "soft_star: Anh có thể gửi tặng em một tấm ảnh selfie độc quyền hoặc lời chúc riêng trên sóng được không?")
        };

        chatLogs[DiscatContactType.Hacker] = new List<(bool, string)>
        {
            (false, "anon_c: Chúng tao đã nắm được địa chỉ IP phòng máy của mày."),
            (false, "anon_c: Khôn hồn thì chuyển ngay $50 phí bảo kê qua đây. Bằng không đừng hỏi tại sao máy sập hoặc có đội SWAT tới gõ cửa!")
        };

        chatLogs[DiscatContactType.Hater] = new List<(bool, string)>
        {
            (false, "d00mer: Tao vừa vào xem thử buổi Stream của mày."),
            (false, "d00mer: Nội dung nhạt nhẽo thật sự, nghỉ stream sớm đi cho đỡ tốn điện!")
        };
    }

    void AppendMessage(DiscatContactType type, bool isPlayer, string text)
    {
        EnsureInitialChatLogs();
        if (!chatLogs.TryGetValue(type, out var list))
        {
            list = new List<(bool, string)>();
            chatLogs[type] = list;
        }
        list.Add((isPlayer, text));
        if (list.Count > 14)
            list.RemoveAt(0);
    }

    // Kiểm tra sự kiện hàng ngày của các nhân vật trên Discat giống DailyCheck của SLS2
    public void DailyCheck()
    {
        EnsureDefaultContacts();
        EnsureInitialChatLogs();
        HasUnreadMessages = true;
        AudioManager.PlayUi("ui_notification");

        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        // Kiểm tra khoản nợ 80% của BitDogo (DiscatBitManManager)
        if (bitDogoOweAmount > 0)
        {
            bitDogoOverdueDays++;
            if (bitDogoOverdueDays < 5)
            {
                AppendMessage(DiscatContactType.BitMan, false,
                    $"BitDogo: Nhắc nhở ngày {bitDogoOverdueDays}/5 — Bạn vẫn còn nợ tôi ${bitDogoOweAmount}. Trả sớm đi trước khi tôi ra tay!");
            }
            else
            {
                float penalty = bitDogoOweAmount * 2f;
                float take = Mathf.Min(gw.Economy.Money, penalty);
                if (take > 0f)
                    gw.Economy.TrySpend(take);
                AppendMessage(DiscatContactType.BitMan, false,
                    $"BitDogo: Quá hạn 5 ngày! Đàn em của tôi đã rút thẳng ${take:0} (phạt gấp đôi) từ ví của bạn!");
                FindFirstObjectByType<HudView>()?.ShowInfo($"BitDogo đã phạt tiền nợ quá hạn (-${take:0})!", false);
                bitDogoOweAmount = 0;
                bitDogoOverdueDays = 0;
            }
        }
        else if (gw.Clock.Day % 2 == 0)
        {
            AppendMessage(DiscatContactType.BitMan, false,
                "BitDogo: Hôm nay tôi lại có kèo rửa tiền Donate mới ($600 ăn chia 20/80). Muốn nhận không?");
        }

        // Kiểm tra tin nhắn định kỳ của FlashPat (DiscatBotManManager)
        if (!flashPatBotPending && gw.Clock.Day % 2 == 1)
        {
            AppendMessage(DiscatContactType.Bot, false,
                "FlashPat: Kèo bơm +180 Bot Viewers giá hữu nghị $65 vẫn đang mở nhé bro!");
        }

        if (activeContent != null)
            BuildWindowContent(activeContent);
    }

    // Phản hồi một liên hệ theo lựa chọn nhánh (branch 0 hoặc 1)
    public void ReplyToContact(DiscatContactType type, int branch)
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;
        EnsureInitialChatLogs();
        AudioManager.Play("typing_key");
        HasUnreadMessages = false;

        switch (type)
        {
            case DiscatContactType.Friend:
                HandleFelixReply(gw, branch);
                break;

            case DiscatContactType.BitMan:
                HandleBitDogoReply(gw, branch);
                break;

            case DiscatContactType.Bot:
                HandleFlashPatReply(gw, branch);
                break;

            case DiscatContactType.Fan:
                HandleSoftStarReply(gw, branch);
                break;

            case DiscatContactType.Hacker:
                HandleAnonHackerReply(gw, branch);
                break;

            case DiscatContactType.Hater:
                HandleDoomerReply(gw, branch);
                break;
        }
    }

    // 1. Kịch bản Felix Tutorial (DiscatTutorialManager.cs)
    void HandleFelixReply(GameWorld gw, int branch)
    {
        if (felixStage == 0)
        {
            AppendMessage(DiscatContactType.Friend, true, "Bạn: Tôi đây rồi Felix! Cần tôi làm gì thế?");
            gw.Economy.Earn(75f);
            gw.Mood.Change(5f);
            felixStage = 1;
            AppendMessage(DiscatContactType.Friend, false,
                "Felix: Tuyệt vời ông bạn! Tôi vừa chuyển tặng $75 làm vốn. Hãy mở OPS để bật sóng và mở Donate LAB để nhận tiền ủng hộ từ người xem nhé!");
            FindFirstObjectByType<HudView>()?.ShowInfo("Felix đã gửi tặng $75 vốn khởi nghiệp qua Discat!", true);
        }
        else if (felixStage == 1)
        {
            AppendMessage(DiscatContactType.Friend, true, "Bạn: Tôi đã nắm rõ cách bật Stream và đọc Donate rồi!");
            gw.Stream.AddFollowers(20);
            gw.Mood.Change(4f);
            felixStage = 2;
            AppendMessage(DiscatContactType.Friend, false,
                "Felix: Quá đỉnh! Tôi vừa giới thiệu kênh của ông cho 20 người bạn (+20 Followers). Chúc lên xu hướng nhé!");
        }
        else
        {
            if (branch == 0)
            {
                AppendMessage(DiscatContactType.Friend, true, "Bạn: Hôm nay kênh phát triển tốt lắm ông bạn ơi!");
                gw.Mood.Change(3f);
                AppendMessage(DiscatContactType.Friend, false, "Felix: Hahaha cứ giữ vững phong độ đó nhé (+3 Mood)!");
            }
            else
            {
                AppendMessage(DiscatContactType.Friend, true, "Bạn: Ông có mẹo nào tăng người xem nhanh không?");
                AppendMessage(DiscatContactType.Friend, false, "Felix: Nhớ nâng cấp PC trong Computers, mua gói mạng FastWeb và rút thẻ tương tác (F1) mỗi buổi stream nhé!");
            }
        }
    }

    // 2. Kịch bản BitDogo (DiscatBitManManager.cs): Nhận kèo Donate $600 (giữ 20%, nợ 80%) hoặc bán BTC
    void HandleBitDogoReply(GameWorld gw, int branch)
    {
        if (bitDogoOweAmount > 0)
        {
            if (branch == 0)
            {
                if (gw.Economy.TrySpend(bitDogoOweAmount))
                {
                    AppendMessage(DiscatContactType.BitMan, true, $"Bạn: Tôi chuyển trả đủ ${bitDogoOweAmount} (80% tiền chia) cho ông đây.");
                    AppendMessage(DiscatContactType.BitMan, false, "BitDogo: Làm ăn uy tín đấy! Lần sau có kèo thơm tôi lại gọi.");
                    AudioManager.Play("purchase");
                    bitDogoOweAmount = 0;
                    bitDogoOverdueDays = 0;
                    bitDogoStage++;
                }
                else
                {
                    AppendMessage(DiscatContactType.BitMan, false, $"BitDogo: Ví của bạn chưa đủ ${bitDogoOweAmount}! Kiếm đủ tiền rồi trả ngay đi.");
                    AudioManager.PlayUi("purchase_fail");
                }
            }
            else
            {
                AppendMessage(DiscatContactType.BitMan, true, "Bạn: Cho tôi khất thêm ít hôm nữa nhé.");
                AppendMessage(DiscatContactType.BitMan, false, $"BitDogo: Nhớ đấy, quá 5 ngày là tôi trừ gấp đôi (${bitDogoOweAmount * 2})!");
            }
            return;
        }

        if (branch == 0)
        {
            int[] offers = { 500, 600, 700, 800, 1000 };
            int offer = offers[UnityEngine.Random.Range(0, offers.Length)];
            int owe = offer * 80 / 100;
            int profit = offer - owe;

            gw.Economy.Earn(offer);
            bitDogoOweAmount = owe;
            bitDogoOverdueDays = 0;
            AudioManager.Play("donate_alert");

            AppendMessage(DiscatContactType.BitMan, true, "Bạn: Chốt kèo! Cứ bắn tiền Donate qua kênh của tôi.");
            AppendMessage(DiscatContactType.BitMan, false,
                $"BitDogo: Đã chuyển nóng ${offer} vào ví của bạn! Bạn lời ròng ${profit} (20%), nhớ hoàn trả cho tôi ${owe} (80%) trong vòng 5 ngày!");
            FindFirstObjectByType<HudView>()?.ShowInfo($"BitDogo vừa chuyển ${offer} (Cần trả lại ${owe} trong 5 ngày)!", true);
        }
        else
        {
            float sold = gw.Crypto.Sell(gw.Crypto.Bitcoin);
            if (sold > 0f)
            {
                AppendMessage(DiscatContactType.BitMan, true, "Bạn: Tôi muốn bán số Bitcoin đào được cho ông.");
                AppendMessage(DiscatContactType.BitMan, false, $"BitDogo: Đã thu mua toàn bộ BTC và chuyển khoản $ {sold:0.##} cho bạn!");
            }
            else
            {
                AppendMessage(DiscatContactType.BitMan, true, "Bạn: Hiện tại tôi chưa muốn nhận kèo rửa tiền.");
                AppendMessage(DiscatContactType.BitMan, false, "BitDogo: Tùy bạn thôi, khi nào cần tiền nhanh hoặc bán BTC thì nhắn tôi.");
            }
        }
    }

    // 3. Kịch bản FlashPat (DiscatBotManManager.cs): Thuê Bot bơm Viewer thần kỳ
    void HandleFlashPatReply(GameWorld gw, int branch)
    {
        if (branch == 0)
        {
            const float botOfferCost = 65f;
            if (gw.Economy.TrySpend(botOfferCost))
            {
                int bots = UnityEngine.Random.Range(150, 240);
                gw.Stream.AddBotViewers(bots);
                flashPatBotPending = false;
                flashPatStage++;
                AudioManager.Play("purchase");
                AppendMessage(DiscatContactType.Bot, true, "Bạn: Chốt luôn! Bơm Bot View vào kênh giúp tôi ($65).");
                AppendMessage(DiscatContactType.Bot, false,
                    $"FlashPat: Xong ngay! Lượng người xem vừa tăng vọt thêm +{bots} Viewers một cách thần kỳ!");
                FindFirstObjectByType<HudView>()?.ShowInfo($"FlashPat đã bơm +{bots} Viewers thần kỳ vào kênh!", true);
            }
            else
            {
                AudioManager.PlayUi("purchase_fail");
                AppendMessage(DiscatContactType.Bot, false, "FlashPat: Bạn chưa đủ $65 rồi, kiếm thêm chút tiền đi nhé!");
            }
        }
        else
        {
            AppendMessage(DiscatContactType.Bot, true, "Bạn: Kênh tôi tự lực đi lên, không chơi Bot đâu.");
            gw.Mood.Change(2f);
            AppendMessage(DiscatContactType.Bot, false, "FlashPat: Tự trọng cao đấy! Khi nào đổi ý muốn lên top nhanh thì cứ ới tôi.");
        }
    }

    // 4. Kịch bản soft_star (DiscatFanManManager.cs): Fan cuồng 4 giai đoạn
    void HandleSoftStarReply(GameWorld gw, int branch)
    {
        if (branch == 0)
        {
            softStarStage++;
            int followersGain = 18 + softStarStage * 6;
            float fanTip = 35f + softStarStage * 15f;
            gw.Stream.AddFollowers(followersGain);
            gw.Economy.Earn(fanTip);
            gw.Mood.Change(4f);

            AppendMessage(DiscatContactType.Fan, true, $"Bạn: Cảm ơn em đã ủng hộ kênh nhé! Gửi tặng em chữ ký kỷ niệm nè (Giai đoạn {softStarStage}).");
            AppendMessage(DiscatContactType.Fan, false,
                $"soft_star: Trời ơi vui quá!! Em vừa rủ cả hội bạn vào Follow (+{followersGain} Followers) và gửi anh ${fanTip:0} uống nước nè ❤️!");
        }
        else
        {
            gw.Mood.Change(2f);
            AppendMessage(DiscatContactType.Fan, true, "Bạn: Anh chỉ giao lưu trên sóng thôi, đừng nhắn tin đòi hỏi quá đà nhé.");
            AppendMessage(DiscatContactType.Fan, false, "soft_star: Dạ em xin lỗi anh, tối nay em vẫn vào xem Stream ủng hộ anh ạ!");
        }
    }

    // 5. Kịch bản anon_c (DiscatHackerManManager.cs): Tống tiền $50, cài Virus hoặc gọi SWAT
    void HandleAnonHackerReply(GameWorld gw, int branch)
    {
        if (branch == 0)
        {
            if (gw.Economy.TrySpend(50f))
            {
                anonHackerStage = 1;
                PendingSwatFromHacker = false;
                AudioManager.Play("purchase");
                AppendMessage(DiscatContactType.Hacker, true, "Bạn: Đây $50, cầm lấy rồi xóa địa chỉ IP của tôi đi!");
                AppendMessage(DiscatContactType.Hacker, false, "anon_c: Biết điều đấy. Tạm thời bọn tao sẽ tha cho phòng máy của mày.");
            }
            else
            {
                AudioManager.PlayUi("purchase_fail");
                AppendMessage(DiscatContactType.Hacker, false, "anon_c: Mày không đủ nổi $50 à? Coi chừng cái máy tính của mày đấy!");
            }
        }
        else
        {
            anonHackerStage = 2;
            PendingSwatFromHacker = true;
            gw.Virus.TryInfect(1f);
            FindFirstObjectByType<SwatManager>()?.OpenSwatTrigger();
            AppendMessage(DiscatContactType.Hacker, true, "Bạn: Mày tưởng tao sợ chắc? Không có 1 xu nào đâu!");
            AppendMessage(DiscatContactType.Hacker, false,
                "anon_c: Mày dám thách thức bọn tao à?! Bọn tao vừa thả Virus vào máy mày và báo đội SWAT tới gõ cửa nhà mày rồi đấy, chờ xem!");
            FindFirstObjectByType<HudView>()?.ShowInfo("⚠ CẢNH BÁO: Hacker anon_c vừa tấn công Virus và điều đội SWAT tới nhà bạn!", false);
        }
    }

    // 6. Kịch bản d00mer (DiscatHaterManManager.cs): Đối답 với Hater qua 4 giai đoạn
    void HandleDoomerReply(GameWorld gw, int branch)
    {
        if (branch == 0)
        {
            doomerStage++;
            if (doomerStage >= 3)
            {
                gw.Mood.Change(6f);
                gw.Stream.AddFollowers(35);
                AppendMessage(DiscatContactType.Hater, true, "Bạn: Cảm ơn vì vẫn luôn chăm chỉ vào xem mọi buổi Stream của tôi nhé.");
                AppendMessage(DiscatContactType.Hater, false,
                    "d00mer: Hừ... công nhận mày lì thật đấy. Thôi được, tao ấn Follow kênh mày rồi đó (+35 Followers, +6 Mood)!");
                doomerStage = 0;
            }
            else
            {
                gw.Mood.Change(2f);
                AppendMessage(DiscatContactType.Hater, true, "Bạn: Không thích thì đừng xem nữa, cảm ơn đã tăng 1 lượt view.");
                AppendMessage(DiscatContactType.Hater, false,
                    $"d00mer: Mày tưởng tao thèm xem à? Để mai tao lại vào soi tiếp xem mày trụ được bao lâu! (Bậc {doomerStage}/3)");
            }
        }
        else
        {
            gw.Mood.Change(-4f);
            AppendMessage(DiscatContactType.Hater, true, "Bạn: Đồ rảnh rỗi sinh nông nổi, biến khỏi kênh của tao ngay!");
            AppendMessage(DiscatContactType.Hater, false, "d00mer: Hahaha cay cú rồi kìa! Càng chửi tao càng rủ thêm người vào phá chat (-4 Mood)!");
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        activeContent = content;
        EnsureDefaultContacts();
        EnsureInitialChatLogs();
        HasUnreadMessages = false;

        content.Clear();
        content.style.flexDirection = FlexDirection.Row;
        content.style.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);

        VisualElement sidebar = new VisualElement();
        sidebar.style.width = 210;
        sidebar.style.flexShrink = 0;
        sidebar.style.paddingLeft = 8;
        sidebar.style.paddingRight = 8;
        sidebar.style.paddingTop = 10;
        sidebar.style.paddingBottom = 10;
        sidebar.style.backgroundColor = new Color(0.09f, 0.10f, 0.13f, 1f);
        sidebar.style.borderRightWidth = 1;
        sidebar.style.borderRightColor = new Color(0.20f, 0.23f, 0.29f, 1f);

        Label dmHeader = new Label("TIN NHẮN TRỰC TIẾP (DISCAT)");
        dmHeader.style.fontSize = 10;
        dmHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        dmHeader.style.color = new Color(0.60f, 0.66f, 0.76f, 1f);
        dmHeader.style.marginBottom = 8;
        sidebar.Add(dmHeader);

        for (int i = 0; i < contacts.Count; i++)
        {
            int idx = i;
            DiscatContact c = contacts[i];
            bool active = idx == selectedIndex;

            string debtTag = (c.type == DiscatContactType.BitMan && bitDogoOweAmount > 0) ? $" [!-${bitDogoOweAmount}]" : string.Empty;
            Button contactBtn = new Button(() =>
            {
                selectedIndex = idx;
                BuildWindowContent(content);
            })
            {
                text = $"● {c.displayName} [{GetRoleLabel(c.type)}]{debtTag}"
            };
            contactBtn.style.height = 30;
            contactBtn.style.marginBottom = 4;
            contactBtn.style.paddingLeft = 8;
            contactBtn.style.fontSize = 11;
            contactBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            contactBtn.style.color = active ? Color.white : new Color(0.76f, 0.80f, 0.88f, 1f);
            contactBtn.style.backgroundColor = active
                ? new Color(0.35f, 0.40f, 0.95f, 1f)
                : new Color(0.14f, 0.16f, 0.20f, 1f);
            contactBtn.style.borderTopLeftRadius = 5;
            contactBtn.style.borderTopRightRadius = 5;
            contactBtn.style.borderBottomLeftRadius = 5;
            contactBtn.style.borderBottomRightRadius = 5;
            contactBtn.style.borderTopWidth = 0;
            contactBtn.style.borderBottomWidth = 0;
            contactBtn.style.borderLeftWidth = 0;
            contactBtn.style.borderRightWidth = 0;
            sidebar.Add(contactBtn);
        }

        content.Add(sidebar);

        DiscatContact current = contacts[Mathf.Clamp(selectedIndex, 0, contacts.Count - 1)];

        VisualElement chatPane = new VisualElement();
        chatPane.style.flexGrow = 1;
        chatPane.style.paddingLeft = 14;
        chatPane.style.paddingRight = 14;
        chatPane.style.paddingTop = 10;
        chatPane.style.paddingBottom = 10;
        chatPane.style.flexDirection = FlexDirection.Column;

        VisualElement channelHeader = new VisualElement();
        channelHeader.style.flexShrink = 0;
        channelHeader.style.paddingBottom = 6;
        channelHeader.style.marginBottom = 8;
        channelHeader.style.borderBottomWidth = 1;
        channelHeader.style.borderBottomColor = new Color(0.22f, 0.25f, 0.32f, 1f);

        Label channelTitle = new Label($"@ {current.displayName} — {GetRoleDescription(current.type)}");
        channelTitle.style.fontSize = 13;
        channelTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        channelTitle.style.color = Color.white;
        channelTitle.style.whiteSpace = WhiteSpace.NoWrap;
        channelTitle.style.overflow = Overflow.Hidden;
        channelTitle.style.textOverflow = TextOverflow.Ellipsis;
        channelHeader.Add(channelTitle);
        chatPane.Add(channelHeader);

        ScrollView historyScroll = new ScrollView();
        historyScroll.style.flexGrow = 1;
        historyScroll.style.flexBasis = 0;
        historyScroll.style.minHeight = 0;
        historyScroll.style.marginBottom = 8;
        DesktopOS.StyleSlimScroller(historyScroll);

        if (chatLogs.TryGetValue(current.type, out var logList))
        {
            for (int i = 0; i < logList.Count; i++)
            {
                var entry = logList[i];
                VisualElement bubble = new VisualElement();
                bubble.style.paddingLeft = 10;
                bubble.style.paddingRight = 10;
                bubble.style.paddingTop = 7;
                bubble.style.paddingBottom = 7;
                bubble.style.marginBottom = 6;
                bubble.style.backgroundColor = entry.isPlayer
                    ? new Color(0.18f, 0.28f, 0.46f, 1f)
                    : new Color(0.17f, 0.19f, 0.24f, 1f);
                bubble.style.borderTopLeftRadius = 6;
                bubble.style.borderTopRightRadius = 6;
                bubble.style.borderBottomLeftRadius = 6;
                bubble.style.borderBottomRightRadius = 6;

                Label msgLbl = new Label(entry.text);
                msgLbl.style.fontSize = 11;
                msgLbl.style.color = entry.isPlayer
                    ? new Color(0.84f, 0.94f, 1f, 1f)
                    : new Color(0.92f, 0.95f, 1f, 1f);
                msgLbl.style.whiteSpace = WhiteSpace.Normal;
                bubble.Add(msgLbl);
                historyScroll.Add(bubble);
            }
        }
        chatPane.Add(historyScroll);

        VisualElement actions = new VisualElement();
        actions.style.flexShrink = 0;
        actions.style.flexDirection = FlexDirection.Row;
        actions.style.flexWrap = Wrap.Wrap;

        Button opt0 = new Button(() =>
        {
            ReplyToContact(current.type, 0);
            BuildWindowContent(content);
        })
        {
            text = GetOptionLabel(current.type, 0)
        };
        opt0.style.height = 28;
        opt0.style.flexShrink = 0;
        opt0.style.marginRight = 8;
        opt0.style.marginTop = 0;
        opt0.style.marginBottom = 0;
        opt0.style.paddingLeft = 10;
        opt0.style.paddingRight = 10;
        opt0.style.paddingTop = 0;
        opt0.style.paddingBottom = 0;
        opt0.style.fontSize = 11;
        opt0.style.unityFontStyleAndWeight = FontStyle.Bold;
        opt0.style.unityTextAlign = TextAnchor.MiddleCenter;
        opt0.style.color = Color.white;
        opt0.style.backgroundColor = new Color(0.35f, 0.40f, 0.95f, 1f);
        opt0.style.borderTopLeftRadius = 5;
        opt0.style.borderTopRightRadius = 5;
        opt0.style.borderBottomLeftRadius = 5;
        opt0.style.borderBottomRightRadius = 5;
        opt0.style.borderTopWidth = 0;
        opt0.style.borderBottomWidth = 0;
        opt0.style.borderLeftWidth = 0;
        opt0.style.borderRightWidth = 0;

        Button opt1 = new Button(() =>
        {
            ReplyToContact(current.type, 1);
            BuildWindowContent(content);
        })
        {
            text = GetOptionLabel(current.type, 1)
        };
        opt1.style.height = 28;
        opt1.style.flexShrink = 0;
        opt1.style.marginTop = 0;
        opt1.style.marginBottom = 0;
        opt1.style.paddingLeft = 10;
        opt1.style.paddingRight = 10;
        opt1.style.paddingTop = 0;
        opt1.style.paddingBottom = 0;
        opt1.style.fontSize = 11;
        opt1.style.unityFontStyleAndWeight = FontStyle.Bold;
        opt1.style.unityTextAlign = TextAnchor.MiddleCenter;
        opt1.style.color = Color.white;
        opt1.style.backgroundColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        opt1.style.borderTopLeftRadius = 5;
        opt1.style.borderTopRightRadius = 5;
        opt1.style.borderBottomLeftRadius = 5;
        opt1.style.borderBottomRightRadius = 5;
        opt1.style.borderTopWidth = 0;
        opt1.style.borderBottomWidth = 0;
        opt1.style.borderLeftWidth = 0;
        opt1.style.borderRightWidth = 0;

        actions.Add(opt0);
        actions.Add(opt1);
        chatPane.Add(actions);

        content.Add(chatPane);
    }

    static string GetRoleLabel(DiscatContactType type)
    {
        return type switch
        {
            DiscatContactType.Friend => "Bạn thân",
            DiscatContactType.BitMan => "Môi giới",
            DiscatContactType.Bot => "ViewBot",
            DiscatContactType.Fan => "Fan cứng",
            DiscatContactType.Hacker => "Hacker",
            DiscatContactType.Hater => "Anti-Fan",
            _ => "Liên hệ"
        };
    }

    static string GetRoleDescription(DiscatContactType type)
    {
        return type switch
        {
            DiscatContactType.Friend => "Người hướng dẫn khởi nghiệp Streamer (DiscatTutorialManager)",
            DiscatContactType.BitMan => "Giao dịch ngầm Donate 20/80 & Thu mua Bitcoin (DiscatBitManManager)",
            DiscatContactType.Bot => "Dịch vụ bơm mắt xem ảo thần kỳ (DiscatBotManManager)",
            DiscatContactType.Fan => "Người hâm mộ cuồng nhiệt (DiscatFanManManager)",
            DiscatContactType.Hacker => "Nhóm tin tặc tống tiền IP & Báo SWAT (DiscatHackerManManager)",
            DiscatContactType.Hater => "Kẻ chuyên chê bai trên sóng Stream (DiscatHaterManManager)",
            _ => "Tin nhắn trực tiếp"
        };
    }

    string GetOptionLabel(DiscatContactType type, int branch)
    {
        if (type == DiscatContactType.Friend)
        {
            if (felixStage == 0)
                return branch == 0 ? "👋 Chào Felix! Nhận quà khởi nghiệp (+$75)" : "💬 Hỏi cách bắt đầu Stream";
            if (felixStage == 1)
                return branch == 0 ? "🚀 Báo tin đã biết mở Stream (+20 Followers)" : "💬 Cảm ơn Felix";
            return branch == 0 ? "😄 Trò chuyện vui vẻ (+Mood)" : "💡 Hỏi mẹo phát triển kênh";
        }

        if (type == DiscatContactType.BitMan)
        {
            if (bitDogoOweAmount > 0)
            {
                return branch == 0
                    ? $"💸 Hoàn trả ngay 80% nợ (${bitDogoOweAmount})"
                    : $"⏳ Khất nợ thêm (Đã nợ {bitDogoOverdueDays}/5 ngày)";
            }
            return branch == 0
                ? "🤝 Nhận kèo Donate ngầm ($500-$1000, lời 20%)"
                : "₿ Bán Bitcoin đào được / Từ chối kèo";
        }

        return (type, branch) switch
        {
            (DiscatContactType.Bot, 0) => "🤖 Thuê FlashPat bơm +180 Viewers ($65)",
            (DiscatContactType.Bot, 1) => "✋ Từ chối dùng Bot (+2 Mood)",
            (DiscatContactType.Fan, 0) => "❤️ Giao lưu & Tặng chữ ký (+Follower, +Donate)",
            (DiscatContactType.Fan, 1) => "🛡 Giữ khoảng cách lịch sự (+2 Mood)",
            (DiscatContactType.Hacker, 0) => "💵 Trả $50 phí bảo kê IP (An toàn)",
            (DiscatContactType.Hacker, 1) => "🔥 Thách thức Hacker (Nguy cơ nhiễm Virus & SWAT!)",
            (DiscatContactType.Hater, 0) => "😎 Phản hồi điềm tĩnh (Cảm hóa thành Fan)",
            (DiscatContactType.Hater, 1) => "💢 Cãi tay đôi với d00mer (-4 Mood)",
            _ => "✓ Phản hồi"
        };
    }
}
