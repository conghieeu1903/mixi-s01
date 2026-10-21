using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Bản ghi một bức ảnh chụp từ Camera điện thoại
public class PhonePhotoRecord
{
    public string caption;
    public string poseName;
    public bool smiling;
    public Texture2D texture;
}

// Ứng dụng Camera theo cơ chế CameraApps của SLS2: kính ngắm trực tiếp, chọn tư thế/nụ cười, chớp flash kép và lưu Texture2D
public class PhoneCameraApp : PhoneApp
{
    static readonly string[] PoseNames =
    {
        "Tự nhiên",
        "Giơ tay Peace ✌",
        "Khoanh tay ngầu",
        "Thả tim ♥",
        "Flex cơ bắp"
    };

    public List<string> AlbumPhotos { get; } = new List<string>();
    public List<PhonePhotoRecord> CapturedRecords { get; } = new List<PhonePhotoRecord>();

    int poseIndex;
    bool isSmiling = true;
    bool isCapturing;
    Texture2D livePreviewTexture;
    VisualElement currentContent;
    VisualElement whiteFlashOverlay;
    VisualElement captureLoadingBox;
    VisualElement captureLoadingFill;
    Coroutine captureRoutine;

    public override string IconGlyph => "📷";
    public override Color AccentColor => new Color(0.28f, 0.56f, 0.96f, 1f);
    public override bool BlockWhileDriving => true;

    void Awake()
    {
        SetDefaultAppName("Camera");
    }

    public override void OnOpen()
    {
        isCapturing = false;
        UpdateLiveViewfinder();
    }

    // Chụp ảnh nhanh và lưu vào danh sách Album
    public void TakePhoto()
    {
        AudioManager.Play("phone_shutter");
        int day = GameWorld.HasInstance ? GameWorld.Instance.Clock.Day : 1;
        int hour = GameWorld.HasInstance ? GameWorld.Instance.Clock.Hour : 8;
        int min = GameWorld.HasInstance ? GameWorld.Instance.Clock.Minute : 0;
        string pose = PoseNames[Mathf.Clamp(poseIndex, 0, PoseNames.Length - 1)];
        string smileTag = isSmiling ? "Cười tươi" : "Mặt ngầu";
        string entry = $"Ảnh #{AlbumPhotos.Count + 1} — Ngày {day} {hour:00}:{min:00} ({pose}, {smileTag})";
        AlbumPhotos.Add(entry);

        Texture2D snap = CaptureCameraTexture(256, 192);
        CapturedRecords.Add(new PhonePhotoRecord
        {
            caption = entry,
            poseName = pose,
            smiling = isSmiling,
            texture = snap
        });

        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã chụp và lưu vào Album ({entry})", true);
    }

    // Chụp khung hình thực tế từ Camera chính ra Texture2D giống CaptureSnapshot của SLS2
    Texture2D CaptureCameraTexture(int width, int height)
    {
        Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        if (cam != null)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture prevCamRt = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();
            }
            finally
            {
                cam.targetTexture = prevCamRt;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
        else
        {
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(45, 72, 115, 255);
            result.SetPixels32(pixels);
            result.Apply();
        }
        return result;
    }

    void UpdateLiveViewfinder()
    {
        if (livePreviewTexture != null)
            Destroy(livePreviewTexture);
        livePreviewTexture = CaptureCameraTexture(480, 360);
    }

    // Chạy hiệu ứng nháy flash trắng 2 lần và thanh lưu ảnh giống CaptureProcess của SLS2
    void StartCaptureProcess()
    {
        if (isCapturing) return;
        if (!isActiveAndEnabled)
        {
            TakePhoto();
            return;
        }
        if (captureRoutine != null)
            StopCoroutine(captureRoutine);
        captureRoutine = StartCoroutine(CaptureProcessRoutine());
    }

