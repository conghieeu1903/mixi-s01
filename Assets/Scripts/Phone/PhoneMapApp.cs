using UnityEngine;
using UnityEngine.UIElements;

// App bản đồ trên điện thoại
public class PhoneMapApp : PhoneApp
{
    void Awake()
    {
        SetDefaultAppName("Bản đồ");
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();

        Label title = new Label("Bản đồ Thành phố — Chọn địa điểm để di chuyển");
        title.AddToClassList("wf-subheading");
        title.style.marginBottom = 8;
        content.Add(title);

        MapSystem map = FindFirstObjectByType<MapSystem>();
        if (map == null || map.Markers.Count == 0)
        {
            content.Add(new Label("Chưa có điểm đánh dấu nào trên bản đồ."));
            return;
        }

        for (int i = 0; i < map.Markers.Count; i++)
        {
            MapMarker marker = map.Markers[i];
            if (marker == null) continue;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;

            Label lbl = new Label($"• {marker.Label}");
            lbl.AddToClassList("wf-text");

            Button goBtn = new Button(() =>
            {
                map.TeleportTo(marker);
                FindFirstObjectByType<PhoneController>()?.Close();
            })
            {
                text = "Tới đây"
            };
            goBtn.AddToClassList("wf-button");
            goBtn.AddToClassList("wf-button--primary");
            goBtn.AddToClassList("wf-button--small");

            row.Add(lbl);
            row.Add(goBtn);
            content.Add(row);
        }
    }
}
