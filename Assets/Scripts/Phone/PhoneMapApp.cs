using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng Bản đồ Thành phố kết hợp radar định vị indicator theo cơ chế MapPanel của SLS2 và dịch chuyển nhanh
public class PhoneMapApp : PhoneApp
{
    int highlightedIndex = -1;
    VisualElement currentContent;

    public override string IconGlyph => "🗺";
    public override Color AccentColor => new Color(0.18f, 0.72f, 0.64f, 1f);
    public override bool BlockWhileDriving => false;

    void Awake()
    {
        SetDefaultAppName("Bản đồ");
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        content.Clear();

        MapSystem map = FindFirstObjectByType<MapSystem>();
        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        // Khung bản đồ radar thành phố trực quan giống MapPanel của SLS2
        VisualElement radarMap = new VisualElement();
        radarMap.style.height = 140;
        radarMap.style.backgroundColor = new Color(0.11f, 0.18f, 0.22f, 1f);
        radarMap.style.borderTopLeftRadius = 10;
        radarMap.style.borderTopRightRadius = 10;
        radarMap.style.borderBottomLeftRadius = 10;
        radarMap.style.borderBottomRightRadius = 10;
        radarMap.style.borderTopWidth = 1;
        radarMap.style.borderBottomWidth = 1;
        radarMap.style.borderLeftWidth = 1;
        radarMap.style.borderRightWidth = 1;
        radarMap.style.borderTopColor = new Color(0.24f, 0.78f, 0.68f, 0.45f);
        radarMap.style.borderBottomColor = new Color(0.24f, 0.78f, 0.68f, 0.45f);
        radarMap.style.borderLeftColor = new Color(0.24f, 0.78f, 0.68f, 0.45f);
        radarMap.style.borderRightColor = new Color(0.24f, 0.78f, 0.68f, 0.45f);
        radarMap.style.overflow = Overflow.Hidden;
        radarMap.style.marginBottom = 8;

        // Lưới đường phố giả lập trên bản đồ
        VisualElement roadH = new VisualElement();
        roadH.style.position = Position.Absolute;
        roadH.style.left = 0;
        roadH.style.right = 0;
        roadH.style.top = 66;
        roadH.style.height = 8;
        roadH.style.backgroundColor = new Color(0.22f, 0.28f, 0.34f, 0.85f);
        radarMap.Add(roadH);

        VisualElement roadV = new VisualElement();
        roadV.style.position = Position.Absolute;
        roadV.style.top = 0;
        roadV.style.bottom = 0;
        roadV.style.left = 135;
        roadV.style.width = 8;
        roadV.style.backgroundColor = new Color(0.22f, 0.28f, 0.34f, 0.85f);
        radarMap.Add(roadV);

        // Chấm định vị người chơi trên radar
        VisualElement playerDot = CreateMapDot(playerPos, new Color(0.25f, 0.85f, 1f, 1f), "Bạn", true);
        radarMap.Add(playerDot);

        if (map != null)
        {
            for (int i = 0; i < map.Markers.Count; i++)
            {
                MapMarker marker = map.Markers[i];
                if (marker == null) continue;
                bool activePin = i == highlightedIndex;
                Color pinColor = activePin
                    ? new Color(1f, 0.85f, 0.2f, 1f)
                    : new Color(0.95f, 0.38f, 0.38f, 0.9f);
                VisualElement pin = CreateMapDot(marker.transform.position, pinColor, activePin ? marker.Label : null, activePin);
                radarMap.Add(pin);
            }
        }

        content.Add(radarMap);

        if (map == null || map.Markers.Count == 0)
        {
            Label empty = new Label("Chưa có điểm đánh dấu nào trên bản đồ.");
            empty.style.color = new Color(0.75f, 0.8f, 0.88f, 1f);
            content.Add(empty);
            return;
        }

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        for (int i = 0; i < map.Markers.Count; i++)
        {
            int idx = i;
            MapMarker marker = map.Markers[idx];
            if (marker == null) continue;

            float dist = player != null ? Vector3.Distance(playerPos, marker.transform.position) : 0f;
            bool isHighlighted = highlightedIndex == idx;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = isHighlighted
                ? new Color(0.2f, 0.34f, 0.36f, 0.98f)
                : new Color(0.15f, 0.17f, 0.23f, 0.95f);
            row.style.borderTopLeftRadius = 8;
            row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8;
            row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom = 5;

            row.RegisterCallback<PointerEnterEvent>(_ =>
            {
                highlightedIndex = idx;
            });

            VisualElement leftCol = new VisualElement();
            leftCol.style.flexGrow = 1;

            Label lbl = new Label($"📍 {marker.Label}");
            lbl.style.color = Color.white;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.fontSize = 12;

            Label distLbl = new Label($"Cách đây: {dist:0}m");
            distLbl.style.color = new Color(0.72f, 0.8f, 0.9f, 1f);
            distLbl.style.fontSize = 10;

            leftCol.Add(lbl);
            leftCol.Add(distLbl);

            VisualElement btnGroup = new VisualElement();
            btnGroup.style.flexDirection = FlexDirection.Row;

            Button pinBtn = new Button(() =>
            {
                AudioManager.PlayUi("phone_tap");
                highlightedIndex = idx;
                BuildAppContent(content);
            })
            {
                text = "Định vị"
            };
            pinBtn.style.backgroundColor = new Color(0.24f, 0.32f, 0.42f, 1f);
            pinBtn.style.color = Color.white;
            pinBtn.style.fontSize = 10;
            pinBtn.style.borderTopLeftRadius = 6;
            pinBtn.style.borderTopRightRadius = 6;
            pinBtn.style.borderBottomLeftRadius = 6;
            pinBtn.style.borderBottomRightRadius = 6;
            pinBtn.style.paddingLeft = 7;
            pinBtn.style.paddingRight = 7;
            pinBtn.style.paddingTop = 4;
            pinBtn.style.paddingBottom = 4;
            pinBtn.style.marginRight = 4;

            Button goBtn = new Button(() =>
            {
                AudioManager.PlayUi("phone_tap");
                map.TeleportTo(marker);
                FindFirstObjectByType<PhoneController>()?.Close();
            })
            {
                text = "Tới đây →"
            };
            goBtn.style.backgroundColor = new Color(0.18f, 0.72f, 0.64f, 1f);
            goBtn.style.color = Color.white;
            goBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            goBtn.style.fontSize = 10;
            goBtn.style.borderTopLeftRadius = 6;
            goBtn.style.borderTopRightRadius = 6;
            goBtn.style.borderBottomLeftRadius = 6;
            goBtn.style.borderBottomRightRadius = 6;
            goBtn.style.paddingLeft = 8;
            goBtn.style.paddingRight = 8;
            goBtn.style.paddingTop = 4;
            goBtn.style.paddingBottom = 4;

            btnGroup.Add(pinBtn);
            btnGroup.Add(goBtn);

            row.Add(leftCol);
            row.Add(btnGroup);
            scroll.Add(row);
        }

        content.Add(scroll);
    }

