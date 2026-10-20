using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Overlay khi đang stream: viewer, follower, thời lượng, thu nhập, popup donate, thẻ và minigame
[RequireComponent(typeof(UIDocument))]
public class StreamOverlayView : MonoBehaviour
{
    static readonly string[] DonateMessages = { "Chúc anh stream vui vẻ!", "Mời anh ly cà phê!", "Tiếp tục phát huy nhé!", "Fan cứng ủng hộ kênh!" };

    UIDocument document;
    ChatView chatView;
    VisualElement rootOverlay;
    Label viewersLabel;
    Label followersLabel;
    Label durationLabel;
    Label incomeLabel;
    Label infoLabel;
    VisualElement donatePopup;
    Label donateTitle;
    Label donateMessage;

    float sessionIncome;
    int startFollowers;
    float chatTimer;
    Coroutine donateRoutine;
    ComputerDesk cachedDesk;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        chatView = GetComponent<ChatView>();
        if (chatView == null)
            chatView = gameObject.AddComponent<ChatView>();
    }

    // Đăng ký sự kiện của StreamSystem
    void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (chatView == null) chatView = GetComponent<ChatView>() ?? gameObject.AddComponent<ChatView>();
        VisualElement root = document.rootVisualElement;
        if (root == null) return;

        rootOverlay = root.Q<VisualElement>("streamOverlay") ?? root;
        rootOverlay.style.backgroundColor = Color.clear;
        SetPickingIgnoreRecursive(rootOverlay);

        VisualElement streamView = root.Q<VisualElement>("streamView");
        if (streamView != null)
            streamView.style.display = DisplayStyle.None;

        viewersLabel = root.Q<Label>("viewersLabel");
        followersLabel = root.Q<Label>("followersLabel");
        durationLabel = root.Q<Label>("durationLabel");
        incomeLabel = root.Q<Label>("incomeLabel");
        infoLabel = root.Q<Label>("infoLabel");
        donatePopup = root.Q<VisualElement>("donatePopup");
        donateTitle = root.Q<Label>("donateTitle");
        donateMessage = root.Q<Label>("donateMessage");

        if (donatePopup != null)
            donatePopup.style.display = DisplayStyle.None;

        EnsureActionToolbar(rootOverlay);

        StreamSystem stream = GameWorld.Instance.Stream;
        stream.Opened += OnStreamOpened;
        stream.Closed += OnStreamClosed;
        stream.StatsChanged += RefreshStats;
        stream.Donated += ShowDonate;

        if (stream.IsOpen)
            OnStreamOpened();
        else
            rootOverlay.style.display = DisplayStyle.None;
    }

    // Hủy đăng ký sự kiện
    void OnDisable()
    {
        if (GameWorld.HasInstance)
        {
            StreamSystem stream = GameWorld.Instance.Stream;
            stream.Opened -= OnStreamOpened;
            stream.Closed -= OnStreamClosed;
            stream.StatsChanged -= RefreshStats;
            stream.Donated -= ShowDonate;
        }
    }

    void EnsureActionToolbar(VisualElement host)
    {
        if (host == null || host.Q<VisualElement>("streamActionBar") != null) return;

        VisualElement bar = new VisualElement { name = "streamActionBar" };
        bar.style.position = Position.Absolute;
        bar.style.left = 24;
        bar.style.bottom = 24;
        bar.style.flexDirection = FlexDirection.Row;
        bar.pickingMode = PickingMode.Position;

        Button cardBtn = new Button(OpenCards) { text = "Rút thẻ (F1)" };
        cardBtn.AddToClassList("wf-button");
        cardBtn.AddToClassList("wf-button--primary");
        cardBtn.AddToClassList("wf-button--small");
        cardBtn.style.marginRight = 8;
        cardBtn.pickingMode = PickingMode.Position;

        Button mgBtn = new Button(StartCategoryMinigame) { text = "Minigame (F2)" };
        mgBtn.AddToClassList("wf-button");
        mgBtn.AddToClassList("wf-button--small");
        mgBtn.pickingMode = PickingMode.Position;

        bar.Add(cardBtn);
        bar.Add(mgBtn);
        host.Add(bar);
    }

    void LateUpdate()
    {
        if (rootOverlay == null || !GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;
        if (cachedDesk == null)
            cachedDesk = FindFirstObjectByType<ComputerDesk>();

        if (cachedDesk != null && cachedDesk.IsOccupied)
        {
            cachedDesk.FitElementToMonitorScreen(rootOverlay);
        }
        else
        {
            rootOverlay.style.position = Position.Absolute;
            rootOverlay.style.left = Length.Percent(0f);
            rootOverlay.style.top = Length.Percent(0f);
            rootOverlay.style.width = Length.Percent(100f);
            rootOverlay.style.height = Length.Percent(100f);
        }
    }

    void Update()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f1Key.wasPressedThisFrame)
                OpenCards();
            else if (kb.f2Key.wasPressedThisFrame)
                StartCategoryMinigame();
        }

        chatTimer += Time.deltaTime;
        if (chatTimer >= 2.5f)
        {
            chatTimer = 0f;
            ChatMessage msg = GameWorld.Instance.Chat.PostRandomMessage(GameWorld.Instance.Stream.Category);
            GameWorld.Instance.Moderation.Track(msg);
            chatView?.Append(msg);
        }
    }

    public void OpenCards()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;
        StreamCardView cardView = FindFirstObjectByType<StreamCardView>();
        if (cardView != null)
            cardView.ShowCards(GameWorld.Instance.Cards.Draw(3));
    }

    public void StartCategoryMinigame()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;
        StreamCategory cat = GameWorld.Instance.Stream.Category;
        StreamMinigame mg = null;
        if (cat == StreamCategory.Asmr)
            mg = FindFirstObjectByType<AsmrGame>();
        else if (cat == StreamCategory.Dancing)
            mg = FindFirstObjectByType<DanceGame>();
        if (mg == null)
            mg = FindFirstObjectByType<JustChattingGame>();

        StreamCardView cardView = FindFirstObjectByType<StreamCardView>();
        if (cardView != null && mg != null)
            cardView.ShowMinigame(mg);
    }

    void SetPickingIgnoreRecursive(VisualElement element)
    {
        if (element == null) return;
        element.pickingMode = PickingMode.Ignore;
        foreach (VisualElement child in element.Children())
            SetPickingIgnoreRecursive(child);
    }

    void OnStreamOpened()
    {
        StreamSystem stream = GameWorld.Instance.Stream;
        sessionIncome = 0f;
        startFollowers = stream.Followers;
        chatTimer = 0f;

        chatView?.Clear();
        if (donatePopup != null)
            donatePopup.style.display = DisplayStyle.None;
        if (rootOverlay != null)
            rootOverlay.style.display = DisplayStyle.Flex;

        chatView?.Append(new ChatMessage
        {
            user = "Hệ thống",
            text = "Buổi stream đã bắt đầu! Nhấn F1 rút thẻ, F2 chơi minigame.",
            isToxic = false
        });
        RefreshStats();
    }

    void OnStreamClosed(StreamRecord record)
    {
        if (donateRoutine != null)
        {
            StopCoroutine(donateRoutine);
            donateRoutine = null;
        }
        if (donatePopup != null)
            donatePopup.style.display = DisplayStyle.None;
        if (rootOverlay != null)
            rootOverlay.style.display = DisplayStyle.None;
    }

    // Cập nhật các chỉ số stream
    void RefreshStats()
    {
        if (!GameWorld.HasInstance) return;
        StreamSystem stream = GameWorld.Instance.Stream;

        if (viewersLabel != null)
            viewersLabel.text = stream.Viewers.ToString("N0");

        if (followersLabel != null)
        {
            int gained = Mathf.Max(0, stream.Followers - startFollowers);
            followersLabel.text = gained > 0 ? $"{stream.Followers:N0} (+{gained})" : stream.Followers.ToString("N0");
        }

        if (durationLabel != null)
        {
            int hours = stream.StreamMinutes / 60;
            int mins = stream.StreamMinutes % 60;
            durationLabel.text = $"{hours:00}:{mins:00}:00";
        }

        if (incomeLabel != null)
            incomeLabel.text = $"$ {sessionIncome:0.##}";

        if (infoLabel != null)
            infoLabel.text = $"Chủ đề: {stream.Category} · Sub: {stream.Subs:N0} · Giới hạn viewer: {stream.ViewerLimit:N0}";
    }

    // Hiện popup donate
    void ShowDonate(float amount)
    {
        sessionIncome += amount;
        RefreshStats();

        string donor = "fan_mixi";
        string message = DonateMessages[Random.Range(0, DonateMessages.Length)];

        chatView?.Append(new ChatMessage
        {
            user = donor,
            text = $"donate $ {amount:0.##} — {message}",
            donate = amount,
            isToxic = false
        });

        if (donateTitle != null)
            donateTitle.text = $"{donor} donate ${amount:0.##}";
        if (donateMessage != null)
            donateMessage.text = $"\"{message}\"";

        if (donatePopup != null)
        {
            if (donateRoutine != null)
                StopCoroutine(donateRoutine);
            donateRoutine = StartCoroutine(ShowDonateRoutine());
        }
    }

    IEnumerator ShowDonateRoutine()
    {
        donatePopup.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(3.5f);
        donatePopup.style.display = DisplayStyle.None;
        donateRoutine = null;
    }
}
