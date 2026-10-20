using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App Youhube trên PC: lịch sử các buổi stream đã lưu
public class YouhubeApp : PcApp
{
    public override string AppName => "Youhube";

    VisualElement listRoot;
    Label summaryLabel;

    public override void OnOpen()
    {
        Rebuild();
    }

    public override void BuildWindowContent(VisualElement contentRoot)
    {
        if (contentRoot == null) return;
        contentRoot.Clear();
        contentRoot.style.flexDirection = FlexDirection.Column;
        contentRoot.style.paddingLeft = 16;
        contentRoot.style.paddingRight = 16;
        contentRoot.style.paddingTop = 12;
        contentRoot.style.paddingBottom = 12;

        Label header = new Label("Youhube Studio — Lịch sử phát sóng & VOD");
        header.AddToClassList("wf-subheading");
        header.style.marginBottom = 8;
        contentRoot.Add(header);

        summaryLabel = new Label("Tổng số buổi stream đã lưu: 0");
        summaryLabel.AddToClassList("wf-caption");
        summaryLabel.style.marginBottom = 8;
        contentRoot.Add(summaryLabel);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        listRoot = scroll;
        contentRoot.Add(scroll);

        Rebuild();
    }

    void Rebuild()
    {
        if (listRoot == null || !GameWorld.HasInstance) return;
        listRoot.Clear();

        List<StreamRecord> history = GameWorld.Instance.History;
        if (summaryLabel != null)
            summaryLabel.text = $"Tổng số buổi stream đã lưu: {history.Count} · Follower: {GameWorld.Instance.Stream.Followers:N0} · Sub: {GameWorld.Instance.Stream.Subs:N0}";

        if (history.Count == 0)
        {
            Label empty = new Label("Chưa có buổi stream nào đủ 10 phút được lưu lại.");
            empty.AddToClassList("wf-text");
            listRoot.Add(empty);
            return;
        }

        for (int i = history.Count - 1; i >= 0; i--)
        {
            StreamRecord rec = history[i];
            VisualElement row = new VisualElement();
            row.AddToClassList("world-panel__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;

            Label info = new Label($"Ngày {rec.day} · Chủ đề: {rec.category} · Thời lượng: {rec.minutes} phút");
            info.AddToClassList("wf-text");

            Label peak = new Label($"Đỉnh: {rec.peakViewers:N0} người xem");
            peak.AddToClassList("wf-subheading");

            row.Add(info);
            row.Add(peak);
            listRoot.Add(row);
        }
    }
}
