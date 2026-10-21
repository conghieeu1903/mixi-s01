using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App chat Discat trên Windows Desktop: bố cục 2 cột kiểu Discord (cột liên hệ bên trái, khung chat bên phải)
public class DiscatApp : PcApp
{
    [SerializeField] List<DiscatContact> contacts = new List<DiscatContact>();

    public override string AppName => "Discat";

    int selectedIndex;
    string lastOutcome = "Chọn một người liên hệ ở cột bên trái để phản hồi tin nhắn.";

    void Awake()
    {
        if (string.IsNullOrEmpty(AppName))
        {
            var field = typeof(PcApp).GetField("appName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, "Discat");
        }
        EnsureDefaultContacts();
    }

    void EnsureDefaultContacts()
    {
        if (contacts != null && contacts.Count > 0) return;
        contacts = new List<DiscatContact>
        {
            CreateContact("MixiFan_99", DiscatContactType.Fan),
            CreateContact("TrollHater_X", DiscatContactType.Hater),
            CreateContact("AnonHacker", DiscatContactType.Hacker),
            CreateContact("ModBot_Auto", DiscatContactType.Bot),
            CreateContact("Satoshi_BitMan", DiscatContactType.BitMan)
        };
    }

    static DiscatContact CreateContact(string name, DiscatContactType type)
    {
        DiscatContact c = ScriptableObject.CreateInstance<DiscatContact>();
        c.displayName = name;
        c.type = type;
        return c;
    }

    // Kiểm tra tin nhắn mới mỗi ngày
    public void DailyCheck()
    {
        EnsureDefaultContacts();
        AudioManager.PlayUi("ui_notification");
        lastOutcome = "Có tin nhắn mới trong hộp thư Discat hôm nay!";
    }

    // Phản hồi một liên hệ theo lựa chọn nhánh (branch 0 hoặc 1)
    public void ReplyToContact(DiscatContactType type, int branch)
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;
        AudioManager.Play("typing_key");

        switch (type)
        {
            case DiscatContactType.Fan:
                if (branch == 0)
                {
                    gw.Mood.Change(4f);
                    gw.Stream.AddFollowers(10);
                    lastOutcome = "Bạn: Cảm ơn em nhiều nhé! (+4 Mood, +10 Follower)";
                }
                else
                {
                    gw.Mood.Change(1f);
                    lastOutcome = "Bạn đã thả tim ❤️ tin nhắn của Fan (+1 Mood).";
                }
                break;

            case DiscatContactType.Hater:
                if (branch == 0)
                {
                    gw.Mood.Change(2f);
                    lastOutcome = "Hệ thống: Đã chặn TrollHater_X khỏi kênh (+2 Mood).";
                }
                else
                {
                    gw.Mood.Change(-4f);
                    lastOutcome = "Bạn cãi tay đôi với Hater làm tụt tâm trạng (-4 Mood).";
                }
                break;

            case DiscatContactType.Hacker:
                if (branch == 0)
                {
                    lastOutcome = "Hệ thống: Đã xóa liên kết độc hại từ AnonHacker (An toàn).";
                }
                else
                {
                    gw.Virus.TryInfect(1f);
                    lastOutcome = "⚠ CẢNH BÁO: Bạn vừa chạy file lạ và máy tính đã nhiễm Virus!";
                }
                break;

            case DiscatContactType.BitMan:
                float sold = gw.Crypto.Sell(gw.Crypto.Bitcoin);
                lastOutcome = sold > 0f
                    ? $"BitMan: Đã nhận Bitcoin và chuyển khoản $ {sold:0.##} vào ví của bạn!"
                    : "BitMan: Ví của bạn chưa có Bitcoin nào để giao dịch.";
                break;

            default:
                lastOutcome = "ModBot: Hệ thống lọc chat tự động đang hoạt động 100%.";
                break;
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        EnsureDefaultContacts();
        content.Clear();
        content.style.flexDirection = FlexDirection.Row;
        content.style.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);

        VisualElement sidebar = new VisualElement();
        sidebar.style.width = 205;
        sidebar.style.flexShrink = 0;
        sidebar.style.paddingLeft = 8;
        sidebar.style.paddingRight = 8;
        sidebar.style.paddingTop = 10;
        sidebar.style.paddingBottom = 10;
        sidebar.style.backgroundColor = new Color(0.09f, 0.10f, 0.13f, 1f);
        sidebar.style.borderRightWidth = 1;
        sidebar.style.borderRightColor = new Color(0.20f, 0.23f, 0.29f, 1f);

        Label dmHeader = new Label("TIN NHẮN TRỰC TIẾP");
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

            Button contactBtn = new Button(() =>
            {
                selectedIndex = idx;
                BuildWindowContent(content);
            })
            {
                text = $"● {c.displayName} [{c.type}]"
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
        channelHeader.style.paddingBottom = 8;
        channelHeader.style.marginBottom = 10;
        channelHeader.style.borderBottomWidth = 1;
        channelHeader.style.borderBottomColor = new Color(0.22f, 0.25f, 0.32f, 1f);

        Label channelTitle = new Label($"@ {current.displayName} — Vai trò: {current.type}");
        channelTitle.style.fontSize = 13;
        channelTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        channelTitle.style.color = Color.white;
        channelHeader.Add(channelTitle);
        chatPane.Add(channelHeader);

        VisualElement incomingBubble = new VisualElement();
        incomingBubble.style.paddingLeft = 12;
        incomingBubble.style.paddingRight = 12;
        incomingBubble.style.paddingTop = 10;
        incomingBubble.style.paddingBottom = 10;
        incomingBubble.style.marginBottom = 10;
        incomingBubble.style.backgroundColor = new Color(0.17f, 0.19f, 0.24f, 1f);
        incomingBubble.style.borderTopLeftRadius = 8;
        incomingBubble.style.borderTopRightRadius = 8;
        incomingBubble.style.borderBottomLeftRadius = 8;
        incomingBubble.style.borderBottomRightRadius = 8;

        Label msgLbl = new Label(GetPromptForType(current.type));
        msgLbl.style.fontSize = 12;
        msgLbl.style.color = new Color(0.92f, 0.95f, 1f, 1f);
        msgLbl.style.whiteSpace = WhiteSpace.Normal;
        incomingBubble.Add(msgLbl);
        chatPane.Add(incomingBubble);

        VisualElement replyBubble = new VisualElement();
        replyBubble.style.paddingLeft = 12;
        replyBubble.style.paddingRight = 12;
        replyBubble.style.paddingTop = 8;
        replyBubble.style.paddingBottom = 8;
        replyBubble.style.marginBottom = 12;
        replyBubble.style.backgroundColor = new Color(0.15f, 0.24f, 0.36f, 1f);
        replyBubble.style.borderTopLeftRadius = 8;
        replyBubble.style.borderTopRightRadius = 8;
        replyBubble.style.borderBottomLeftRadius = 8;
        replyBubble.style.borderBottomRightRadius = 8;

        Label outLbl = new Label(lastOutcome);
        outLbl.style.fontSize = 11;
        outLbl.style.color = new Color(0.82f, 0.92f, 1f, 1f);
        outLbl.style.whiteSpace = WhiteSpace.Normal;
        replyBubble.Add(outLbl);
        chatPane.Add(replyBubble);

        VisualElement spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        chatPane.Add(spacer);

        VisualElement actions = new VisualElement();
        actions.style.flexDirection = FlexDirection.Row;

        Button opt0 = new Button(() =>
        {
            ReplyToContact(current.type, 0);
            BuildWindowContent(content);
        })
        {
            text = GetOptionLabel(current.type, 0)
        };
        opt0.style.height = 30;
        opt0.style.marginRight = 8;
        opt0.style.paddingLeft = 12;
        opt0.style.paddingRight = 12;
        opt0.style.fontSize = 11;
        opt0.style.unityFontStyleAndWeight = FontStyle.Bold;
        opt0.style.color = Color.white;
        opt0.style.backgroundColor = new Color(0.35f, 0.40f, 0.95f, 1f);

        Button opt1 = new Button(() =>
        {
            ReplyToContact(current.type, 1);
            BuildWindowContent(content);
        })
        {
            text = GetOptionLabel(current.type, 1)
        };
        opt1.style.height = 30;
        opt1.style.paddingLeft = 12;
        opt1.style.paddingRight = 12;
        opt1.style.fontSize = 11;
        opt1.style.color = Color.white;
        opt1.style.backgroundColor = new Color(0.24f, 0.28f, 0.36f, 1f);

        actions.Add(opt0);
        actions.Add(opt1);
        chatPane.Add(actions);

        content.Add(chatPane);
    }

    static string GetPromptForType(DiscatContactType type)
    {
        return type switch
        {
            DiscatContactType.Fan => "MixiFan_99: Anh ơi tối nay lên sóng sớm nhé, cả nhóm tụi em đang hóng!",
            DiscatContactType.Hater => "TrollHater_X: Kênh toàn ăn may chứ có kỹ năng gì đâu!",
            DiscatContactType.Hacker => "AnonHacker: Tải thử tool tăng 10,000 viewer miễn phí này đi bro: cheat_stream.exe",
            DiscatContactType.BitMan => "Satoshi_BitMan: Đang thu mua Bitcoin giá $40,000 / BTC. Có bán không bạn hiền?",
            _ => "ModBot_Auto: Báo cáo kiểm duyệt tự động sẵn sàng."
        };
    }

    static string GetOptionLabel(DiscatContactType type, int branch)
    {
        return (type, branch) switch
        {
            (DiscatContactType.Fan, 0) => "💬 Trả lời thân thiện (+Follower)",
            (DiscatContactType.Fan, 1) => "❤️ Thả tim tin nhắn",
            (DiscatContactType.Hater, 0) => "🚫 Chặn luôn (+Mood)",
            (DiscatContactType.Hater, 1) => "💢 Tranh cãi lại (-Mood)",
            (DiscatContactType.Hacker, 0) => "🛡 Từ chối link lạ",
            (DiscatContactType.Hacker, 1) => "⚠ Tải file lạ (Nguy cơ Virus)",
            (DiscatContactType.BitMan, 0) => "💰 Bán hết Bitcoin lấy USD",
            (DiscatContactType.BitMan, 1) => "⛏ Giữ lại đào tiếp",
            _ => "✓ Kiểm tra trạng thái"
        };
    }
}
