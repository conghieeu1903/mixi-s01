using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App camera: chụp ảnh lưu vào album
public class PhoneCameraApp : PhoneApp
{
    public List<string> AlbumPhotos { get; } = new List<string>();

    void Awake()
    {
        SetDefaultAppName("Camera");
    }

    public void TakePhoto()
    {
        int day = GameWorld.HasInstance ? GameWorld.Instance.Clock.Day : 1;
        int hour = GameWorld.HasInstance ? GameWorld.Instance.Clock.Hour : 8;
        int min = GameWorld.HasInstance ? GameWorld.Instance.Clock.Minute : 0;
        string entry = $"Ảnh #{AlbumPhotos.Count + 1} — Ngày {day} lúc {hour:00}:{min:00}";
        AlbumPhotos.Add(entry);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã chụp và lưu vào Album ({entry})", true);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();

        Label title = new Label($"Camera & Album ({AlbumPhotos.Count} ảnh)");
        title.AddToClassList("wf-subheading");
        title.style.marginBottom = 8;
        content.Add(title);

        Button snapBtn = new Button(() =>
        {
            TakePhoto();
            BuildAppContent(content);
        })
        {
            text = "Chụp ảnh kỷ niệm (+2 Mood)"
        };
        snapBtn.AddToClassList("wf-button");
        snapBtn.AddToClassList("wf-button--primary");
        snapBtn.style.marginBottom = 10;
        content.Add(snapBtn);

        for (int i = AlbumPhotos.Count - 1; i >= 0; i--)
        {
            Label photoLbl = new Label(AlbumPhotos[i]);
            photoLbl.AddToClassList("wf-text");
            content.Add(photoLbl);
        }
    }
}
