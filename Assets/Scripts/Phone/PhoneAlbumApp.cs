using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng Album xem lại ảnh đã chụp và phóng to ảnh toàn màn hình theo cơ chế AlbumAppsPanel của SLS2
public class PhoneAlbumApp : PhoneApp
{
    int selectedPhotoIndex = -1;
    VisualElement currentContent;

    public override string IconGlyph => "🖼";
    public override Color AccentColor => new Color(0.68f, 0.36f, 0.92f, 1f);
    public override bool BlockWhileDriving => false;

    void Awake()
    {
        SetDefaultAppName("Album");
    }

    public override void OnOpen()
    {
        selectedPhotoIndex = -1;
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        content.Clear();

        PhoneCameraApp camApp = FindFirstObjectByType<PhoneCameraApp>();
        int count = camApp != null ? camApp.CapturedRecords.Count : 0;

        if (selectedPhotoIndex >= 0 && selectedPhotoIndex < count)
        {
            BuildBigImageView(content, camApp, selectedPhotoIndex);
            return;
        }

        VisualElement topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        topRow.style.marginBottom = 8;

        Label sub = new Label($"Bộ sưu tập ảnh ({count} tấm)");
        sub.style.color = new Color(0.82f, 0.88f, 0.96f, 1f);
        sub.style.unityFontStyleAndWeight = FontStyle.Bold;
        sub.style.fontSize = 12;

        Button openCamBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            if (camApp != null && Phone != null)
                Phone.Launch(camApp);
        })
        {
            text = "📷 Mở Camera"
        };
        openCamBtn.style.backgroundColor = new Color(0.28f, 0.56f, 0.96f, 1f);
        openCamBtn.style.color = Color.white;
        openCamBtn.style.borderTopLeftRadius = 6;
        openCamBtn.style.borderTopRightRadius = 6;
        openCamBtn.style.borderBottomLeftRadius = 6;
        openCamBtn.style.borderBottomRightRadius = 6;
        openCamBtn.style.paddingLeft = 8;
        openCamBtn.style.paddingRight = 8;
        openCamBtn.style.paddingTop = 4;
        openCamBtn.style.paddingBottom = 4;

        topRow.Add(sub);
        topRow.Add(openCamBtn);
        content.Add(topRow);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        if (count == 0)
        {
            VisualElement emptyBox = new VisualElement();
            emptyBox.style.alignItems = Align.Center;
            emptyBox.style.justifyContent = Justify.Center;
            emptyBox.style.marginTop = 48;

            Label emptyIcon = new Label("🖼");
            emptyIcon.style.fontSize = 36;
            emptyIcon.style.marginBottom = 8;

            Label emptyTxt = new Label("Chưa có bức ảnh nào trong Album.\nHãy mở ứng dụng Camera để chụp ảnh lưu niệm!");
            emptyTxt.style.color = new Color(0.72f, 0.78f, 0.86f, 1f);
            emptyTxt.style.fontSize = 12;
            emptyTxt.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyTxt.style.whiteSpace = WhiteSpace.Normal;

            emptyBox.Add(emptyIcon);
            emptyBox.Add(emptyTxt);
            scroll.Add(emptyBox);
        }
        else
        {
            VisualElement grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;

            for (int i = count - 1; i >= 0; i--)
            {
                int idx = i;
                PhonePhotoRecord rec = camApp.CapturedRecords[idx];

                VisualElement thumbCard = new VisualElement();
                thumbCard.style.width = Length.Percent(48f);
                thumbCard.style.backgroundColor = new Color(0.15f, 0.17f, 0.23f, 0.95f);
                thumbCard.style.borderTopLeftRadius = 8;
                thumbCard.style.borderTopRightRadius = 8;
                thumbCard.style.borderBottomLeftRadius = 8;
                thumbCard.style.borderBottomRightRadius = 8;
                thumbCard.style.paddingLeft = 6;
                thumbCard.style.paddingRight = 6;
                thumbCard.style.paddingTop = 6;
                thumbCard.style.paddingBottom = 6;
                thumbCard.style.marginBottom = 8;

                VisualElement imgBox = new VisualElement();
                imgBox.style.height = 84;
                imgBox.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
                imgBox.style.borderTopLeftRadius = 6;
                imgBox.style.borderTopRightRadius = 6;
                imgBox.style.borderBottomLeftRadius = 6;
                imgBox.style.borderBottomRightRadius = 6;
                imgBox.style.marginBottom = 4;
                if (rec != null && rec.texture != null)
                    imgBox.style.backgroundImage = new StyleBackground(rec.texture);

                Label capLbl = new Label($"Ảnh #{idx + 1} · {rec?.poseName}");
                capLbl.style.color = Color.white;
                capLbl.style.fontSize = 10;
                capLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                capLbl.style.marginBottom = 4;

                Button viewBtn = new Button(() =>
                {
                    AudioManager.PlayUi("phone_tap");
                    selectedPhotoIndex = idx;
                    BuildAppContent(content);
                })
                {
                    text = "Phóng to 🔍"
                };
                viewBtn.style.backgroundColor = new Color(0.68f, 0.36f, 0.92f, 1f);
                viewBtn.style.color = Color.white;
                viewBtn.style.fontSize = 10;
                viewBtn.style.borderTopLeftRadius = 5;
                viewBtn.style.borderTopRightRadius = 5;
                viewBtn.style.borderBottomLeftRadius = 5;
                viewBtn.style.borderBottomRightRadius = 5;
                viewBtn.style.paddingTop = 3;
                viewBtn.style.paddingBottom = 3;

                thumbCard.Add(imgBox);
                thumbCard.Add(capLbl);
                thumbCard.Add(viewBtn);
                grid.Add(thumbCard);
            }

            scroll.Add(grid);
        }

        content.Add(scroll);
    }

    // Dựng chế độ xem ảnh lớn toàn màn hình giống bigImage của SLS2
    void BuildBigImageView(VisualElement content, PhoneCameraApp camApp, int index)
    {
        PhonePhotoRecord rec = camApp.CapturedRecords[index];

        VisualElement bigBox = new VisualElement();
        bigBox.style.height = 195;
        bigBox.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
        bigBox.style.borderTopLeftRadius = 10;
        bigBox.style.borderTopRightRadius = 10;
        bigBox.style.borderBottomLeftRadius = 10;
        bigBox.style.borderBottomRightRadius = 10;
        bigBox.style.marginBottom = 8;
        if (rec != null && rec.texture != null)
            bigBox.style.backgroundImage = new StyleBackground(rec.texture);

        content.Add(bigBox);

        Label caption = new Label(rec != null ? rec.caption : $"Ảnh #{index + 1}");
        caption.style.color = Color.white;
        caption.style.fontSize = 11;
        caption.style.whiteSpace = WhiteSpace.Normal;
        caption.style.marginBottom = 10;
        content.Add(caption);

        Button shareBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_notification");
            if (GameWorld.HasInstance)
            {
                GameWorld.Instance.Stream.AddFollowers(4);
                GameWorld.Instance.Mood.Change(2f);
            }
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã đăng ảnh lên Fakegram! (+4 Follower, +2 Mood)", true);
        })
        {
            text = "✨ Đăng ảnh lên Fakegram (+4 Follower)"
        };
        shareBtn.style.backgroundColor = new Color(0.88f, 0.28f, 0.58f, 1f);
        shareBtn.style.color = Color.white;
        shareBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        shareBtn.style.borderTopLeftRadius = 8;
        shareBtn.style.borderTopRightRadius = 8;
        shareBtn.style.borderBottomLeftRadius = 8;
        shareBtn.style.borderBottomRightRadius = 8;
        shareBtn.style.paddingTop = 7;
        shareBtn.style.paddingBottom = 7;
        shareBtn.style.marginBottom = 6;
        content.Add(shareBtn);

        Button backBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            selectedPhotoIndex = -1;
            BuildAppContent(content);
        })
        {
            text = "← Quay lại danh sách Album"
        };
        backBtn.style.backgroundColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        backBtn.style.color = Color.white;
        backBtn.style.borderTopLeftRadius = 8;
        backBtn.style.borderTopRightRadius = 8;
        backBtn.style.borderBottomLeftRadius = 8;
        backBtn.style.borderBottomRightRadius = 8;
        backBtn.style.paddingTop = 6;
        backBtn.style.paddingBottom = 6;
        content.Add(backBtn);
    }
}
