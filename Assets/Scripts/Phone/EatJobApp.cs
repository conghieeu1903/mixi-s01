using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng nhận đơn chạy Shipper giao đồ ăn kiếm thêm thu nhập theo cơ chế EatApps của SLS2
public class EatJobApp : PhoneApp
{
    enum JobState
    {
        Idle,
        Searching,
        Found,
        Delivering
    }

    JobState state = JobState.Idle;
    int currentDistance;
    int currentPrice;
    string currentCustomerOrder;
    float deliveryProgress01;
    public int TotalEarnedMoneyEat { get; private set; }
    public int CompletedDeliveries { get; private set; }

    VisualElement currentContent;
    Coroutine jobRoutine;

    static readonly string[] SampleOrders =
    {
        "2x Burger Bò Phô Mai + 1x Cola",
        "1x Pizza Hải Sản Cỡ Lớn",
        "3x Gà Rán Giòn Cay + Khoai Tây",
        "2x Phở Bò Đặc Biệt + Quẩy",
        "2x Trà Sữa Trân Châu Đường Đen",
        "1x Combo Cơm Tấm Sườn Bì Chả"
    };

    public override string IconGlyph => "🛵";
    public override Color AccentColor => new Color(0.18f, 0.76f, 0.44f, 1f);
    public override bool BlockWhileDriving => false;

    void Awake()
    {
        SetDefaultAppName("Shipper");
    }

    // Bắt đầu bật chế độ dò tìm đơn giao hàng quanh khu vực
    public void GoButtonClicked()
    {
        if (state != JobState.Idle) return;
        AudioManager.PlayUi("phone_tap");
        if (jobRoutine != null)
            StopCoroutine(jobRoutine);
        jobRoutine = StartCoroutine(SearchingProcess());
    }

    // Quá trình quét tìm đơn hàng mới giống SearchingProcess của SLS2
    IEnumerator SearchingProcess()
    {
        state = JobState.Searching;
        RefreshView();

        float waitTime = Random.Range(2.2f, 3.8f);
        yield return new WaitForSecondsRealtime(waitTime);

        currentDistance = Random.Range(250, 401);
        currentPrice = Random.Range(25, 61);
        currentCustomerOrder = SampleOrders[Random.Range(0, SampleOrders.Length)];
        state = JobState.Found;
        AudioManager.Play("delivery_founded");
        RefreshView();
        jobRoutine = null;
    }

    // Chấp nhận đơn giao hàng vừa tìm thấy
    public void AcceptButton()
    {
        if (state != JobState.Found) return;
        AudioManager.PlayUi("phone_tap");
        deliveryProgress01 = 0f;
        state = JobState.Delivering;
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã nhận đơn Shipper ({currentDistance}m) — Thưởng ${currentPrice}!", true);
        if (jobRoutine != null)
            StopCoroutine(jobRoutine);
        jobRoutine = StartCoroutine(DeliveryProcessRoutine());
    }

    // Từ chối hoặc hủy đơn giao hàng hiện tại
    public void CancelButton()
    {
        AudioManager.PlayUi("phone_tap");
        if (jobRoutine != null)
        {
            StopCoroutine(jobRoutine);
            jobRoutine = null;
        }
        state = JobState.Idle;
        RefreshView();
    }

    // Tiến trình đang đi giao đơn hàng cho khách
    IEnumerator DeliveryProcessRoutine()
    {
        RefreshView();
        const float duration = 4.5f;
        float elapsed = 0f;
        while (elapsed < duration && state == JobState.Delivering)
        {
            elapsed += Time.unscaledDeltaTime;
            deliveryProgress01 = Mathf.Clamp01(elapsed / duration);
            RefreshView();
            yield return null;
        }

        if (state == JobState.Delivering)
            DeliveryCompleted();
    }