    IEnumerator CaptureProcessRoutine()
    {
        isCapturing = true;
        TakePhoto();
        UpdateLiveViewfinder();

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.style.display = DisplayStyle.Flex;
            yield return new WaitForSecondsRealtime(0.09f);
            whiteFlashOverlay.style.display = DisplayStyle.None;
            yield return new WaitForSecondsRealtime(0.08f);
            whiteFlashOverlay.style.display = DisplayStyle.Flex;
            yield return new WaitForSecondsRealtime(0.09f);
            whiteFlashOverlay.style.display = DisplayStyle.None;
        }

        if (captureLoadingBox != null && captureLoadingFill != null)
        {
            captureLoadingBox.style.display = DisplayStyle.Flex;
            const float saveTime = 0.55f;
            float elapsed = 0f;
            while (elapsed < saveTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float pct = Mathf.Clamp01(elapsed / saveTime) * 100f;
                captureLoadingFill.style.width = Length.Percent(pct);
                yield return null;
            }
            captureLoadingBox.style.display = DisplayStyle.None;
        }

        isCapturing = false;
        captureRoutine = null;
        if (currentContent != null)
            BuildAppContent(currentContent);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        content.Clear();

        if (livePreviewTexture == null)
            UpdateLiveViewfinder();

        // Khung kính ngắm Camera trực tiếp mở rộng lấp đầy màn hình điện thoại kèm lớp chớp trắng whiteBox
        VisualElement viewfinder = new VisualElement();
        viewfinder.style.flexGrow = 1;
        viewfinder.style.minHeight = 320;
        viewfinder.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
        viewfinder.style.borderTopLeftRadius = 12;
        viewfinder.style.borderTopRightRadius = 12;
        viewfinder.style.borderBottomLeftRadius = 12;
        viewfinder.style.borderBottomRightRadius = 12;
        viewfinder.style.overflow = Overflow.Hidden;
        viewfinder.style.marginBottom = 10;
        if (livePreviewTexture != null)
            viewfinder.style.backgroundImage = new StyleBackground(livePreviewTexture);

        // Khung ngắm tiêu cự ở giữa màn hình
        VisualElement reticle = new VisualElement();
        reticle.style.position = Position.Absolute;
        reticle.style.left = 12;
        reticle.style.right = 12;
        reticle.style.top = 12;
        reticle.style.bottom = 12;
        reticle.style.borderTopWidth = 1;
        reticle.style.borderBottomWidth = 1;
        reticle.style.borderLeftWidth = 1;
        reticle.style.borderRightWidth = 1;
        reticle.style.borderTopColor = new Color(1f, 1f, 1f, 0.45f);
        reticle.style.borderBottomColor = new Color(1f, 1f, 1f, 0.45f);
        reticle.style.borderLeftColor = new Color(1f, 1f, 1f, 0.45f);
        reticle.style.borderRightColor = new Color(1f, 1f, 1f, 0.45f);

        Label hudBadge = new Label($"● LIVE · {PoseNames[poseIndex]} · {(isSmiling ? "😊 Cười" : "😎 Ngầu")}");
        hudBadge.style.position = Position.Absolute;
        hudBadge.style.left = 8;
        hudBadge.style.bottom = 6;
        hudBadge.style.backgroundColor = new Color(0f, 0f, 0f, 0.62f);
        hudBadge.style.color = Color.white;
        hudBadge.style.fontSize = 10;
        hudBadge.style.paddingLeft = 6;
        hudBadge.style.paddingRight = 6;
        hudBadge.style.paddingTop = 2;
        hudBadge.style.paddingBottom = 2;
        hudBadge.style.borderTopLeftRadius = 4;
        hudBadge.style.borderTopRightRadius = 4;
        hudBadge.style.borderBottomLeftRadius = 4;
        hudBadge.style.borderBottomRightRadius = 4;
        reticle.Add(hudBadge);
        viewfinder.Add(reticle);

