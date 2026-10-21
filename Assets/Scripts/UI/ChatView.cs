using UnityEngine;
using UnityEngine.UIElements;

// Khung chat trên stream overlay, có nút cấm người xem
[RequireComponent(typeof(UIDocument))]
public class ChatView : MonoBehaviour
{
    [Tooltip("Số tin nhắn tối đa hiển thị")]
    [SerializeField] int maxLines = 15;

    UIDocument document;
    VisualElement chatList;

    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    void EnsureList()
    {
        if (document == null) document = GetComponent<UIDocument>();
        VisualElement root = document != null ? document.rootVisualElement : null;
        if (root != null && chatList == null)
            chatList = root.Q<VisualElement>("chatList");
    }

    public void Clear()
    {
        EnsureList();
        chatList?.Clear();
    }

    // Thêm một tin nhắn vào khung chat
    public void Append(ChatMessage message)
    {
        if (message == null) return;
        if (GameWorld.HasInstance && GameWorld.Instance.Moderation.BannedUsers.Contains(message.user))
            return;

        EnsureList();
        if (chatList == null) return;

        while (chatList.childCount >= maxLines)
            chatList.RemoveAt(0);

        VisualElement line = new VisualElement();
        line.AddToClassList("stream__chat-line");
        if (message.donate > 0f || message.isToxic)
            line.AddToClassList("stream__chat-line--donate");

        Label userLabel = new Label($"{message.user}:");
        userLabel.AddToClassList("wf-text");
        userLabel.AddToClassList("stream__chat-user");

        Label msgLabel = new Label(message.text);
        msgLabel.AddToClassList("wf-text");
        msgLabel.style.flexGrow = 1;

        line.Add(userLabel);
        line.Add(msgLabel);

        if (message.isToxic)
        {
            Button banBtn = new Button(() =>
            {
                AudioManager.PlayUi("ui_cancel");
                if (GameWorld.HasInstance)
                    GameWorld.Instance.Moderation.Ban(message.user);
                line.RemoveFromHierarchy();
                FindFirstObjectByType<HudView>()?.ShowInfo($"Đã cấm {message.user} khỏi kênh chat", true);
            })
            {
                text = "Cấm"
            };
            banBtn.AddToClassList("wf-button");
            banBtn.AddToClassList("wf-button--small");
            banBtn.pickingMode = PickingMode.Position;
            line.Add(banBtn);
        }

        chatList.Add(line);
    }
}