    VisualElement CreateMapDot(Vector3 worldPos, Color color, string tag, bool prominent)
    {
        float nx = Mathf.Clamp01((worldPos.x + 45f) / 90f);
        float nz = Mathf.Clamp01(1f - (worldPos.z + 45f) / 90f);

        VisualElement dot = new VisualElement();
        dot.style.position = Position.Absolute;
        dot.style.left = Length.Percent(nx * 88f + 4f);
        dot.style.top = Length.Percent(nz * 80f + 8f);
        int size = prominent ? 12 : 8;
        dot.style.width = size;
        dot.style.height = size;
        dot.style.backgroundColor = color;
        dot.style.borderTopLeftRadius = size / 2;
        dot.style.borderTopRightRadius = size / 2;
        dot.style.borderBottomLeftRadius = size / 2;
        dot.style.borderBottomRightRadius = size / 2;

        if (!string.IsNullOrEmpty(tag))
        {
            Label tagLbl = new Label(tag);
            tagLbl.style.position = Position.Absolute;
            tagLbl.style.left = 14;
            tagLbl.style.top = -3;
            tagLbl.style.fontSize = 9;
            tagLbl.style.color = Color.white;
            tagLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tagLbl.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            tagLbl.style.paddingLeft = 4;
            tagLbl.style.paddingRight = 4;
            tagLbl.style.borderTopLeftRadius = 3;
            tagLbl.style.borderTopRightRadius = 3;
            tagLbl.style.borderBottomLeftRadius = 3;
            tagLbl.style.borderBottomRightRadius = 3;
            dot.Add(tagLbl);
        }

        return dot;
    }
}
