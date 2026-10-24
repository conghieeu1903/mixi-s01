using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Các màn wireframe có thể chuyển tới
public enum WireframeScreen
{
    MainMenu,
    MenuDialogs,
    CreateCharacter,
    Loading,
    HUD,
    Pause,
    Settings,
    Inventory,
    Map,
    Phone,
    Computer,
    ComputerStates,
    StreamOverlay,
    NpcDialog,
    Blackjack,
    WorldPanels,
    Minigames,
}

// Gán một màn với file UXML của nó
[Serializable]
public struct WireframeScreenEntry
{
    public WireframeScreen screen; // Màn được gán
    public VisualTreeAsset asset; // File UXML của màn
}

// Chuyển giữa các màn wireframe theo luồng trong tài liệu UI, dùng để bấm thử prototype
[RequireComponent(typeof(UIDocument))]
public class WireframeNavigator : MonoBehaviour
{
    private const string TemplateUssClassName = "wf-template"; // Class giúp màn được clone phủ kín màn hình

    [SerializeField] private WireframeScreenEntry[] screens; // Danh sách màn và UXML tương ứng
    [SerializeField] private WireframeScreen startScreen = WireframeScreen.MainMenu; // Màn hiện đầu tiên
    [SerializeField] private bool hasSave = true; // Giả lập đã có save để đi nhánh Saved Game Found
    [SerializeField] private float loadingSeconds = 1.5f; // Thời gian đứng ở màn Loading