        whiteFlashOverlay = new VisualElement();
        whiteFlashOverlay.style.position = Position.Absolute;
        whiteFlashOverlay.style.left = 0;
        whiteFlashOverlay.style.right = 0;
        whiteFlashOverlay.style.top = 0;
        whiteFlashOverlay.style.bottom = 0;
        whiteFlashOverlay.style.backgroundColor = Color.white;
        whiteFlashOverlay.style.display = DisplayStyle.None;
        viewfinder.Add(whiteFlashOverlay);

        captureLoadingBox = new VisualElement();
        captureLoadingBox.style.position = Position.Absolute;
        captureLoadingBox.style.left = 0;
        captureLoadingBox.style.right = 0;
        captureLoadingBox.style.top = 0;
        captureLoadingBox.style.bottom = 0;
        captureLoadingBox.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
        captureLoadingBox.style.alignItems = Align.Center;
        captureLoadingBox.style.justifyContent = Justify.Center;
        captureLoadingBox.style.display = DisplayStyle.None;

        Label savingLbl = new Label("Đang xử lý & lưu ảnh...");
        savingLbl.style.color = Color.white;
        savingLbl.style.fontSize = 11;
        savingLbl.style.marginBottom = 6;

        VisualElement barBg = new VisualElement();
        barBg.style.width = 160;
        barBg.style.height = 8;
        barBg.style.backgroundColor = new Color(1f, 1f, 1f, 0.2f);
        barBg.style.borderTopLeftRadius = 4;
        barBg.style.borderTopRightRadius = 4;
        barBg.style.borderBottomLeftRadius = 4;
        barBg.style.borderBottomRightRadius = 4;
        barBg.style.overflow = Overflow.Hidden;

        captureLoadingFill = new VisualElement();
        captureLoadingFill.style.width = Length.Percent(0f);
        captureLoadingFill.style.height = Length.Percent(100f);
        captureLoadingFill.style.backgroundColor = new Color(0.28f, 0.76f, 0.98f, 1f);
        barBg.Add(captureLoadingFill);

        captureLoadingBox.Add(savingLbl);
        captureLoadingBox.Add(barBg);
        viewfinder.Add(captureLoadingBox);

        content.Add(viewfinder);

        // Bộ chọn tư thế tạo dáng giống CharacterPosSlider của SLS2
        VisualElement poseBox = new VisualElement();
        poseBox.style.flexDirection = FlexDirection.Row;
        poseBox.style.alignItems = Align.Center;
        poseBox.style.justifyContent = Justify.SpaceBetween;
        poseBox.style.backgroundColor = new Color(0.15f, 0.18f, 0.24f, 0.95f);
        poseBox.style.borderTopLeftRadius = 8;
        poseBox.style.borderTopRightRadius = 8;
        poseBox.style.borderBottomLeftRadius = 8;
        poseBox.style.borderBottomRightRadius = 8;
        poseBox.style.paddingLeft = 8;
        poseBox.style.paddingRight = 8;
        poseBox.style.paddingTop = 5;
        poseBox.style.paddingBottom = 5;
        poseBox.style.marginBottom = 6;

