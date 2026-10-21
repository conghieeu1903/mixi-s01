using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng Thú cưng theo cơ chế PetAppsPanel của SLS2: dò thú cưng gần bên, mua bóng, ném bóng thu phục và chăm sóc
public class PhonePetApp : PhoneApp
{
    public int BallCount { get; private set; } = 2;

    Pet currentTargetPet;
    bool catchProcessStarted;
    string resultBannerText;
    bool resultPositive = true;
    VisualElement currentContent;
    Coroutine catchRoutine;

    public override string IconGlyph => "🐾";
    public override Color AccentColor => new Color(0.96f, 0.56f, 0.22f, 1f);
    public override bool BlockWhileDriving => true;

    void Awake()
    {
        SetDefaultAppName("Thú cưng");
    }

    public override void OnOpen()
    {
        catchProcessStarted = false;
        resultBannerText = null;
        CheckNearPets();
    }

    // Dò tìm thú cưng đang ở gần người chơi hoặc trong danh sách nuôi giống CheckNearPets của SLS2
    public void CheckNearPets()
    {
        currentTargetPet = null;
        PlayerMotor pm = FindFirstObjectByType<PlayerMotor>();
        Pet[] allPets = FindObjectsByType<Pet>(FindObjectsSortMode.None);

        float bestDist = float.MaxValue;
        for (int i = 0; i < allPets.Length; i++)
        {
            Pet p = allPets[i];
            if (p == null) continue;
            float d = pm != null ? Vector3.Distance(pm.transform.position, p.transform.position) : 0f;
            if (d < bestDist)
            {
                bestDist = d;
                currentTargetPet = p;
            }
        }
    }

