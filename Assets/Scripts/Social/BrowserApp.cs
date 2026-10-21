using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Trình duyệt web kiểu Chrome/Edge trên Windows Desktop: thanh Tab, thanh địa chỉ URL, Mail, Crypto, Nhân viên, Antivirus
public class BrowserApp : PcApp
{
    readonly List<string> history = new List<string> { "work://mail" };
    int historyIndex;
    VisualElement activeContent;

    public override string AppName => "Browser";
    public string CurrentUrl => history[Mathf.Clamp(historyIndex, 0, history.Count - 1)];

    void Awake()
    {
        if (string.IsNullOrEmpty(AppName))
        {
            var field = typeof(PcApp).GetField("appName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, "Browser");
        }
    }

    public void Navigate(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        AudioManager.PlayUi("ui_tab");
        if (historyIndex < history.Count - 1)
            history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
        history.Add(url);
        historyIndex = history.Count - 1;
        if (activeContent != null)
            BuildWindowContent(activeContent);
    }

    public void GoBack()
    {
        if (historyIndex > 0)
        {
            historyIndex--;
            AudioManager.PlayUi("ui_tab");
            if (activeContent != null)
                BuildWindowContent(activeContent);
        }
    }

    public void GoForward()
    {
        if (historyIndex < history.Count - 1)
        {
            historyIndex++;
            AudioManager.PlayUi("ui_tab");
            if (activeContent != null)
                BuildWindowContent(activeContent);
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        activeContent = content;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 12;
        content.style.paddingRight = 12;
        content.style.paddingTop = 10;
        content.style.paddingBottom = 10;
        content.style.backgroundColor = new Color(0.11f, 0.13f, 0.17f, 1f);

        VisualElement tabBar = new VisualElement();
        tabBar.style.flexDirection = FlexDirection.Row;
        tabBar.style.alignItems = Align.Center;
        tabBar.style.marginBottom = 6;

        (string label, string url)[] tabs =
        {
            ("✉ Hộp thư Việc làm", "work://mail"),
            ("₿ Ví Đào Bitcoin", "btc://wallet"),
            ("👥 Quản lý Nhân viên", "work://agency"),
            ("🛡 Windows Defender & Điện", "av://defender")
        };

        for (int i = 0; i < tabs.Length; i++)
        {
            string targetUrl = tabs[i].url;
            bool active = CurrentUrl == targetUrl;
            Button tabBtn = new Button(() => Navigate(targetUrl)) { text = tabs[i].label };
            tabBtn.style.height = 26;
            tabBtn.style.marginRight = 5;
            tabBtn.style.paddingLeft = 10;
            tabBtn.style.paddingRight = 10;
            tabBtn.style.fontSize = 11;
            tabBtn.style.color = active ? Color.white : new Color(0.74f, 0.80f, 0.88f, 1f);
            tabBtn.style.backgroundColor = active
                ? new Color(0.18f, 0.48f, 0.88f, 1f)
                : new Color(0.16f, 0.19f, 0.24f, 1f);
            tabBtn.style.borderTopLeftRadius = 5;
            tabBtn.style.borderTopRightRadius = 5;
            tabBtn.style.borderBottomLeftRadius = 0;
            tabBtn.style.borderBottomRightRadius = 0;
            tabBtn.style.borderTopWidth = 0;
            tabBtn.style.borderBottomWidth = 0;
            tabBtn.style.borderLeftWidth = 0;
            tabBtn.style.borderRightWidth = 0;
            tabBar.Add(tabBtn);
        }
        content.Add(tabBar);

        VisualElement urlBar = new VisualElement();
        urlBar.style.flexDirection = FlexDirection.Row;
        urlBar.style.alignItems = Align.Center;
        urlBar.style.paddingLeft = 8;
        urlBar.style.paddingRight = 8;
        urlBar.style.paddingTop = 4;
        urlBar.style.paddingBottom = 4;
        urlBar.style.marginBottom = 10;
        urlBar.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        urlBar.style.borderTopLeftRadius = 5;
        urlBar.style.borderTopRightRadius = 5;
        urlBar.style.borderBottomLeftRadius = 5;
        urlBar.style.borderBottomRightRadius = 5;

        Button backBtn = new Button(GoBack) { text = "←" };
        backBtn.style.width = 24;
        backBtn.style.height = 22;
        backBtn.style.marginRight = 4;
        backBtn.style.color = Color.white;
        backBtn.style.backgroundColor = new Color(0.18f, 0.22f, 0.28f, 1f);

        Button fwdBtn = new Button(GoForward) { text = "→" };
        fwdBtn.style.width = 24;
        fwdBtn.style.height = 22;
        fwdBtn.style.marginRight = 8;
        fwdBtn.style.color = Color.white;
        fwdBtn.style.backgroundColor = new Color(0.18f, 0.22f, 0.28f, 1f);

        Label urlLbl = new Label($"🔒 https://{CurrentUrl.Replace("://", ".mixi/")}");
        urlLbl.style.fontSize = 11;
        urlLbl.style.color = new Color(0.68f, 0.84f, 0.98f, 1f);

        urlBar.Add(backBtn);
        urlBar.Add(fwdBtn);
        urlBar.Add(urlLbl);
        content.Add(urlBar);

        ScrollView body = new ScrollView();
        body.style.flexGrow = 1;
        content.Add(body);

        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        if (CurrentUrl == "btc://wallet")
        {
            Label btcLbl = new Label($"₿ Số dư ví đào: {gw.Crypto.Bitcoin:0.0000} BTC (Tỷ giá: $ {CryptoMining.UsdPerBitcoin:N0} / BTC)");
            btcLbl.style.fontSize = 14;
            btcLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            btcLbl.style.color = new Color(0.98f, 0.76f, 0.22f, 1f);
            btcLbl.style.marginBottom = 10;
            body.Add(btcLbl);

            Button sellBtn = new Button(() =>
            {
                float usd = gw.Crypto.Sell(gw.Crypto.Bitcoin);
                FindFirstObjectByType<HudView>()?.ShowInfo($"Đã bán Bitcoin thu về $ {usd:0.##}", usd > 0f);
                BuildWindowContent(content);
            })
            {
                text = "💰 Bán toàn bộ Bitcoin sang USD"
            };
            sellBtn.style.height = 30;
            sellBtn.style.color = Color.white;
            sellBtn.style.backgroundColor = new Color(0.86f, 0.52f, 0.12f, 1f);
            body.Add(sellBtn);
        }
        else if (CurrentUrl == "work://agency")
        {
            Label wTitle = new Label($"👥 Đội ngũ nhân viên Stream ({gw.Workers.Workers.Count}/{WorkerManager.MaxWorkers})");
            wTitle.style.fontSize = 13;
            wTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            wTitle.style.color = Color.white;
            wTitle.style.marginBottom = 8;
            body.Add(wTitle);

            List<StreamWorker> candidates = WorkerManager.CreateDefaultCandidates();
            for (int i = 0; i < candidates.Count; i++)
            {
                StreamWorker cand = candidates[i];
                StreamWorker hired = gw.Workers.Workers.Find(w => w.name == cand.name);

                VisualElement row = CreateCardRow();
                Label info = new Label(hired != null
                    ? $"{hired.name} · Lương $ {hired.salary}/ngày · Năng lượng {hired.energy:0}% · Đã kiếm $ {hired.earnedMoney:0.##}"
                    : $"{cand.name} · Lương $ {cand.salary}/ngày · Follower khởi điểm {cand.followers}");
                info.style.fontSize = 11;
                info.style.color = Color.white;

                Button actBtn = new Button(() =>
                {
                    if (hired != null)
                        gw.Workers.Fire(hired);
                    else
                        gw.Workers.Hire(cand);
                    BuildWindowContent(content);
                })
                {
                    text = hired != null ? "Sa thải" : "Thuê ngay"
                };
                actBtn.style.height = 24;
                actBtn.style.paddingLeft = 10;
                actBtn.style.paddingRight = 10;
                actBtn.style.fontSize = 11;
                actBtn.style.color = Color.white;
                actBtn.style.backgroundColor = hired != null
                    ? new Color(0.68f, 0.22f, 0.24f, 1f)
                    : new Color(0.18f, 0.62f, 0.36f, 1f);

                row.Add(info);
                row.Add(actBtn);
                body.Add(row);
            }
        }
        else if (CurrentUrl == "av://defender")
        {
            Label vStatus = new Label(gw.Virus.IsInfected
                ? "⚠ CẢNH BÁO: Máy tính đang nhiễm Virus (Stream bị kìm hãm người xem!)"
                : "🛡 Windows Defender: Máy tính an toàn, không phát hiện mã độc.");
            vStatus.style.fontSize = 13;
            vStatus.style.unityFontStyleAndWeight = FontStyle.Bold;
            vStatus.style.color = gw.Virus.IsInfected
                ? new Color(1f, 0.36f, 0.38f, 1f)
                : new Color(0.36f, 0.92f, 0.56f, 1f);
            vStatus.style.marginBottom = 8;
            body.Add(vStatus);

            Label eStatus = new Label($"⚡ Điện năng tiêu thụ hôm nay: {gw.Electricity.KwhToday:0.00} kWh");
            eStatus.style.fontSize = 12;
            eStatus.style.color = new Color(0.84f, 0.90f, 0.98f, 1f);
            eStatus.style.marginBottom = 10;
            body.Add(eStatus);

            Button scanBtn = new Button(() =>
            {
                gw.Virus.Scan();
                FindFirstObjectByType<HudView>()?.ShowInfo("Đã quét và diệt sạch Virus trên PC!", true);
                BuildWindowContent(content);
            })
            {
                text = "🛡 Quét & Diệt sạch Virus ngay"
            };
            scanBtn.style.height = 30;
            scanBtn.style.color = Color.white;
            scanBtn.style.backgroundColor = new Color(0.18f, 0.58f, 0.36f, 1f);
            body.Add(scanBtn);
        }
        else
        {
            Label mTitle = new Label("✉ Hộp thư Việc làm thêm & Hợp đồng Tài trợ");
            mTitle.style.fontSize = 13;
            mTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            mTitle.style.color = Color.white;
            mTitle.style.marginBottom = 8;
            body.Add(mTitle);

            for (int i = 0; i < gw.Mail.Inbox.Count; i++)
            {
                MailJob job = gw.Mail.Inbox[i];
                VisualElement row = CreateCardRow();

                Label lbl = new Label($"[{job.sender}] {job.title} — Thù lao: $ {job.reward:0.##}");
                lbl.style.fontSize = 11;
                lbl.style.color = Color.white;

                Button btn = new Button(() =>
                {
                    gw.Mail.Complete(job);
                    FindFirstObjectByType<HudView>()?.ShowInfo($"Đã hoàn thành việc [{job.title}] (+$ {job.reward:0.##})", true);
                    BuildWindowContent(content);
                })
                {
                    text = job.done ? "✓ Đã nhận" : "Nhận việc"
                };
                btn.style.height = 24;
                btn.style.paddingLeft = 10;
                btn.style.paddingRight = 10;
                btn.style.fontSize = 11;
                btn.style.color = Color.white;
                btn.style.backgroundColor = job.done
                    ? new Color(0.26f, 0.30f, 0.36f, 1f)
                    : new Color(0.18f, 0.56f, 0.88f, 1f);
                btn.SetEnabled(!job.done);

                row.Add(lbl);
                row.Add(btn);
                body.Add(row);
            }
        }
    }

    static VisualElement CreateCardRow()
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 10;
        row.style.paddingRight = 10;
        row.style.paddingTop = 7;
        row.style.paddingBottom = 7;
        row.style.marginBottom = 5;
        row.style.backgroundColor = new Color(0.15f, 0.18f, 0.23f, 1f);
        row.style.borderTopLeftRadius = 5;
        row.style.borderTopRightRadius = 5;
        row.style.borderBottomLeftRadius = 5;
        row.style.borderBottomRightRadius = 5;
        return row;
    }
}
