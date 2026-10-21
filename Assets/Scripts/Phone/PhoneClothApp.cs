using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng Tủ đồ & Trang phục theo cơ chế ClothApps của SLS2: phối đồ nhanh trên điện thoại hoặc ghé Tiệm Thời Trang
public class PhoneClothApp : PhoneApp
{
    static readonly string[] HairNames = { "Undercut", "Layer Hàn Quốc", "Slick Back", "Mullet", "Buzz Cut" };
    static readonly string[] TopNames = { "Áo Thun Streamer", "Hoodie Streetwear", "Áo Khoác Bomber", "Áo Sơ Mi Lịch Lãm", "Áo Đấu Esport" };
    static readonly string[] BottomNames = { "Quần Jeans Đen", "Quần Jogger Năng Động", "Quần Short Thoải Mái", "Quần Tây Âu", "Quần Cargo Túi Hộp" };
    static readonly string[] ShoeNames = { "Sneaker Trắng", "Giày Chạy Bộ", "Boots Cổ Cao", "Dép Tổ Ong Huyền Thoại" };

    VisualElement currentContent;

    public override string IconGlyph => "👕";
    public override Color AccentColor => new Color(0.92f, 0.34f, 0.68f, 1f);
    public override bool BlockWhileDriving => true;

    void Awake()
    {
        SetDefaultAppName("Quần áo");
    }

    AppearanceData GetCurrentAppearance()
    {
        AppearanceSystem sys = FindFirstObjectByType<AppearanceSystem>();
        if (sys != null && sys.Current != null)
            return sys.Current;
        return new AppearanceData();
    }

    void ApplyAppearance(AppearanceData data)
    {
        AppearanceSystem sys = FindFirstObjectByType<AppearanceSystem>();
        if (sys != null)
            sys.Apply(data);
        AudioManager.PlayUi("phone_tap");
        if (currentContent != null)
            BuildAppContent(currentContent);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        content.Clear();

        AppearanceData data = GetCurrentAppearance();

        Label sub = new Label("Tùy chỉnh ngoại hình & Trang phục Streamer");
        sub.style.color = new Color(0.85f, 0.9f, 0.96f, 1f);
        sub.style.unityFontStyleAndWeight = FontStyle.Bold;
        sub.style.fontSize = 12;
        sub.style.marginBottom = 8;
        content.Add(sub);

        content.Add(BuildPartSelector("Kiểu tóc", HairNames[Mathf.Clamp(data.hairStyle, 0, HairNames.Length - 1)],
            () => { data.hairStyle = (data.hairStyle - 1 + HairNames.Length) % HairNames.Length; ApplyAppearance(data); },
            () => { data.hairStyle = (data.hairStyle + 1) % HairNames.Length; ApplyAppearance(data); }));

        content.Add(BuildPartSelector("Áo (Top)", TopNames[Mathf.Clamp(data.top, 0, TopNames.Length - 1)],
            () => { data.top = (data.top - 1 + TopNames.Length) % TopNames.Length; ApplyAppearance(data); },
            () => { data.top = (data.top + 1) % TopNames.Length; ApplyAppearance(data); }));

        content.Add(BuildPartSelector("Quần (Bottom)", BottomNames[Mathf.Clamp(data.bottom, 0, BottomNames.Length - 1)],
            () => { data.bottom = (data.bottom - 1 + BottomNames.Length) % BottomNames.Length; ApplyAppearance(data); },
            () => { data.bottom = (data.bottom + 1) % BottomNames.Length; ApplyAppearance(data); }));

        content.Add(BuildPartSelector("Giày (Shoes)", ShoeNames[Mathf.Clamp(data.shoes, 0, ShoeNames.Length - 1)],
            () => { data.shoes = (data.shoes - 1 + ShoeNames.Length) % ShoeNames.Length; ApplyAppearance(data); },
            () => { data.shoes = (data.shoes + 1) % ShoeNames.Length; ApplyAppearance(data); }));

        Button buyOutfitBtn = new Button(() =>
        {
            if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(35f))
            {
                AudioManager.PlayUi("purchase_fail");
                Phone?.ShowPhoneWarning("Không đủ $35 để đặt mua bộ sưu tập thời trang mới!");
                return;
            }

            AudioManager.Play("cash_beep");
            AudioManager.Play("purchase");
            data.top = (data.top + 1) % TopNames.Length;
            data.bottom = (data.bottom + 1) % BottomNames.Length;
            data.shoes = (data.shoes + 1) % ShoeNames.Length;
            ApplyAppearance(data);
            GameWorld.Instance.Mood.Change(5f);
            GameWorld.Instance.Stream.AddFollowers(6);
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã diện bộ trang phục mới qua ClothApp! (+5 Mood, +6 Follower)", true);
        })
        {
            text = "🛍 Mua bộ trang phục mới ($35 · +5 Mood · +6 Follower)"
        };
        buyOutfitBtn.style.backgroundColor = new Color(0.92f, 0.34f, 0.68f, 1f);
        buyOutfitBtn.style.color = Color.white;
        buyOutfitBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        buyOutfitBtn.style.fontSize = 11;
        buyOutfitBtn.style.borderTopLeftRadius = 10;
        buyOutfitBtn.style.borderTopRightRadius = 10;
        buyOutfitBtn.style.borderBottomLeftRadius = 10;
        buyOutfitBtn.style.borderBottomRightRadius = 10;
        buyOutfitBtn.style.paddingTop = 8;
        buyOutfitBtn.style.paddingBottom = 8;
        buyOutfitBtn.style.marginTop = 6;
        buyOutfitBtn.style.marginBottom = 6;
        content.Add(buyOutfitBtn);