    // Mua thêm 1 quả bóng thu phục với giá $25 giống BuyBallButton của SLS2
    public void BuyBallButton()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(25f))
        {
            AudioManager.PlayUi("purchase_fail");
            if (Phone != null)
                Phone.ShowPhoneWarning("Không đủ $25 để mua bóng thu phục!");
            else
                FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ $25 để mua bóng thu phục", false);
            return;
        }

        BallCount++;
        AudioManager.Play("cash_beep");
        RefreshView();
    }

    // Ném bóng thu phục / huấn luyện thú cưng giống CatchButton của SLS2
    public void CatchButton()
    {
        if (currentTargetPet == null)
        {
            CheckNearPets();
            if (currentTargetPet == null)
            {
                if (Phone != null)
                    Phone.ShowPhoneWarning("Không tìm thấy thú cưng nào ở gần!");
                return;
            }
        }

        if (BallCount < 1)
        {
            AudioManager.PlayUi("ui_error");
            if (Phone != null)
                Phone.ShowPhoneWarning("Không còn bóng để ném! Hãy mua bóng ($25).");
            return;
        }

        if (catchProcessStarted) return;
        BallCount--;
        if (isActiveAndEnabled)
        {
            if (catchRoutine != null)
                StopCoroutine(catchRoutine);
            catchRoutine = StartCoroutine(CatchRoutine());
        }
    }

    // Chuỗi âm thanh và hiệu ứng thu phục thú cưng giống CatchRoutine của SLS2
    IEnumerator CatchRoutine()
    {
        catchProcessStarted = true;
        resultBannerText = null;
        RefreshView();

        AudioManager.Play("taming_start_a");
        yield return new WaitForSecondsRealtime(0.45f);
        AudioManager.Play("taming_start_b");
        yield return new WaitForSecondsRealtime(0.45f);
        AudioManager.Play("spin_whoosh");
        yield return new WaitForSecondsRealtime(0.9f);

        catchProcessStarted = false;
        if (currentTargetPet != null)
        {
            currentTargetPet.Feed();
            resultPositive = true;
            resultBannerText = $"THU PHỤC & HUẤN LUYỆN THÀNH CÔNG {currentTargetPet.PetName.ToUpper()}!";
            AudioManager.Play("taming_success");
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã thu phục & chăm sóc {currentTargetPet.PetName}! (+3 Mood, +5% Viewer Bonus)", true);
        }
        else
        {
            resultPositive = false;
            resultBannerText = "Thú cưng đã chạy thoát!";
            AudioManager.Play("taming_fail");
        }

        RefreshView();
        yield return new WaitForSecondsRealtime(2f);
        resultBannerText = null;
        RefreshView();
        catchRoutine = null;
    }

    void RefreshView()
    {
        if (currentContent != null)
            BuildAppContent(currentContent);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        content.Clear();

        CheckNearPets();

        // Thanh hiển thị số bóng hiện có và nút Mua bóng ($25)
        VisualElement topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.alignItems = Align.Center;
        topBar.style.backgroundColor = new Color(0.16f, 0.19f, 0.25f, 0.95f);
        topBar.style.borderTopLeftRadius = 10;
        topBar.style.borderTopRightRadius = 10;
        topBar.style.borderBottomLeftRadius = 10;
        topBar.style.borderBottomRightRadius = 10;
        topBar.style.paddingLeft = 10;
        topBar.style.paddingRight = 10;
        topBar.style.paddingTop = 7;
        topBar.style.paddingBottom = 7;
        topBar.style.marginBottom = 8;

        Label ballLbl = new Label($"🔴 Bóng thu phục: {BallCount}");
        ballLbl.style.color = Color.white;
        ballLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        ballLbl.style.fontSize = 12;

        Button buyBallBtn = new Button(BuyBallButton) { text = "+ Mua bóng ($25)" };
        buyBallBtn.style.backgroundColor = new Color(0.96f, 0.56f, 0.22f, 1f);
        buyBallBtn.style.color = Color.white;
        buyBallBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        buyBallBtn.style.fontSize = 11;
        buyBallBtn.style.borderTopLeftRadius = 6;
        buyBallBtn.style.borderTopRightRadius = 6;
        buyBallBtn.style.borderBottomLeftRadius = 6;
        buyBallBtn.style.borderBottomRightRadius = 6;
        buyBallBtn.style.paddingLeft = 8;
        buyBallBtn.style.paddingRight = 8;
        buyBallBtn.style.paddingTop = 4;
        buyBallBtn.style.paddingBottom = 4;

        topBar.Add(ballLbl);
        topBar.Add(buyBallBtn);
        content.Add(topBar);

        if (!string.IsNullOrEmpty(resultBannerText))
        {
            VisualElement banner = new VisualElement();
            banner.style.backgroundColor = resultPositive
                ? new Color(0.16f, 0.58f, 0.34f, 0.95f)
                : new Color(0.68f, 0.22f, 0.22f, 0.95f);
            banner.style.borderTopLeftRadius = 8;
            banner.style.borderTopRightRadius = 8;
            banner.style.borderBottomLeftRadius = 8;
            banner.style.borderBottomRightRadius = 8;
            banner.style.paddingLeft = 10;
            banner.style.paddingRight = 10;
            banner.style.paddingTop = 8;
            banner.style.paddingBottom = 8;
            banner.style.marginBottom = 8;

            Label bannerLbl = new Label(resultBannerText);
            bannerLbl.style.color = Color.white;
            bannerLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            bannerLbl.style.fontSize = 12;
            bannerLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            bannerLbl.style.whiteSpace = WhiteSpace.Normal;
            banner.Add(bannerLbl);
            content.Add(banner);
        }

        if (currentTargetPet == null)
        {
            VisualElement notFound = new VisualElement();
            notFound.style.flexGrow = 1;
            notFound.style.alignItems = Align.Center;
            notFound.style.justifyContent = Justify.Center;

            Label nfText = new Label("Không phát hiện thú cưng nào ở gần bạn.");
            nfText.style.color = new Color(0.75f, 0.8f, 0.88f, 1f);
            nfText.style.unityTextAlign = TextAnchor.MiddleCenter;
            notFound.Add(nfText);
            content.Add(notFound);
            return;
        }

        bool isDog = currentTargetPet.PetName.IndexOf("Chó", System.StringComparison.OrdinalIgnoreCase) >= 0
                  || currentTargetPet.name.IndexOf("Dog", System.StringComparison.OrdinalIgnoreCase) >= 0;

        VisualElement petCard = new VisualElement();
        petCard.style.backgroundColor = new Color(0.15f, 0.17f, 0.23f, 0.95f);
        petCard.style.borderTopLeftRadius = 12;
        petCard.style.borderTopRightRadius = 12;
        petCard.style.borderBottomLeftRadius = 12;
        petCard.style.borderBottomRightRadius = 12;
        petCard.style.paddingLeft = 12;
        petCard.style.paddingRight = 12;
        petCard.style.paddingTop = 10;
        petCard.style.paddingBottom = 10;
        petCard.style.marginBottom = 8;

        VisualElement headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.alignItems = Align.Center;
        headerRow.style.justifyContent = Justify.SpaceBetween;
        headerRow.style.marginBottom = 8;

        Label petTitle = new Label($"{(isDog ? "🐶" : "🐱")} {currentTargetPet.PetName}");
        petTitle.style.color = Color.white;
        petTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        petTitle.style.fontSize = 15;

        Label stars = new Label("★★★");
        stars.style.color = new Color(0.98f, 0.82f, 0.24f, 1f);
        stars.style.fontSize = 14;

        headerRow.Add(petTitle);
        headerRow.Add(stars);
        petCard.Add(headerRow);

        float hp = Mathf.Clamp(currentTargetPet.Happiness, 0f, 100f);
        petCard.Add(BuildStatBar("Độ vui vẻ (HP)", $"{hp:0}/100", hp / 100f, new Color(0.24f, 0.86f, 0.48f, 1f)));
        petCard.Add(BuildStatBar("Độ no (Armor)", $"{100f - currentTargetPet.Hunger:0}/100", Mathf.Clamp01((100f - currentTargetPet.Hunger) / 100f), new Color(0.32f, 0.68f, 0.96f, 1f)));

        VisualElement bonusRow = new VisualElement();
        bonusRow.style.flexDirection = FlexDirection.Row;
        bonusRow.style.justifyContent = Justify.SpaceBetween;
        bonusRow.style.marginTop = 6;

        Label happyBonus = new Label("Bonus Mood: +4%");
        happyBonus.style.color = new Color(0.95f, 0.78f, 0.35f, 1f);
        happyBonus.style.fontSize = 11;
        happyBonus.style.unityFontStyleAndWeight = FontStyle.Bold;

        int vBonus = GameWorld.HasInstance ? GameWorld.Instance.Pets.ViewerBonus() : 5;
        Label viewerBonus = new Label($"Bonus Viewer: +{Mathf.Max(5, vBonus)}%");
        viewerBonus.style.color = new Color(0.45f, 0.92f, 0.62f, 1f);
        viewerBonus.style.fontSize = 11;
        viewerBonus.style.unityFontStyleAndWeight = FontStyle.Bold;

        bonusRow.Add(happyBonus);
        bonusRow.Add(viewerBonus);
        petCard.Add(bonusRow);

        content.Add(petCard);

        // Nút Ném bóng thu phục (CatchButton) và nút Cho ăn nhanh
        Button catchBtn = new Button(CatchButton)
        {
            text = catchProcessStarted ? "🔴 Đang lắc bóng thu phục..." : "🔴 NÉM BÓNG THU PHỤC / HUẤN LUYỆN"
        };
        catchBtn.SetEnabled(!catchProcessStarted);
        catchBtn.style.backgroundColor = new Color(0.92f, 0.28f, 0.28f, 1f);
        catchBtn.style.color = Color.white;
        catchBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        catchBtn.style.fontSize = 12;
        catchBtn.style.borderTopLeftRadius = 16;
        catchBtn.style.borderTopRightRadius = 16;
        catchBtn.style.borderBottomLeftRadius = 16;
        catchBtn.style.borderBottomRightRadius = 16;
        catchBtn.style.paddingTop = 9;
        catchBtn.style.paddingBottom = 9;
        catchBtn.style.marginBottom = 6;
        content.Add(catchBtn);

        Button feedBtn = new Button(() =>
        {
            if (GameWorld.HasInstance)
            {
                for (int i = 0; i < GameWorld.Instance.Pets.Pets.Count; i++)
                    GameWorld.Instance.Pets.Pets[i]?.Feed();
            }
            else if (currentTargetPet != null)
            {
                currentTargetPet.Feed();
            }
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã cho thú cưng ăn và chăm sóc qua App! (+Bonus Viewer)", true);
            RefreshView();
        })
        {
            text = "🍖 Cho ăn & Vuốt ve từ xa"
        };
        feedBtn.style.backgroundColor = new Color(0.22f, 0.68f, 0.42f, 1f);
        feedBtn.style.color = Color.white;
        feedBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        feedBtn.style.borderTopLeftRadius = 10;
        feedBtn.style.borderTopRightRadius = 10;
        feedBtn.style.borderBottomLeftRadius = 10;
        feedBtn.style.borderBottomRightRadius = 10;
        feedBtn.style.paddingTop = 7;
        feedBtn.style.paddingBottom = 7;
        content.Add(feedBtn);
    }

    VisualElement BuildStatBar(string label, string valueText, float fill01, Color barColor)
    {
        VisualElement wrap = new VisualElement();
        wrap.style.marginBottom = 6;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 2;

        Label l = new Label(label);
        l.style.color = new Color(0.78f, 0.84f, 0.92f, 1f);
        l.style.fontSize = 11;

        Label r = new Label(valueText);
        r.style.color = Color.white;
        r.style.fontSize = 11;
        r.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(l);
        row.Add(r);

        VisualElement bg = new VisualElement();
        bg.style.height = 8;
        bg.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
        bg.style.borderTopLeftRadius = 4;
        bg.style.borderTopRightRadius = 4;
        bg.style.borderBottomLeftRadius = 4;
        bg.style.borderBottomRightRadius = 4;
        bg.style.overflow = Overflow.Hidden;

        VisualElement fill = new VisualElement();
        fill.style.width = Length.Percent(Mathf.Clamp01(fill01) * 100f);
        fill.style.height = Length.Percent(100f);
        fill.style.backgroundColor = barColor;
        bg.Add(fill);

        wrap.Add(row);
        wrap.Add(bg);
        return wrap;
    }
}
