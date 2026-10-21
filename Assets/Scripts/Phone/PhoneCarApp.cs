using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng Quản lý & Cứu hộ Xe theo cơ chế carAppsPanel của SLS2: điều xe về bãi đỗ nhà hoặc vị trí gần nhất
public class PhoneCarApp : PhoneApp
{
    bool isParkingLoading;
    string parkingDoneMessage;
    VisualElement currentContent;
    Coroutine unstockRoutine;

    public override string IconGlyph => "🚗";
    public override Color AccentColor => new Color(0.92f, 0.28f, 0.32f, 1f);
    public override bool BlockWhileDriving => true;

    void Awake()
    {
        SetDefaultAppName("Xe");
    }

    public override void OnOpen()
    {
        isParkingLoading = false;
        parkingDoneMessage = null;
    }

    // Điều xe về bãi đỗ trước cửa nhà giống UnstuckCarButton của SLS2
    public void UnstuckCarButton()
    {
        if (isParkingLoading) return;
        AudioManager.PlayUi("phone_tap");
        if (isActiveAndEnabled)
        {
            if (unstockRoutine != null)
                StopCoroutine(unstockRoutine);
            unstockRoutine = StartCoroutine(UnstockVehicleRoutine(true));
        }
    }

    // Điều xe tới vị trí đường gần người chơi nhất giống UnstuckCarButton2 của SLS2
    public void UnstuckCarButton2()
    {
        if (isParkingLoading) return;
        AudioManager.PlayUi("phone_tap");
        if (isActiveAndEnabled)
        {
            if (unstockRoutine != null)
                StopCoroutine(unstockRoutine);
            unstockRoutine = StartCoroutine(UnstockVehicleRoutine(false));
        }
    }

    // Tiến trình đang điều xe và hiện thông báo Đỗ xe thành công giống UnstockVehicleRoutine của SLS2
    IEnumerator UnstockVehicleRoutine(bool toHomeParking)
    {
        isParkingLoading = true;
        parkingDoneMessage = null;
        RefreshView();

        yield return new WaitForSecondsRealtime(1.1f);

        VehicleController targetVehicle = FindPrimaryVehicle();
        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();

        if (targetVehicle != null)
        {
            if (toHomeParking)
            {
                Vector3 homePos = new Vector3(4.5f, 0.1f, -6.5f);
                MapSystem map = FindFirstObjectByType<MapSystem>();
                if (map != null)
                {
                    for (int i = 0; i < map.Markers.Count; i++)
                    {
                        MapMarker m = map.Markers[i];
                        if (m != null && m.Label.IndexOf("Nhà", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            homePos = m.transform.position + new Vector3(2.8f, 0f, 1.5f);
                            break;
                        }
                    }
                }
                targetVehicle.transform.position = homePos;
            }
            else if (player != null)
            {
                Vector3 forward = player.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                forward.Normalize();
                targetVehicle.transform.position = player.transform.position + forward * 3.2f;
            }

            AudioManager.PlayAt("car_horn", targetVehicle.transform.position);
        }

        isParkingLoading = false;
        parkingDoneMessage = toHomeParking
            ? "Đỗ xe về bãi trước nhà thành công!"
            : "Đã điều xe tới bãi đỗ gần bạn thành công!";
        FindFirstObjectByType<HudView>()?.ShowInfo(parkingDoneMessage, true);
        RefreshView();

        yield return new WaitForSecondsRealtime(1.6f);
        parkingDoneMessage = null;
        RefreshView();
        unstockRoutine = null;
    }

    VehicleController FindPrimaryVehicle()
    {
        VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
        VehicleController best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleController v = vehicles[i];
            if (v == null) continue;
            float dist = player != null ? Vector3.Distance(player.transform.position, v.transform.position) : i;
            if (dist < bestScore)
            {
                bestScore = dist;
                best = v;
            }
        }
        return best;
    }