    // Hoàn thành chuyến giao hàng và cộng tiền thưởng vào ví
    public void DeliveryCompleted()
    {
        if (jobRoutine != null)
        {
            StopCoroutine(jobRoutine);
            jobRoutine = null;
        }

        state = JobState.Idle;
        deliveryProgress01 = 0f;
        TotalEarnedMoneyEat += currentPrice;
        CompletedDeliveries++;

        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Economy.Earn(currentPrice);
            GameWorld.Instance.Mood.Change(2f);
        }

        AudioManager.Play("cash_beep");
        AudioManager.Play("purchase");
        FindFirstObjectByType<HudView>()?.ShowInfo($"Giao hàng hoàn tất! +${currentPrice} (Tổng kiếm được: ${TotalEarnedMoneyEat})", true);
        RefreshView();
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

        // Thẻ thống kê thu nhập nghề Shipper
        VisualElement statsBox = new VisualElement();
        statsBox.style.backgroundColor = new Color(0.14f, 0.22f, 0.18f, 0.95f);
        statsBox.style.borderTopLeftRadius = 10;
        statsBox.style.borderTopRightRadius = 10;
        statsBox.style.borderBottomLeftRadius = 10;
        statsBox.style.borderBottomRightRadius = 10;
        statsBox.style.paddingLeft = 12;
        statsBox.style.paddingRight = 12;
        statsBox.style.paddingTop = 8;
        statsBox.style.paddingBottom = 8;
        statsBox.style.marginBottom = 10;

        Label statsTitle = new Label($"Tổng thu nhập Shipper: ${TotalEarnedMoneyEat}");
        statsTitle.style.color = new Color(0.35f, 0.95f, 0.55f, 1f);
        statsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsTitle.style.fontSize = 13;

        Label statsSub = new Label($"Số chuyến hoàn thành: {CompletedDeliveries} đơn");
        statsSub.style.color = new Color(0.75f, 0.85f, 0.78f, 1f);
        statsSub.style.fontSize = 11;

        statsBox.Add(statsTitle);
        statsBox.Add(statsSub);
        content.Add(statsBox);

        VisualElement body = new VisualElement();
        body.style.flexGrow = 1;
        body.style.justifyContent = Justify.Center;
        body.style.alignItems = Align.Center;
        body.style.backgroundColor = new Color(0.15f, 0.17f, 0.22f, 0.95f);
        body.style.borderTopLeftRadius = 12;
        body.style.borderTopRightRadius = 12;
        body.style.borderBottomLeftRadius = 12;
        body.style.borderBottomRightRadius = 12;
        body.style.paddingLeft = 14;
        body.style.paddingRight = 14;
        body.style.paddingTop = 16;
        body.style.paddingBottom = 16;

