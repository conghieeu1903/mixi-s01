using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Điều khiển menu chính: Start mở màn chọn level, Shop mở cửa hàng, Options mở cài đặt, chọn thẻ để vào level sinh tự động
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Class USS cho thẻ level bị khóa")]
    const string LockedClass = "level-card--locked";

    [Tooltip("Nhóm nút của menu chính")]
    VisualElement menuRoot;

    [Tooltip("Màn chọn level")]
    VisualElement levelSelect;

    [Tooltip("Màn cửa hàng")]
    VisualElement shopScreen;

    [Tooltip("Màn cài đặt")]
    VisualElement settingsScreen;

    [Tooltip("Bộ điều khiển màn cài đặt")]
    SettingsController settings;

    [Tooltip("Thanh số dư xu ở menu chính")]
    CurrencyPill coinsPill;

    [Tooltip("Thanh số dư kim cương ở menu chính")]
    CurrencyPill diamondsPill;

    // Lấy các phần tử giao diện và gắn sự kiện cho nút, thẻ level
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        menuRoot = root.Q<VisualElement>("menuRoot");
        levelSelect = root.Q<VisualElement>("levelSelect");
        shopScreen = root.Q<VisualElement>("shopScreen");
        settingsScreen = root.Q<VisualElement>("settingsScreen");
        settings = GetComponent<SettingsController>();
        if (settings != null) settings.BackRequested += ShowMainMenu;

        root.Q<VisualElement>("startButton").AddManipulator(new Clickable(ShowLevelSelect));
        root.Q<VisualElement>("shopButton").AddManipulator(new Clickable(ShowShop));
        root.Q<SkewButton>("backButton").clicked += ShowMainMenu;
        shopScreen.Q<VisualElement>("backButton").AddManipulator(new Clickable(ShowMainMenu));
        root.Q<VisualElement>("optionsButton").AddManipulator(new Clickable(ShowSettings));
        root.Q<VisualElement>("topSettingsButton").AddManipulator(new Clickable(ShowSettings));
        shopScreen.Q<VisualElement>("settingsButton").AddManipulator(new Clickable(ShowSettings));

        coinsPill = menuRoot.Q<CurrencyPill>("coinsPill");
        diamondsPill = menuRoot.Q<CurrencyPill>("diamondsPill");
        Wallet.Changed += RefreshWallet;
        RefreshWallet();

        SetupLevelCards(root);
        ShowMainMenu();
    }

    // Gỡ sự kiện cập nhật ví
    void OnDisable() => Wallet.Changed -= RefreshWallet;

    // Hiển thị số dư xu và kim cương hiện tại lên menu chính
    void RefreshWallet()
    {
        coinsPill?.SetAmount(Wallet.Coins);
        diamondsPill?.SetAmount(Wallet.Diamonds);
    }

    // Gán số level cho các thẻ, cửa sổ thẻ trượt theo level cao nhất đã mở khóa
    void SetupLevelCards(VisualElement root)
    {
        List<VisualElement> cards = new List<VisualElement>();
        for (VisualElement card = root.Q<VisualElement>("levelCard1"); card != null; card = root.Q<VisualElement>($"levelCard{cards.Count + 1}"))
            cards.Add(card);

        GameManager game = GameManager.Instance;
        int highest = Mathf.Max(game.CurrentLevel, game.CloudSave.Current != null ? game.CloudSave.Current.highestLevel : 1);
        int firstLevel = Mathf.Max(1, highest - cards.Count + 1);

        for (int i = 0; i < cards.Count; i++)
        {
            int level = firstLevel + i;
            bool unlocked = LevelLoader.CanLoad && level <= highest;
            cards[i].EnableInClassList(LockedClass, !unlocked);
            cards[i].Q<GameText>("levelNumber").text = level.ToString();
            if (unlocked) cards[i].AddManipulator(new Clickable(() => LevelLoader.Load(level)));
        }
    }

    // Ẩn menu chính và hiện màn chọn level
    void ShowLevelSelect()
    {
        menuRoot.style.display = DisplayStyle.None;
        levelSelect.style.display = DisplayStyle.Flex;
    }

    // Ẩn menu chính và hiện màn cửa hàng
    void ShowShop()
    {
        menuRoot.style.display = DisplayStyle.None;
        shopScreen.style.display = DisplayStyle.Flex;
    }

    // Ẩn các màn khác và hiện màn cài đặt
    void ShowSettings()
    {
        menuRoot.style.display = DisplayStyle.None;
        shopScreen.style.display = DisplayStyle.None;
        settingsScreen.style.display = DisplayStyle.Flex;
        if (settings != null) settings.Show();
    }

    // Ẩn các màn phụ và hiện lại menu chính
    void ShowMainMenu()
    {
        levelSelect.style.display = DisplayStyle.None;
        shopScreen.style.display = DisplayStyle.None;
        settingsScreen.style.display = DisplayStyle.None;
        menuRoot.style.display = DisplayStyle.Flex;
    }
}