    private readonly Dictionary<WireframeScreen, VisualTreeAsset> assets = new();
    private UIDocument document;
    private WireframeScreen current;
    private WireframeScreen settingsReturn = WireframeScreen.MainMenu; // Màn quay lại khi đóng Settings
    private Coroutine loadingRoutine;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        foreach (var entry in screens)
        {
            if (entry.asset != null)
                assets[entry.screen] = entry.asset;
        }
    }

    private void Start() => Show(startScreen);

    // Xóa màn cũ, clone UXML của màn mới và gắn sự kiện cho các nút
    public void Show(WireframeScreen screen)
    {
        if (!assets.TryGetValue(screen, out var asset))
        {
            Debug.LogWarning($"[Wireframe] Chưa gán UXML cho màn {screen}");
            return;
        }

        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
            loadingRoutine = null;
        }

        var root = document.rootVisualElement;
        root.Clear();
        var tree = asset.Instantiate();
        tree.AddToClassList(TemplateUssClassName);
        root.Add(tree);

        current = screen;
        Bind(screen, tree);
    }

    private void ShowLoading(WireframeScreen next)
    {
        Show(WireframeScreen.Loading);
        loadingRoutine = StartCoroutine(LoadingThen(next));
    }

    private IEnumerator LoadingThen(WireframeScreen next)
    {
        yield return new WaitForSeconds(loadingSeconds);
        loadingRoutine = null;
        Show(next);
    }

    private void OpenSettings()
    {
        settingsReturn = current;
        Show(WireframeScreen.Settings);
    }

    private void Bind(WireframeScreen screen, VisualElement root)
    {
        switch (screen)
        {
            case WireframeScreen.MainMenu:
                On(root, "playButton", () => Show(hasSave ? WireframeScreen.MenuDialogs : WireframeScreen.CreateCharacter));
                On(root, "settingsButton", OpenSettings);
                On(root, "exitButton", () => Show(WireframeScreen.MenuDialogs));
                break;
            case WireframeScreen.MenuDialogs:
                On(root, "continueButton", () => ShowLoading(WireframeScreen.HUD));
                On(root, "deleteSaveButton", () => Show(WireframeScreen.MainMenu));
                On(root, "cancelButton", () => Show(WireframeScreen.MainMenu));
                On(root, "noButton", () => Show(WireframeScreen.MainMenu));
                On(root, "yesButton", Application.Quit);
                break;
            case WireframeScreen.CreateCharacter:
                On(root, "backButton", () => Show(WireframeScreen.MainMenu));
                On(root, "startButton", () => ShowLoading(WireframeScreen.HUD));
                break;
            case WireframeScreen.Pause:
                On(root, "resumeButton", () => Show(WireframeScreen.HUD));
                On(root, "settingsButton", OpenSettings);
                On(root, "controlsButton", () =>
                {
                    OpenSettings();
                    SwitchSettingsTab(document.rootVisualElement, 2);
                });
                On(root, "mainMenuButton", () => ShowLoading(WireframeScreen.MainMenu));
                On(root, "quitButton", Application.Quit);
                break;
            case WireframeScreen.Settings:
                On(root, "graphicsTab", () => SwitchSettingsTab(root, 0));
                On(root, "audioTab", () => SwitchSettingsTab(root, 1));
                On(root, "controlsTab", () => SwitchSettingsTab(root, 2));
                On(root, "languageTab", () => SwitchSettingsTab(root, 3));
                On(root, "backButton", () => Show(settingsReturn));
                On(root, "applyButton", () => Show(settingsReturn));
                SwitchSettingsTab(root, 0);
                break;
            case WireframeScreen.Inventory:
            case WireframeScreen.Map:
                On(root, "closeButton", () => Show(WireframeScreen.HUD));
                break;
            case WireframeScreen.NpcDialog:
                On(root, "choice1", () => Show(WireframeScreen.HUD));
                On(root, "choice2", () => Show(WireframeScreen.HUD));
                On(root, "choice3", () => Show(WireframeScreen.HUD));
                break;
            case WireframeScreen.Blackjack:
                On(root, "leaveButton", () => Show(WireframeScreen.HUD));
                break;
            case WireframeScreen.WorldPanels:
                On(root, "goLiveButton", () => Show(WireframeScreen.StreamOverlay));
                break;
        }
    }

    private static void On(VisualElement root, string buttonName, Action action)
    {
        var button = root.Q<Button>(buttonName);
        if (button != null)
            button.clicked += action;
    }

    // Chuyển tab trong màn Settings của Wireframe
    private static void SwitchSettingsTab(VisualElement root, int tabIndex)
    {
        if (root == null) return;
        SetTab(root, "graphicsTab", "graphicsPage", tabIndex == 0);
        SetTab(root, "audioTab", "audioPage", tabIndex == 1);
        SetTab(root, "controlsTab", "controlsPage", tabIndex == 2);
        SetTab(root, "languageTab", "languagePage", tabIndex == 3);
    }

    private static void SetTab(VisualElement root, string tabName, string pageName, bool active)
    {
        root.Q<Button>(tabName)?.EnableInClassList("wf-tab--active", active);
        var page = root.Q<VisualElement>(pageName);
        if (page != null)
        {
            page.EnableInClassList("settings__page--hidden", !active);
            page.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || loadingRoutine != null)
            return;

        if (keyboard.pageDownKey.wasPressedThisFrame)
            Cycle(1);
        else if (keyboard.pageUpKey.wasPressedThisFrame)
            Cycle(-1);
        else if (current == WireframeScreen.HUD)
            HandleHudKeys(keyboard);
        else if (keyboard.escapeKey.wasPressedThisFrame)
            HandleEscape();
        else if (current == WireframeScreen.Map && keyboard.mKey.wasPressedThisFrame)
            Show(WireframeScreen.HUD);
        else if (current == WireframeScreen.Inventory && (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
            Show(WireframeScreen.HUD);
    }

    // Phím tắt khi đang ở HUD, mô phỏng UIManager.Update và các tương tác thế giới
    private void HandleHudKeys(Keyboard keyboard)
    {
        if (keyboard.escapeKey.wasPressedThisFrame) Show(WireframeScreen.Pause);
        else if (keyboard.mKey.wasPressedThisFrame) Show(WireframeScreen.Map);
        else if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) Show(WireframeScreen.Inventory);
        else if (keyboard.pKey.wasPressedThisFrame) Show(WireframeScreen.Phone);
        else if (keyboard.cKey.wasPressedThisFrame) Show(WireframeScreen.Computer);
        else if (keyboard.lKey.wasPressedThisFrame) Show(WireframeScreen.StreamOverlay);
        else if (keyboard.tKey.wasPressedThisFrame) Show(WireframeScreen.NpcDialog);
        else if (keyboard.bKey.wasPressedThisFrame) Show(WireframeScreen.Blackjack);
        else if (keyboard.gKey.wasPressedThisFrame) Show(WireframeScreen.Minigames);
        else if (keyboard.eKey.wasPressedThisFrame) Show(WireframeScreen.WorldPanels);
    }

    private void HandleEscape()
    {
        switch (current)
        {
            case WireframeScreen.MainMenu:
                Show(WireframeScreen.MenuDialogs);
                break;
            case WireframeScreen.MenuDialogs:
            case WireframeScreen.CreateCharacter:
                Show(WireframeScreen.MainMenu);
                break;
            case WireframeScreen.Settings:
                Show(settingsReturn);
                break;
            case WireframeScreen.Computer:
                Show(WireframeScreen.ComputerStates);
                break;
            default:
                Show(WireframeScreen.HUD);
                break;
        }
    }

    // Lật qua mọi màn theo thứ tự trong danh sách để xem nhanh
    private void Cycle(int step)
    {
        var count = screens.Length;
        if (count == 0)
            return;

        var index = Array.FindIndex(screens, entry => entry.screen == current);
        var next = ((index + step) % count + count) % count;
        Show(screens[next].screen);
    }
}