        if (state == JobState.Idle)
        {
            Label icon = new Label("🛵");
            icon.style.fontSize = 40;
            icon.style.marginBottom = 8;

            Label status = new Label("Sẵn sàng nhận đơn giao đồ ăn");
            status.style.color = Color.white;
            status.style.unityFontStyleAndWeight = FontStyle.Bold;
            status.style.fontSize = 14;
            status.style.marginBottom = 6;

            Label desc = new Label("Bật trực tuyến để tìm khách đặt đồ ăn quanh thành phố và nhận tiền thưởng mỗi chuyến ($25 - $60).");
            desc.style.color = new Color(0.75f, 0.8f, 0.88f, 1f);
            desc.style.fontSize = 11;
            desc.style.unityTextAlign = TextAnchor.MiddleCenter;
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 16;

            Button goBtn = new Button(GoButtonClicked) { text = "GO — BẮT ĐẦU TÌM ĐƠN" };
            goBtn.style.backgroundColor = new Color(0.18f, 0.76f, 0.44f, 1f);
            goBtn.style.color = Color.white;
            goBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            goBtn.style.fontSize = 14;
            goBtn.style.borderTopLeftRadius = 20;
            goBtn.style.borderTopRightRadius = 20;
            goBtn.style.borderBottomLeftRadius = 20;
            goBtn.style.borderBottomRightRadius = 20;
            goBtn.style.paddingLeft = 22;
            goBtn.style.paddingRight = 22;
            goBtn.style.paddingTop = 10;
            goBtn.style.paddingBottom = 10;

            body.Add(icon);
            body.Add(status);
            body.Add(desc);
            body.Add(goBtn);
        }
        else if (state == JobState.Searching)
        {
            Label radar = new Label("📡");
            radar.style.fontSize = 38;
            radar.style.marginBottom = 10;

            Label searchingTxt = new Label("Đang tìm đơn hàng quanh bạn...");
            searchingTxt.style.color = new Color(0.95f, 0.82f, 0.32f, 1f);
            searchingTxt.style.unityFontStyleAndWeight = FontStyle.Bold;
            searchingTxt.style.fontSize = 14;
            searchingTxt.style.marginBottom = 14;

            Button cancelBtn = new Button(CancelButton) { text = "Dừng tìm kiếm" };
            cancelBtn.style.backgroundColor = new Color(0.45f, 0.48f, 0.55f, 1f);
            cancelBtn.style.color = Color.white;
            cancelBtn.style.borderTopLeftRadius = 8;
            cancelBtn.style.borderTopRightRadius = 8;
            cancelBtn.style.borderBottomLeftRadius = 8;
            cancelBtn.style.borderBottomRightRadius = 8;
            cancelBtn.style.paddingLeft = 14;
            cancelBtn.style.paddingRight = 14;
            cancelBtn.style.paddingTop = 6;
            cancelBtn.style.paddingBottom = 6;

            body.Add(radar);
            body.Add(searchingTxt);
            body.Add(cancelBtn);
        }
        else if (state == JobState.Found)
        {
            Label badge = new Label("🔔 TÌM THẤY ĐƠN HÀNG MỚI!");
            badge.style.color = new Color(0.98f, 0.82f, 0.25f, 1f);
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.fontSize = 14;
            badge.style.marginBottom = 8;

            Label orderLbl = new Label(currentCustomerOrder);
            orderLbl.style.color = Color.white;
            orderLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            orderLbl.style.fontSize = 13;
            orderLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            orderLbl.style.whiteSpace = WhiteSpace.Normal;
            orderLbl.style.marginBottom = 8;

            Label distLbl = new Label($"Khoảng cách: {currentDistance}M");
            distLbl.style.color = new Color(0.75f, 0.84f, 0.95f, 1f);
            distLbl.style.fontSize = 12;
            distLbl.style.marginBottom = 4;

            Label priceLbl = new Label($"Tiền công: ${currentPrice}");
            priceLbl.style.color = new Color(0.35f, 0.95f, 0.55f, 1f);
            priceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            priceLbl.style.fontSize = 18;
            priceLbl.style.marginBottom = 14;

            VisualElement btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.Center;

            Button acceptBtn = new Button(AcceptButton) { text = "✓ Nhận đơn" };
            acceptBtn.style.backgroundColor = new Color(0.18f, 0.76f, 0.44f, 1f);
            acceptBtn.style.color = Color.white;
            acceptBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            acceptBtn.style.borderTopLeftRadius = 8;
            acceptBtn.style.borderTopRightRadius = 8;
            acceptBtn.style.borderBottomLeftRadius = 8;
            acceptBtn.style.borderBottomRightRadius = 8;
            acceptBtn.style.paddingLeft = 14;
            acceptBtn.style.paddingRight = 14;
            acceptBtn.style.paddingTop = 8;
            acceptBtn.style.paddingBottom = 8;
            acceptBtn.style.marginRight = 8;

            Button rejectBtn = new Button(CancelButton) { text = "✕ Từ chối" };
            rejectBtn.style.backgroundColor = new Color(0.82f, 0.25f, 0.25f, 1f);
            rejectBtn.style.color = Color.white;
            rejectBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            rejectBtn.style.borderTopLeftRadius = 8;
            rejectBtn.style.borderTopRightRadius = 8;
            rejectBtn.style.borderBottomLeftRadius = 8;
            rejectBtn.style.borderBottomRightRadius = 8;
            rejectBtn.style.paddingLeft = 14;
            rejectBtn.style.paddingRight = 14;
            rejectBtn.style.paddingTop = 8;
            rejectBtn.style.paddingBottom = 8;

            btnRow.Add(acceptBtn);
            btnRow.Add(rejectBtn);

            body.Add(badge);
            body.Add(orderLbl);
            body.Add(distLbl);
            body.Add(priceLbl);
            body.Add(btnRow);
        }
        else if (state == JobState.Delivering)
        {
            int remainMeters = Mathf.Max(0, Mathf.RoundToInt(currentDistance * (1f - deliveryProgress01)));

            Label title = new Label("🛵 ĐANG GIAO ĐƠN HÀNG...");
            title.style.color = new Color(0.35f, 0.88f, 0.98f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.marginBottom = 6;

            Label orderLbl = new Label($"{currentCustomerOrder} (${currentPrice})");
            orderLbl.style.color = Color.white;
            orderLbl.style.fontSize = 12;
            orderLbl.style.marginBottom = 8;

            Label remLbl = new Label($"Còn cách điểm giao: {remainMeters}M ({Mathf.RoundToInt(deliveryProgress01 * 100f)}%)");
            remLbl.style.color = new Color(0.82f, 0.88f, 0.95f, 1f);
            remLbl.style.fontSize = 12;
            remLbl.style.marginBottom = 8;

            VisualElement barBg = new VisualElement();
            barBg.style.width = 220;
            barBg.style.height = 10;
            barBg.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
            barBg.style.borderTopLeftRadius = 5;
            barBg.style.borderTopRightRadius = 5;
            barBg.style.borderBottomLeftRadius = 5;
            barBg.style.borderBottomRightRadius = 5;
            barBg.style.overflow = Overflow.Hidden;
            barBg.style.marginBottom = 14;

            VisualElement barFill = new VisualElement();
            barFill.style.width = Length.Percent(deliveryProgress01 * 100f);
            barFill.style.height = Length.Percent(100f);
            barFill.style.backgroundColor = new Color(0.18f, 0.76f, 0.44f, 1f);
            barBg.Add(barFill);

            Button finishNowBtn = new Button(DeliveryCompleted) { text = "Giao tận tay khách ngay" };
            finishNowBtn.style.backgroundColor = new Color(0.18f, 0.76f, 0.44f, 1f);
            finishNowBtn.style.color = Color.white;
            finishNowBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            finishNowBtn.style.borderTopLeftRadius = 8;
            finishNowBtn.style.borderTopRightRadius = 8;
            finishNowBtn.style.borderBottomLeftRadius = 8;
            finishNowBtn.style.borderBottomRightRadius = 8;
            finishNowBtn.style.paddingLeft = 14;
            finishNowBtn.style.paddingRight = 14;
            finishNowBtn.style.paddingTop = 7;
            finishNowBtn.style.paddingBottom = 7;
            finishNowBtn.style.marginBottom = 6;

            Button cancelBtn = new Button(CancelButton) { text = "Hủy chuyến" };
            cancelBtn.style.backgroundColor = new Color(0.45f, 0.25f, 0.25f, 1f);
            cancelBtn.style.color = Color.white;
            cancelBtn.style.borderTopLeftRadius = 6;
            cancelBtn.style.borderTopRightRadius = 6;
            cancelBtn.style.borderBottomLeftRadius = 6;
            cancelBtn.style.borderBottomRightRadius = 6;
            cancelBtn.style.paddingLeft = 10;
            cancelBtn.style.paddingRight = 10;
            cancelBtn.style.paddingTop = 4;
            cancelBtn.style.paddingBottom = 4;

            body.Add(title);
            body.Add(orderLbl);
            body.Add(remLbl);
            body.Add(barBg);
            body.Add(finishNowBtn);
            body.Add(cancelBtn);
        }

        content.Add(body);
    }
}
