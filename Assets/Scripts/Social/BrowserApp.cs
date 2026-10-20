using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Trình duyệt web nội bộ: các trang giả, back/forward, quản lý việc làm thêm, đào coin, nhân viên, diệt virus
public class BrowserApp : PcApp
{
    readonly List<string> history = new List<string> { "work://mail" };
    int historyIndex;
    VisualElement activeContent;

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
            if (activeContent != null)
                BuildWindowContent(activeContent);
        }
    }

    public void GoForward()
    {
        if (historyIndex < history.Count - 1)
        {
            historyIndex++;
            if (activeContent != null)
                BuildWindowContent(activeContent);
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        activeContent = content;
        content.Clear();

        VisualElement navBar = new VisualElement();
        navBar.style.flexDirection = FlexDirection.Row;
        navBar.style.flexWrap = Wrap.Wrap;
        navBar.style.marginBottom = 8;

        Button backBtn = new Button(GoBack) { text = "<" };
        backBtn.AddToClassList("wf-button");
        backBtn.AddToClassList("wf-button--square");
        backBtn.style.marginRight = 4;

        Button fwdBtn = new Button(GoForward) { text = ">" };
        fwdBtn.AddToClassList("wf-button");
        fwdBtn.AddToClassList("wf-button--square");
        fwdBtn.style.marginRight = 8;

        navBar.Add(backBtn);
        navBar.Add(fwdBtn);

        (string label, string url)[] tabs =
        {
            ("Thư việc làm (Mail)", "work://mail"),
            ("Đào Bitcoin (Crypto)", "btc://wallet"),
            ("Nhân viên Stream", "work://agency"),
            ("Diệt Virus & Điện", "av://defender")
        };

        for (int i = 0; i < tabs.Length; i++)
        {
            string targetUrl = tabs[i].url;
            Button tabBtn = new Button(() => Navigate(targetUrl)) { text = tabs[i].label };
            tabBtn.AddToClassList("wf-button");
            tabBtn.AddToClassList("wf-button--small");
            if (CurrentUrl == targetUrl) tabBtn.AddToClassList("wf-button--primary");
            tabBtn.style.marginRight = 6;
            navBar.Add(tabBtn);
        }

        content.Add(navBar);

        ScrollView body = new ScrollView();
        body.style.flexGrow = 1;
        content.Add(body);

        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        if (CurrentUrl == "btc://wallet")
        {
            Label btcLbl = new Label($"Số dư ví: {gw.Crypto.Bitcoin:0.0000} BTC (Tỷ giá: $ {CryptoMining.UsdPerBitcoin:N0}/BTC)");
            btcLbl.AddToClassList("wf-subheading");
            btcLbl.style.marginBottom = 8;
            body.Add(btcLbl);

            Button sellBtn = new Button(() =>
            {
                float usd = gw.Crypto.Sell(gw.Crypto.Bitcoin);
                FindFirstObjectByType<HudView>()?.ShowInfo($"Đã bán Bitcoin thu về $ {usd:0.##}", usd > 0f);
                BuildWindowContent(content);
            })
            {
                text = "Bán toàn bộ Bitcoin sang USD"
            };
            sellBtn.AddToClassList("wf-button");
            sellBtn.AddToClassList("wf-button--primary");
            body.Add(sellBtn);
        }
        else if (CurrentUrl == "work://agency")
        {
            Label wTitle = new Label($"Đội ngũ nhân viên Stream ({gw.Workers.Workers.Count}/{WorkerManager.MaxWorkers})");
            wTitle.AddToClassList("wf-subheading");
            wTitle.style.marginBottom = 8;
            body.Add(wTitle);

            List<StreamWorker> candidates = WorkerManager.CreateDefaultCandidates();
            for (int i = 0; i < candidates.Count; i++)
            {
                StreamWorker cand = candidates[i];
                StreamWorker hired = gw.Workers.Workers.Find(w => w.name == cand.name);

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 4;
                row.style.paddingBottom = 4;

                Label info = new Label(hired != null
                    ? $"{hired.name} · Lương $ {hired.salary}/ngày · Năng lượng {hired.energy:0}% · Đã kiếm $ {hired.earnedMoney:0.##}"
                    : $"{cand.name} · Lương $ {cand.salary}/ngày · Follower khởi điểm {cand.followers}");
                info.AddToClassList("wf-text");

                Button actBtn = new Button(() =>
                {
                    if (hired != null)
                        gw.Workers.Fire(hired);
                    else
                        gw.Workers.Hire(cand);
                    BuildWindowContent(content);
                })
                {
                    text = hired != null ? "Sa thải" : "Thuê"
                };
                actBtn.AddToClassList("wf-button");
                actBtn.AddToClassList("wf-button--small");
                if (hired == null) actBtn.AddToClassList("wf-button--primary");

                row.Add(info);
                row.Add(actBtn);
                body.Add(row);
            }
        }
        else if (CurrentUrl == "av://defender")
        {
            Label vStatus = new Label(gw.Virus.IsInfected
                ? "Trạng thái máy tính: ĐANG NHIỄM VIRUS (Stream bị chặn tăng trưởng!)"
                : "Trạng thái máy tính: An toàn, không có Virus");
            vStatus.AddToClassList("wf-subheading");
            vStatus.style.marginBottom = 6;
            body.Add(vStatus);

            Label eStatus = new Label($"Điện năng tiêu thụ hôm nay: {gw.Electricity.KwhToday:0.00} kWh");
            eStatus.AddToClassList("wf-text");
            eStatus.style.marginBottom = 8;
            body.Add(eStatus);

            Button scanBtn = new Button(() =>
            {
                gw.Virus.Scan();
                FindFirstObjectByType<HudView>()?.ShowInfo("Đã quét và diệt sạch Virus trên PC!", true);
                BuildWindowContent(content);
            })
            {
                text = "Quét & Diệt Virus ngay"
            };
            scanBtn.AddToClassList("wf-button");
            scanBtn.AddToClassList("wf-button--primary");
            body.Add(scanBtn);
        }
        else
        {
            Label mTitle = new Label("Hộp thư việc làm thêm & Tài trợ");
            mTitle.AddToClassList("wf-subheading");
            mTitle.style.marginBottom = 8;
            body.Add(mTitle);

            for (int i = 0; i < gw.Mail.Inbox.Count; i++)
            {
                MailJob job = gw.Mail.Inbox[i];
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 4;
                row.style.paddingBottom = 4;

                Label lbl = new Label($"[{job.sender}] {job.title} — Thù lao: $ {job.reward:0.##}");
                lbl.AddToClassList("wf-text");

                Button btn = new Button(() =>
                {
                    gw.Mail.Complete(job);
                    FindFirstObjectByType<HudView>()?.ShowInfo($"Đã hoàn thành việc [{job.title}] (+$ {job.reward:0.##})", true);
                    BuildWindowContent(content);
                })
                {
                    text = job.done ? "Đã nhận" : "Làm ngay"
                };
                btn.AddToClassList("wf-button");
                btn.AddToClassList("wf-button--small");
                if (!job.done) btn.AddToClassList("wf-button--primary");
                btn.SetEnabled(!job.done);

                row.Add(lbl);
                row.Add(btn);
                body.Add(row);
            }
        }
    }
}
