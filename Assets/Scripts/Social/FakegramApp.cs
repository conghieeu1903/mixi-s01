using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App mạng xã hội Fakegram trên Windows Desktop: feed ảnh, đăng bài tăng follower & mood
public class FakegramApp : PcApp
{
    readonly List<string> posts = new List<string>
    {
        "@mixi_daily: Góc stream mới nâng cấp đèn RGB sáng trưng! (❤️ 128 lượt thích)",
        "@pro_setup: Khoe dàn RTX 4080 vừa giao tới cửa sáng nay (❤️ 340 lượt thích)",
        "@cat_lover: Boss mèo nằm ngủ ngay cạnh case máy tính (❤️ 512 lượt thích)"
    };

    public override string AppName => "Fakegram";
    public int PostCount => posts.Count;

    void Reset()
    {
        SetDefaultAppName("Fakegram");
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(AppName))
            SetDefaultAppName("Fakegram");
    }

    void SetDefaultAppName(string value)
    {
        var field = typeof(PcApp).GetField("appName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, value);
    }

    // Đăng bài mới để tăng follower và mood
    public void CreatePost(string caption)
    {
        string text = string.IsNullOrEmpty(caption) ? "Chuẩn bị lên sóng tối nay nhé anh em!" : caption;
        posts.Insert(0, $"@ban_streamer: {text} (❤️ Vừa xong)");
        AudioManager.Play("phone_shutter");
        AudioManager.PlayUi("ui_success");
        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Mood.Change(3f);
            GameWorld.Instance.Stream.AddFollowers(8);
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.11f, 0.12f, 0.16f, 1f);

        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 10;

        Label title = new Label("📸 FAKEGRAM — Bảng tin Streamer");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.96f, 0.38f, 0.72f, 1f);

        Button postBtn = new Button(() =>
        {
            CreatePost("Check-in góc máy trước giờ lên sóng!");
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã đăng bài lên Fakegram (+8 Follower, +3 Mood)", true);
            BuildWindowContent(content);
        })
        {
            text = "＋ Đăng bài mới (+Follower)"
        };
        postBtn.style.height = 28;
        postBtn.style.paddingLeft = 12;
        postBtn.style.paddingRight = 12;
        postBtn.style.fontSize = 11;
        postBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        postBtn.style.color = Color.white;
        postBtn.style.backgroundColor = new Color(0.84f, 0.24f, 0.58f, 1f);

        header.Add(title);
        header.Add(postBtn);
        content.Add(header);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        for (int i = 0; i < posts.Count; i++)
        {
            VisualElement card = new VisualElement();
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.marginBottom = 6;
            card.style.backgroundColor = new Color(0.16f, 0.18f, 0.23f, 1f);
            card.style.borderTopLeftRadius = 6;
            card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = 6;
            card.style.borderBottomRightRadius = 6;
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = new Color(0.84f, 0.24f, 0.58f, 1f);

            Label lbl = new Label(posts[i]);
            lbl.style.fontSize = 12;
            lbl.style.color = Color.white;
            card.Add(lbl);
            scroll.Add(card);
        }
        content.Add(scroll);
    }
}
