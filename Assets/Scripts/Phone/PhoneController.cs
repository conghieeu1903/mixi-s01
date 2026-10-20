using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Điện thoại: mở/đóng, màn Home, mở app (chặn khi đang lái xe)
[RequireComponent(typeof(UIDocument))]
public class PhoneController : UIPanel
{
    [SerializeField] List<PhoneApp> apps = new List<PhoneApp>();

    VisualElement contentArea;
    PhoneApp activeApp;
    bool built;

    void OnEnable()
    {
        built = false;
        CollectApps();
        EnsurePhoneFrame();
    }

    void CollectApps()
    {
        PhoneApp[] found = GetComponentsInChildren<PhoneApp>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !apps.Contains(found[i]))
                apps.Add(found[i]);
        }
    }

    void EnsurePhoneFrame()
    {
        if (built || Root == null) return;
        built = true;

        VisualElement host = Root.Q<VisualElement>("worldPanels") ?? Root.Q<VisualElement>("phoneBoard") ?? Root;
        host.Clear();
        host.AddToClassList("wf-screen");
        host.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        host.style.alignItems = Align.Center;
        host.style.justifyContent = Justify.Center;

        VisualElement phoneBox = new VisualElement();
        phoneBox.AddToClassList("wf-panel");
        phoneBox.style.width = 420;
        phoneBox.style.height = 560;
        phoneBox.style.paddingLeft = 18;
        phoneBox.style.paddingRight = 18;
        phoneBox.style.paddingTop = 14;
        phoneBox.style.paddingBottom = 14;

        VisualElement topBar = new VisualElement();
        topBar.AddToClassList("wf-panel__header");

        Label title = new Label("Điện thoại thông minh (Phím P)");
        title.AddToClassList("wf-subheading");
        title.AddToClassList("wf-grow");

        Button homeBtn = new Button(ShowHomeScreen) { text = "Home" };
        homeBtn.AddToClassList("wf-button");
        homeBtn.AddToClassList("wf-button--small");
        homeBtn.style.marginRight = 6;

        Button closeBtn = new Button(Close) { text = "×" };
        closeBtn.AddToClassList("wf-button");
        closeBtn.AddToClassList("wf-button--square");

        topBar.Add(title);
        topBar.Add(homeBtn);
        topBar.Add(closeBtn);
        phoneBox.Add(topBar);

        contentArea = new ScrollView();
        contentArea.style.flexGrow = 1;
        phoneBox.Add(contentArea);

        host.Add(phoneBox);
    }

    public bool IsBlockedByDriving(out string reason)
    {
        VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] != null && vehicles[i].IsDriven)
            {
                reason = "Không thể dùng điện thoại khi đang lái xe";
                return true;
            }
        }
        reason = null;
        return false;
    }

    public void Open()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    public void Close()
    {
        if (activeApp != null)
        {
            activeApp.OnClose();
            activeApp = null;
        }
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }

    public override void Show()
    {
        CollectApps();
        EnsurePhoneFrame();
        ShowHomeScreen();
        base.Show();
    }

    public void ShowHomeScreen()
    {
        EnsurePhoneFrame();
        if (contentArea == null) return;
        contentArea.Clear();

        if (IsBlockedByDriving(out string reason))
        {
            Label warn = new Label(reason);
            warn.AddToClassList("wf-subheading");
            contentArea.Add(warn);
            return;
        }

        string[] appButtons = { "Đồ ăn", "Bản đồ", "Camera", "Album", "Skill", "Thú cưng", "Xe", "Quần áo" };
        for (int i = 0; i < appButtons.Length; i++)
        {
            string label = appButtons[i];
            Button btn = new Button(() => OnAppButtonClicked(label))
            {
                text = $"Mở ứng dụng: {label}"
            };
            btn.AddToClassList("wf-button");
            btn.style.marginBottom = 6;
            contentArea.Add(btn);
        }
    }

    void OnAppButtonClicked(string label)
    {
        if (label == "Skill")
        {
            Close();
            SkillTreeView sk = FindFirstObjectByType<SkillTreeView>();
            if (sk != null && UIManager.Instance != null)
                UIManager.Instance.Open(sk);
            return;
        }

        if (label == "Thú cưng")
        {
            if (GameWorld.HasInstance)
            {
                for (int i = 0; i < GameWorld.Instance.Pets.Pets.Count; i++)
                    GameWorld.Instance.Pets.Pets[i]?.Feed();
            }
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã cho thú cưng ăn và chăm sóc qua App! (+Bonus Viewer)", true);
            return;
        }

        if (label == "Xe")
        {
            FindFirstObjectByType<MapSystem>()?.TeleportToLabel("Showroom Xe");
            Close();
            return;
        }

        if (label == "Quần áo")
        {
            FindFirstObjectByType<MapSystem>()?.TeleportToLabel("Tiệm Thời Trang & Cắt Tóc");
            Close();
            return;
        }

        string searchName = label == "Album" ? "Camera" : label;
        for (int i = 0; i < apps.Count; i++)
        {
            if (apps[i] != null && string.Equals(apps[i].AppName, searchName, System.StringComparison.OrdinalIgnoreCase))
            {
                Launch(apps[i]);
                return;
            }
        }
    }

    // Mở một app trên điện thoại
    public void Launch(PhoneApp app)
    {
        if (app == null) return;
        EnsurePhoneFrame();
        if (!app.CanOpen(out string reason))
        {
            if (contentArea != null)
            {
                contentArea.Clear();
                contentArea.Add(new Label(reason));
            }
            return;
        }

        if (activeApp != null && activeApp != app)
            activeApp.OnClose();

        activeApp = app;
        activeApp.OnOpen();
        if (contentArea != null)
            activeApp.BuildAppContent(contentArea);
    }
}