        Button goStoreBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            FindFirstObjectByType<MapSystem>()?.TeleportToLabel("Tiệm Thời Trang & Cắt Tóc");
            Phone?.Close();
        })
        {
            text = "✂ Đi tới Tiệm Thời Trang & Cắt Tóc"
        };
        goStoreBtn.style.backgroundColor = new Color(0.22f, 0.26f, 0.34f, 1f);
        goStoreBtn.style.color = Color.white;
        goStoreBtn.style.borderTopLeftRadius = 8;
        goStoreBtn.style.borderTopRightRadius = 8;
        goStoreBtn.style.borderBottomLeftRadius = 8;
        goStoreBtn.style.borderBottomRightRadius = 8;
        goStoreBtn.style.paddingTop = 6;
        goStoreBtn.style.paddingBottom = 6;
        content.Add(goStoreBtn);
    }

    VisualElement BuildPartSelector(string category, string valueName, System.Action onPrev, System.Action onNext)
    {
        VisualElement box = new VisualElement();
        box.style.backgroundColor = new Color(0.15f, 0.17f, 0.23f, 0.95f);
        box.style.borderTopLeftRadius = 8;
        box.style.borderTopRightRadius = 8;
        box.style.borderBottomLeftRadius = 8;
        box.style.borderBottomRightRadius = 8;
        box.style.paddingLeft = 10;
        box.style.paddingRight = 10;
        box.style.paddingTop = 6;
        box.style.paddingBottom = 6;
        box.style.marginBottom = 6;

        Label catLbl = new Label(category);
        catLbl.style.color = new Color(0.72f, 0.78f, 0.88f, 1f);
        catLbl.style.fontSize = 10;
        catLbl.style.marginBottom = 3;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;

        Button prevBtn = new Button(onPrev) { text = "◀" };
        StyleSmallArrow(prevBtn);

        Label valLbl = new Label(valueName);
        valLbl.style.color = Color.white;
        valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        valLbl.style.fontSize = 12;

        Button nextBtn = new Button(onNext) { text = "▶" };
        StyleSmallArrow(nextBtn);

        row.Add(prevBtn);
        row.Add(valLbl);
        row.Add(nextBtn);

        box.Add(catLbl);
        box.Add(row);
        return box;
    }

    void StyleSmallArrow(Button btn)
    {
        btn.style.backgroundColor = new Color(0.26f, 0.3f, 0.38f, 1f);
        btn.style.color = Color.white;
        btn.style.borderTopLeftRadius = 6;
        btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6;
        btn.style.borderBottomRightRadius = 6;
        btn.style.paddingLeft = 8;
        btn.style.paddingRight = 8;
        btn.style.paddingTop = 2;
        btn.style.paddingBottom = 2;
    }
}
