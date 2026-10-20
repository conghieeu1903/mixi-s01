using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App chat Discat trên PC
public class DiscatApp : PcApp
{
    [SerializeField] List<DiscatContact> contacts = new List<DiscatContact>();

    int selectedIndex;
    string lastOutcome = "Chọn một người liên hệ để trò chuyện.";

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
        lastOutcome = "Có tin nhắn mới trong hộp thư Discat hôm nay!";
    }

    // Phản hồi một liên hệ theo lựa chọn nhánh (branch 0 hoặc 1)
    public void ReplyToContact(DiscatContactType type, int branch)
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        switch (type)
        {
            case DiscatContactType.Fan:
                if (branch == 0)
                {
                    gw.Mood.Change(4f);
                    gw.Stream.AddFollowers(10);
                    lastOutcome = "Bạn cảm ơn Fan nhiệt tình (+4 Mood, +10 Follower).";
                }
                else
                {
                    gw.Mood.Change(1f);
                    lastOutcome = "Bạn thả tim tin nhắn của Fan (+1 Mood).";
                }
                break;

            case DiscatContactType.Hater:
                if (branch == 0)
                {
                    gw.Mood.Change(2f);
                    lastOutcome = "Bạn chặn Hater gọn lẹ, giữ tâm trạng thoải mái (+2 Mood).";
                }
                else
                {
                    gw.Mood.Change(-4f);
                    lastOutcome = "Cãi tay đôi với Hater làm bạn bực mình (-4 Mood).";
                }
                break;

            case DiscatContactType.Hacker:
                if (branch == 0)
                {
                    lastOutcome = "Bạn từ chối mở file lạ từ Hacker (An toàn).";
                }
                else
                {
                    gw.Virus.TryInfect(1f);
                    lastOutcome = "CẢNH BÁO: Bạn vừa bấm link lạ của Hacker và máy tính đã nhiễm Virus!";
                }
                break;

            case DiscatContactType.BitMan:
                float sold = gw.Crypto.Sell(gw.Crypto.Bitcoin);
                lastOutcome = sold > 0f
                    ? $"Đã bán toàn bộ Bitcoin cho BitMan thu về $ {sold:0.##}!"
                    : "Chưa có Bitcoin để giao dịch với BitMan.";
                break;

            default:
                lastOutcome = "Bot hệ thống: Kênh của bạn đang hoạt động ổn định.";
                break;
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        EnsureDefaultContacts();
        content.Clear();

        Label header = new Label("Discat — Trò chuyện cộng đồng & Đối tác");
        header.AddToClassList("wf-subheading");
        header.style.marginBottom = 8;
        content.Add(header);

        VisualElement contactBar = new VisualElement();
        contactBar.style.flexDirection = FlexDirection.Row;
        contactBar.style.flexWrap = Wrap.Wrap;
        contactBar.style.marginBottom = 10;

        for (int i = 0; i < contacts.Count; i++)
        {
            int idx = i;
            DiscatContact c = contacts[i];
            Button tabBtn = new Button(() =>
            {
                selectedIndex = idx;
                BuildWindowContent(content);
            })
            {
                text = $"{c.displayName} ({c.type})"
            };
            tabBtn.AddToClassList("wf-button");
            tabBtn.AddToClassList("wf-button--small");
            if (idx == selectedIndex) tabBtn.AddToClassList("wf-button--primary");
            tabBtn.style.marginRight = 6;
            tabBtn.style.marginBottom = 4;
            contactBar.Add(tabBtn);
        }
        content.Add(contactBar);

        DiscatContact current = contacts[Mathf.Clamp(selectedIndex, 0, contacts.Count - 1)];
        VisualElement box = new VisualElement();
        box.AddToClassList("wf-panel");
        box.style.paddingLeft = 12;
        box.style.paddingRight = 12;
        box.style.paddingTop = 10;
        box.style.paddingBottom = 10;

        Label msgLbl = new Label(GetPromptForType(current.type));
        msgLbl.AddToClassList("wf-text");
        msgLbl.style.marginBottom = 8;
        box.Add(msgLbl);

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
        opt0.AddToClassList("wf-button");
        opt0.AddToClassList("wf-button--primary");
        opt0.AddToClassList("wf-button--small");
        opt0.style.marginRight = 8;

        Button opt1 = new Button(() =>
        {
            ReplyToContact(current.type, 1);
            BuildWindowContent(content);
        })
        {
            text = GetOptionLabel(current.type, 1)
        };
        opt1.AddToClassList("wf-button");
        opt1.AddToClassList("wf-button--small");

        actions.Add(opt0);
        actions.Add(opt1);
        box.Add(actions);

        Label outLbl = new Label(lastOutcome);
        outLbl.AddToClassList("wf-caption");
        outLbl.style.marginTop = 8;
        box.Add(outLbl);

        content.Add(box);
    }

    static string GetPromptForType(DiscatContactType type)
    {
        return type switch
        {
            DiscatContactType.Fan => "Fan: Anh ơi tối nay lên sóng sớm nhé, tụi em chờ!",
            DiscatContactType.Hater => "Hater: Kênh toàn ăn may chứ có kỹ năng gì đâu!",
            DiscatContactType.Hacker => "Hacker: Tải thử tool tăng 10,000 viewer miễn phí này đi bro: cheat_stream.exe",
            DiscatContactType.BitMan => "BitMan: Đang thu mua Bitcoin giá 40,000 USD / BTC. Có bán không?",
            _ => "ModBot: Báo cáo kiểm duyệt tự động sẵn sàng."
        };
    }

    static string GetOptionLabel(DiscatContactType type, int branch)
    {
        return (type, branch) switch
        {
            (DiscatContactType.Fan, 0) => "Trả lời thân thiện (+Follower)",
            (DiscatContactType.Fan, 1) => "Thả tim tin nhắn",
            (DiscatContactType.Hater, 0) => "Chặn luôn (+Mood)",
            (DiscatContactType.Hater, 1) => "Tranh cãi lại (-Mood)",
            (DiscatContactType.Hacker, 0) => "Từ chối link lạ",
            (DiscatContactType.Hacker, 1) => "Tải file lạ (Nguy cơ Virus)",
            (DiscatContactType.BitMan, 0) => "Bán hết Bitcoin lấy USD",
            (DiscatContactType.BitMan, 1) => "Giữ lại đào tiếp",
            _ => "Kiểm tra trạng thái"
        };
    }
}
