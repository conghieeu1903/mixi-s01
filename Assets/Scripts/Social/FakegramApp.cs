using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App mạng xã hội Fakegram: feed ảnh, đăng bài
public class FakegramApp : PcApp
{
    readonly List<string> posts = new List<string>
    {
        "@mixi_daily: Góc stream mới nâng cấp đèn RGB sáng trưng! (128 lượt thích)",
        "@pro_setup: Khoe dàn RTX 4080 vừa giao tới cửa sáng nay (340 lượt thích)",
        "@cat_lover: Boss mèo nằm ngủ ngay cạnh case máy tính (512 lượt thích)"
    };

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
        posts.Insert(0, $"@ban_streamer: {text} (Vừa xong)");
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

        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 10;

        Label title = new Label("Fakegram — Mạng xã hội Streamer");
        title.AddToClassList("wf-subheading");

        Button postBtn = new Button(() =>
        {
            CreatePost("Check-in góc máy trước giờ lên sóng!");
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã đăng bài lên Fakegram (+8 Follower, +3 Mood)", true);
            BuildWindowContent(content);
        })
        {
            text = "Đăng bài mới (+Follower)"
        };
        postBtn.AddToClassList("wf-button");
        postBtn.AddToClassList("wf-button--primary");
        postBtn.AddToClassList("wf-button--small");

        header.Add(title);
        header.Add(postBtn);
        content.Add(header);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        for (int i = 0; i < posts.Count; i++)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("wf-panel");
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.marginBottom = 6;

            Label lbl = new Label(posts[i]);
            lbl.AddToClassList("wf-text");
            card.Add(lbl);
            scroll.Add(card);
        }
        content.Add(scroll);
    }
}