    void RefreshView()
    {
        if (currentContent != null)
            BuildAppContent(currentContent);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        content.Clear();

        if (isParkingLoading)
        {
            VisualElement loadBox = new VisualElement();
            loadBox.style.flexGrow = 1;
            loadBox.style.alignItems = Align.Center;
            loadBox.style.justifyContent = Justify.Center;

            Label icon = new Label("🚗");
            icon.style.fontSize = 38;
            icon.style.marginBottom = 8;

            Label txt = new Label("Đang điều phối xe về bãi đỗ...");
            txt.style.color = Color.white;
            txt.style.unityFontStyleAndWeight = FontStyle.Bold;
            txt.style.fontSize = 13;

            loadBox.Add(icon);
            loadBox.Add(txt);
            content.Add(loadBox);
            return;
        }

        if (!string.IsNullOrEmpty(parkingDoneMessage))
        {
            VisualElement doneBox = new VisualElement();
            doneBox.style.backgroundColor = new Color(0.16f, 0.58f, 0.34f, 0.95f);
            doneBox.style.borderTopLeftRadius = 10;
            doneBox.style.borderTopRightRadius = 10;
            doneBox.style.borderBottomLeftRadius = 10;
            doneBox.style.borderBottomRightRadius = 10;
            doneBox.style.paddingLeft = 10;
            doneBox.style.paddingRight = 10;
            doneBox.style.paddingTop = 8;
            doneBox.style.paddingBottom = 8;
            doneBox.style.marginBottom = 10;

            Label doneLbl = new Label("✓ " + parkingDoneMessage);
            doneLbl.style.color = Color.white;
            doneLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            doneLbl.style.fontSize = 12;
            doneLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            doneLbl.style.whiteSpace = WhiteSpace.Normal;
            doneBox.Add(doneLbl);
            content.Add(doneBox);
        }

        VehicleController vehicle = FindPrimaryVehicle();
        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
        float dist = vehicle != null && player != null
            ? Vector3.Distance(player.transform.position, vehicle.transform.position)
            : 0f;

        VisualElement infoCard = new VisualElement();
        infoCard.style.backgroundColor = new Color(0.15f, 0.17f, 0.23f, 0.95f);
        infoCard.style.borderTopLeftRadius = 12;
        infoCard.style.borderTopRightRadius = 12;
        infoCard.style.borderBottomLeftRadius = 12;
        infoCard.style.borderBottomRightRadius = 12;
        infoCard.style.paddingLeft = 12;
        infoCard.style.paddingRight = 12;
        infoCard.style.paddingTop = 10;
        infoCard.style.paddingBottom = 10;
        infoCard.style.marginBottom = 10;

        Label carTitle = new Label(vehicle != null ? $"Phương tiện: {vehicle.name}" : "Phương tiện cá nhân");
        carTitle.style.color = Color.white;
        carTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        carTitle.style.fontSize = 14;
        carTitle.style.marginBottom = 4;

        Label distLbl = new Label(vehicle != null
            ? $"Khoảng cách tới xe hiện tại: {dist:0.0}m · Trạng thái: Sẵn sàng"
            : "Chưa có xe gần đây");
        distLbl.style.color = new Color(0.75f, 0.82f, 0.92f, 1f);
        distLbl.style.fontSize = 11;

        infoCard.Add(carTitle);
        infoCard.Add(distLbl);
        content.Add(infoCard);

        // Nút Cứu hộ / Đỗ xe về trước nhà (UnstuckCarButton)
        Button homeParkBtn = new Button(UnstuckCarButton)
        {
            text = "🏠 Cứu hộ & Đỗ xe về trước cửa Nhà"
        };
        StyleActionButton(homeParkBtn, new Color(0.24f, 0.56f, 0.94f, 1f));
        content.Add(homeParkBtn);

        // Nút Triệu hồi xe tới bãi đỗ gần nhất (UnstuckCarButton2)
        Button nearParkBtn = new Button(UnstuckCarButton2)
        {
            text = "📍 Triệu hồi xe tới vị trí gần bạn nhất"
        };
        StyleActionButton(nearParkBtn, new Color(0.18f, 0.72f, 0.44f, 1f));
        content.Add(nearParkBtn);

        // Nút đi tới Showroom Xe
        Button showroomBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            FindFirstObjectByType<MapSystem>()?.TeleportToLabel("Showroom Xe");
            Phone?.Close();
        })
        {
            text = "🚘 Đi tới Showroom Xe Thành phố"
        };
        StyleActionButton(showroomBtn, new Color(0.92f, 0.28f, 0.32f, 1f));
        content.Add(showroomBtn);
    }

    void StyleActionButton(Button btn, Color bg)
    {
        btn.style.backgroundColor = bg;
        btn.style.color = Color.white;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.fontSize = 12;
        btn.style.borderTopLeftRadius = 10;
        btn.style.borderTopRightRadius = 10;
        btn.style.borderBottomLeftRadius = 10;
        btn.style.borderBottomRightRadius = 10;
        btn.style.paddingTop = 9;
        btn.style.paddingBottom = 9;
        btn.style.marginBottom = 8;
    }
}