        Button prevPoseBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            poseIndex = (poseIndex - 1 + PoseNames.Length) % PoseNames.Length;
            BuildAppContent(content);
        })
        {
            text = "◀"
        };
        StyleSmallBtn(prevPoseBtn);

        Label poseLbl = new Label($"Tư thế {poseIndex + 1}/{PoseNames.Length}: {PoseNames[poseIndex]}");
        poseLbl.style.color = Color.white;
        poseLbl.style.fontSize = 11;
        poseLbl.style.unityFontStyleAndWeight = FontStyle.Bold;

        Button nextPoseBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            poseIndex = (poseIndex + 1) % PoseNames.Length;
            BuildAppContent(content);
        })
        {
            text = "▶"
        };
        StyleSmallBtn(nextPoseBtn);

        poseBox.Add(prevPoseBtn);
        poseBox.Add(poseLbl);
        poseBox.Add(nextPoseBtn);
        content.Add(poseBox);

        // Hàng chọn biểu cảm Cười / Ngầu giống SmileButton / NoSmileButton của SLS2
        VisualElement smileRow = new VisualElement();
        smileRow.style.flexDirection = FlexDirection.Row;
        smileRow.style.marginBottom = 8;

        Button smileBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            isSmiling = true;
            BuildAppContent(content);
        })
        {
            text = "😊 Mỉm cười"
        };
        StyleToggleBtn(smileBtn, isSmiling);

        Button coolBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            isSmiling = false;
            BuildAppContent(content);
        })
        {
            text = "😎 Lạnh lùng"
        };
        StyleToggleBtn(coolBtn, !isSmiling);

        smileRow.Add(smileBtn);
        smileRow.Add(coolBtn);
        content.Add(smileRow);

        // Nút bấm chụp ảnh lớn và nút mở nhanh Album
        Button snapBtn = new Button(StartCaptureProcess)
        {
            text = "📸 CHỤP ẢNH (+2 Mood)"
        };
        snapBtn.style.backgroundColor = new Color(0.28f, 0.56f, 0.96f, 1f);
        snapBtn.style.color = Color.white;
        snapBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        snapBtn.style.fontSize = 13;
        snapBtn.style.borderTopLeftRadius = 18;
        snapBtn.style.borderTopRightRadius = 18;
        snapBtn.style.borderBottomLeftRadius = 18;
        snapBtn.style.borderBottomRightRadius = 18;
        snapBtn.style.paddingTop = 9;
        snapBtn.style.paddingBottom = 9;
        snapBtn.style.marginBottom = 6;
        content.Add(snapBtn);

        Button openAlbumBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            PhoneAlbumApp album = FindFirstObjectByType<PhoneAlbumApp>();
            if (album != null && Phone != null)
                Phone.Launch(album);
        })
        {
            text = $"🖼 Mở Album ({AlbumPhotos.Count} ảnh đã lưu)"
        };
        openAlbumBtn.style.backgroundColor = new Color(0.2f, 0.24f, 0.32f, 1f);
        openAlbumBtn.style.color = new Color(0.85f, 0.9f, 0.98f, 1f);
        openAlbumBtn.style.borderTopLeftRadius = 8;
        openAlbumBtn.style.borderTopRightRadius = 8;
        openAlbumBtn.style.borderBottomLeftRadius = 8;
        openAlbumBtn.style.borderBottomRightRadius = 8;
        openAlbumBtn.style.paddingTop = 6;
        openAlbumBtn.style.paddingBottom = 6;
        content.Add(openAlbumBtn);
    }

    void StyleSmallBtn(Button btn)
    {
        btn.style.backgroundColor = new Color(0.25f, 0.3f, 0.38f, 1f);
        btn.style.color = Color.white;
        btn.style.borderTopLeftRadius = 6;
        btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6;
        btn.style.borderBottomRightRadius = 6;
        btn.style.paddingLeft = 8;
        btn.style.paddingRight = 8;
        btn.style.paddingTop = 3;
        btn.style.paddingBottom = 3;
    }

    void StyleToggleBtn(Button btn, bool active)
    {
        btn.style.flexGrow = 1;
        btn.style.flexShrink = 1;
        btn.style.flexBasis = 0;
        btn.style.marginLeft = 3;
        btn.style.marginRight = 3;
        btn.style.paddingLeft = 6;
        btn.style.paddingRight = 6;
        btn.style.paddingTop = 8;
        btn.style.paddingBottom = 8;
        btn.style.borderTopLeftRadius = 8;
        btn.style.borderTopRightRadius = 8;
        btn.style.borderBottomLeftRadius = 8;
        btn.style.borderBottomRightRadius = 8;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.fontSize = 13;
        btn.style.backgroundColor = active
            ? new Color(0.28f, 0.56f, 0.96f, 1f)
            : new Color(0.16f, 0.19f, 0.25f, 1f);
        btn.style.color = Color.white;
    }
}
