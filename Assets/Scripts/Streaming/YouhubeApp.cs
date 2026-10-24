using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App Youhube Studio trên Windows Desktop: thống kê kênh & lịch sử các buổi stream đã lưu
public class YouhubeApp : PcApp
{
    public override string AppName => "Youhube";

    VisualElement listRoot;
    Label kpiVodLabel;
    Label kpiFollowersLabel;
    Label kpiSubsLabel;

    public override void OnOpen()
    {
        Rebuild();
    }

    public override void BuildWindowContent(VisualElement contentRoot)
    {
        if (contentRoot == null) return;
        contentRoot.Clear();
        contentRoot.style.flexDirection = FlexDirection.Column;
        contentRoot.style.paddingLeft = 14;
        contentRoot.style.paddingRight = 14;
        contentRoot.style.paddingTop = 12;
        contentRoot.style.paddingBottom = 12;
        contentRoot.style.backgroundColor = new Color(0.10f, 0.11f, 0.14f, 1f);

        Label header = new Label("▶ YOUHUBE STUDIO — Phân tích Kênh & Lưu trữ VOD");
        header.style.flexShrink = 0;
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.98f, 0.34f, 0.36f, 1f);
        header.style.marginBottom = 10;
        contentRoot.Add(header);

        VisualElement kpiRow = new VisualElement();
        kpiRow.style.flexShrink = 0;
        kpiRow.style.flexDirection = FlexDirection.Row;
        kpiRow.style.marginBottom = 12;

        kpiVodLabel = new Label("0 VOD");
        kpiFollowersLabel = new Label("0");
        kpiSubsLabel = new Label("0");

        kpiRow.Add(CreateKpiCard("VIDEO ĐÃ LƯU", kpiVodLabel, new Color(0.86f, 0.22f, 0.24f, 1f)));
        kpiRow.Add(CreateKpiCard("NGƯỜI THEO DÕI", kpiFollowersLabel, new Color(0.22f, 0.56f, 0.94f, 1f)));
        kpiRow.Add(CreateKpiCard("SUBSCRIBERS", kpiSubsLabel, new Color(0.20f, 0.74f, 0.44f, 1f)));
        contentRoot.Add(kpiRow);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.flexBasis = 0;
        scroll.style.minHeight = 0;
        listRoot = scroll;
        contentRoot.Add(scroll);

        Rebuild();
    }

    VisualElement CreateKpiCard(string title, Label valueLabel, Color accent)
    {
        VisualElement card = new VisualElement();
        card.style.flexGrow = 1;
        card.style.flexBasis = 0;
        card.style.marginRight = 8;
        card.style.paddingLeft = 10;
        card.style.paddingRight = 10;
        card.style.paddingTop = 8;
        card.style.paddingBottom = 8;
        card.style.backgroundColor = new Color(0.15f, 0.17f, 0.22f, 1f);
        card.style.borderTopLeftRadius = 6;
        card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6;
        card.style.borderBottomRightRadius = 6;
        card.style.borderTopWidth = 2;
        card.style.borderTopColor = accent;

        Label cap = new Label(title);
        cap.style.fontSize = 10;
        cap.style.color = new Color(0.68f, 0.74f, 0.84f, 1f);

        valueLabel.style.fontSize = 16;
        valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        valueLabel.style.color = Color.white;
        valueLabel.style.marginTop = 2;

        card.Add(cap);
        card.Add(valueLabel);
        return card;
    }

    void Rebuild()
    {
        if (listRoot == null || !GameWorld.HasInstance) return;
        listRoot.Clear();

        List<StreamRecord> history = GameWorld.Instance.History;
        if (kpiVodLabel != null) kpiVodLabel.text = $"{history.Count} buổi";
        if (kpiFollowersLabel != null) kpiFollowersLabel.text = $"{GameWorld.Instance.Stream.Followers:N0}";
        if (kpiSubsLabel != null) kpiSubsLabel.text = $"{GameWorld.Instance.Stream.Subs:N0}";

        if (history.Count == 0)
        {
            Label empty = new Label("Chưa có buổi stream nào đủ 10 phút được lưu lại trên kênh.");
            empty.style.fontSize = 12;
            empty.style.color = new Color(0.72f, 0.78f, 0.86f, 1f);
            listRoot.Add(empty);
            return;
        }

        for (int i = history.Count - 1; i >= 0; i--)
        {
            StreamRecord rec = history[i];
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.marginBottom = 5;
            row.style.backgroundColor = new Color(0.15f, 0.17f, 0.22f, 1f);
            row.style.borderTopLeftRadius = 5;
            row.style.borderTopRightRadius = 5;
            row.style.borderBottomLeftRadius = 5;
            row.style.borderBottomRightRadius = 5;

            Label info = new Label($"🎬 Ngày {rec.day} · Chủ đề: {rec.category} · Thời lượng: {rec.minutes} phút");
            info.style.fontSize = 12;
            info.style.color = Color.white;

            Label peak = new Label($"👁 Đỉnh: {rec.peakViewers:N0} viewers");
            peak.style.fontSize = 12;
            peak.style.unityFontStyleAndWeight = FontStyle.Bold;
            peak.style.color = new Color(0.98f, 0.42f, 0.44f, 1f);

            row.Add(info);
            row.Add(peak);
            listRoot.Add(row);
        }
    }
}
