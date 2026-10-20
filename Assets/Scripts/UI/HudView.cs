using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// HUD trong game: giờ, ngày, tiền, badge hóa đơn, gợi ý tương tác, thông báo
[RequireComponent(typeof(UIDocument))]
public class HudView : MonoBehaviour
{
    [SerializeField] PlayerInteractor interactor;
    [SerializeField] ItemCarrier carrier;

    UIDocument document;
    Label dayLabel;
    Label clockLabel;
    Label moneyLabel;
    Label billBadge;
    Label mailBadge;
    Label itemNameLabel;
    VisualElement interactionHints;
    VisualElement bottomInfo;
    VisualElement heldItemBox;
    Label heldItemNameLabel;
    VisualElement contextHintsBox;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        if (interactor == null)
            interactor = FindFirstObjectByType<PlayerInteractor>();
        if (carrier == null)
            carrier = FindFirstObjectByType<ItemCarrier>();
    }

    // Lấy element trong UXML và đăng ký sự kiện của GameWorld
    void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        VisualElement root = document.rootVisualElement;
        if (root == null) return;

        VisualElement hudScreen = root.Q<VisualElement>("hud") ?? root;
        hudScreen.style.backgroundColor = Color.clear;
        hudScreen.pickingMode = PickingMode.Ignore;

        HideElement(root.Q<VisualElement>("world"));
        HideElement(root.Q<VisualElement>(className: "hud__fader-note"));
        HideElement(root.Q<VisualElement>("warningPanel"));
        HideElement(root.Q<VisualElement>("skillPointToast"));

        heldItemBox = root.Q<VisualElement>("heldItem");
        if (heldItemBox != null)
        {
            List<Label> labels = heldItemBox.Query<Label>().ToList();
            if (labels.Count > 0)
                heldItemNameLabel = labels[labels.Count - 1];
        }
        contextHintsBox = root.Q<VisualElement>("contextHints");

        HideElement(contextHintsBox);
        HideElement(heldItemBox);

        dayLabel = root.Q<Label>("dayLabel");
        clockLabel = root.Q<Label>("clockLabel");
        moneyLabel = root.Q<Label>("moneyLabel");
        billBadge = root.Q<Label>("billBadge");
        mailBadge = root.Q<Label>("mailBadge");
        itemNameLabel = root.Q<Label>("itemNameLabel");
        interactionHints = root.Q<VisualElement>("interactionHints");
        bottomInfo = root.Q<VisualElement>("bottomInfo");

        Label shortcutNote = root.Q<Label>("shortcutNote");
        if (shortcutNote != null)
            shortcutNote.text = "ESC: Pause · I/Tab: Kho · C: PC · B: Hóa đơn";

        if (mailBadge != null)
            mailBadge.style.display = DisplayStyle.None;

        if (bottomInfo != null)
            bottomInfo.Clear();

        GameWorld world = GameWorld.Instance;
        world.Clock.MinutePassed += RefreshClock;
        world.Clock.DayPassed += RefreshClock;
        world.Economy.MoneyChanged += RefreshMoney;
        world.Economy.BillsChanged += RefreshBills;
        world.Stream.Donated += OnStreamDonated;

        if (interactor == null)
            interactor = FindFirstObjectByType<PlayerInteractor>();
        if (interactor != null)
            interactor.TargetChanged += RefreshPrompt;

        if (carrier == null)
            carrier = FindFirstObjectByType<ItemCarrier>();
        if (carrier != null)
            carrier.HeldChanged += RefreshHeldItem;

        RefreshClock();
        RefreshMoney(world.Economy.Money);
        RefreshBills();
        RefreshPrompt(interactor != null ? interactor.Current : null);
        RefreshHeldItem(carrier != null ? carrier.Held : null);
    }

    // Hủy đăng ký sự kiện
    void OnDisable()
    {
        if (GameWorld.HasInstance)
        {
            GameWorld world = GameWorld.Instance;
            world.Clock.MinutePassed -= RefreshClock;
            world.Clock.DayPassed -= RefreshClock;
            world.Economy.MoneyChanged -= RefreshMoney;
            world.Economy.BillsChanged -= RefreshBills;
            world.Stream.Donated -= OnStreamDonated;
        }

        if (interactor != null)
            interactor.TargetChanged -= RefreshPrompt;
        if (carrier != null)
            carrier.HeldChanged -= RefreshHeldItem;
    }

    void RefreshHeldItem(WorldItem held)
    {
        bool hasHeld = held != null && held.Data != null;
        if (heldItemBox != null)
            heldItemBox.style.display = hasHeld ? DisplayStyle.Flex : DisplayStyle.None;
        if (contextHintsBox != null)
            contextHintsBox.style.display = hasHeld ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasHeld && heldItemNameLabel != null)
            heldItemNameLabel.text = held.Data.displayName;
    }

    void HideElement(VisualElement element)
    {
        if (element != null)
            element.style.display = DisplayStyle.None;
    }

    void OnStreamDonated(float amount)
    {
        ShowInfo($"+ $ {amount:0.##} từ donate", true);
    }

    // Cập nhật đồng hồ và ngày
    void RefreshClock()
    {
        if (!GameWorld.HasInstance) return;
        GameClock clock = GameWorld.Instance.Clock;
        if (dayLabel != null)
            dayLabel.text = $"Ngày {clock.Day}";
        if (clockLabel != null)
            clockLabel.text = $"{clock.Hour:00}:{clock.Minute:00}";
    }

    // Cập nhật số tiền
    void RefreshMoney(float money)
    {
        if (moneyLabel != null)
            moneyLabel.text = $"$ {money:0.##}";
        RefreshBills();
    }

    // Cập nhật badge số hóa đơn chưa trả
    void RefreshBills()
    {
        if (billBadge == null || !GameWorld.HasInstance) return;
        int count = GameWorld.Instance.Economy.UnreadBillCount;
        billBadge.text = count.ToString();
        billBadge.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Đổi gợi ý phím theo vật đang nhắm tới
    void RefreshPrompt(IInteractable target)
    {
        if (target == null)
        {
            if (itemNameLabel != null)
                itemNameLabel.style.display = DisplayStyle.None;
            if (interactionHints != null)
                interactionHints.style.display = DisplayStyle.None;
            return;
        }

        if (itemNameLabel != null)
        {
            itemNameLabel.text = target.Prompt;
            itemNameLabel.style.display = DisplayStyle.Flex;
        }

        if (interactionHints != null)
        {
            interactionHints.Clear();
            VisualElement row = new VisualElement();
            row.AddToClassList("hud__hint");

            Label keyLabel = new Label("E");
            keyLabel.AddToClassList("wf-key");

            Label actionLabel = new Label(target.Prompt);
            actionLabel.AddToClassList("wf-text");

            row.Add(keyLabel);
            row.Add(actionLabel);
            interactionHints.Add(row);
            interactionHints.style.display = DisplayStyle.Flex;
        }
    }

    // Hiện một dòng thông báo ở cuối màn hình (Bottom Info)
    public void ShowInfo(string message, bool positive)
    {
        if (bottomInfo == null) return;

        Label line = new Label(message);
        line.AddToClassList("wf-text");
        line.AddToClassList("hud__info-line");
        if (positive)
            line.AddToClassList("hud__info-line--positive");

        bottomInfo.Add(line);
        StartCoroutine(RemoveInfoAfterDelay(line, 3.5f));
    }

    IEnumerator RemoveInfoAfterDelay(VisualElement line, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (line != null && line.parent != null)
            line.RemoveFromHierarchy();
    }
}
