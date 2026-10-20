using System;
using UnityEngine;
using UnityEngine.UIElements;

// Thẻ xem quảng cáo nhận thưởng theo Figma: khung tím, tiêu đề, mức thưởng, khung video, cụm xu và nút WATCH NOW
[UxmlElement]
public partial class RewardAdCard : VisualElement
{
    public const string UssClassName = "reward-ad-card"; // Class USS gốc của thẻ quảng cáo
    public const string TitleUssClassName = UssClassName + "__title"; // Class USS của dòng tiêu đề
    public const string RewardUssClassName = UssClassName + "__reward"; // Class USS của dòng phần thưởng
    public const string VideoBoxUssClassName = UssClassName + "__video-box"; // Class USS của khung video
    public const string PlayIconUssClassName = UssClassName + "__play-icon"; // Class USS của tam giác Play
    public const string CoinsUssClassName = UssClassName + "__coins"; // Class USS của cụm đồng xu
    public const string ActionBtnUssClassName = UssClassName + "__action-btn"; // Class USS của nút xem ngay
    public const string ActionLabelUssClassName = UssClassName + "__action-label"; // Class USS của chữ trên nút

    const string UnitFormat = "{0} <size=55>{1}</size>"; // Định dạng thưởng + đơn vị nhỏ hơn

    readonly StrokeText titleLabel; // Nhãn tiêu đề (WATCH AD)
    readonly StrokeText rewardLabel; // Nhãn phần thưởng (GET 100 Xu)
    readonly VisualElement actionButton; // Nút bấm xem quảng cáo
    readonly Label actionLabel; // Chữ trên nút bấm

    string currentReward = "GET 100"; // Nội dung phần thưởng
    string currentUnit = "Xu"; // Đơn vị phần thưởng

    public event Action watchClicked; // Sự kiện khi bấm nút xem quảng cáo

    // Dòng tiêu đề trên cùng
    [UxmlAttribute]
    public string titleText
    {
        get => titleLabel.text;
        set => titleLabel.text = value;
    }

    // Dòng phần thưởng (ví dụ: GET 100)
    [UxmlAttribute]
    public string rewardText
    {
        get => currentReward;
        set
        {
            currentReward = value ?? string.Empty;
            RefreshReward();
        }
    }

    // Đơn vị nhỏ phía sau phần thưởng (ví dụ: Xu)
    [UxmlAttribute]
    public string unitText
    {
        get => currentUnit;
        set
        {
            currentUnit = value ?? string.Empty;
            RefreshReward();
        }
    }

    // Chữ trên nút hành động
    [UxmlAttribute]
    public string buttonText
    {
        get => actionLabel.text;
        set => actionLabel.text = value;
    }

    // Khởi tạo thẻ với đầy đủ thành phần con
    public RewardAdCard()
    {
        AddToClassList(UssClassName);

        titleLabel = new StrokeText("WATCH AD") { pickingMode = PickingMode.Ignore };
        titleLabel.AddToClassList(TitleUssClassName);
        Add(titleLabel);

        rewardLabel = new StrokeText { pickingMode = PickingMode.Ignore, enableRichText = true };
        rewardLabel.AddToClassList(RewardUssClassName);
        Add(rewardLabel);

        var videoBox = new VisualElement { pickingMode = PickingMode.Ignore };
        videoBox.AddToClassList(VideoBoxUssClassName);
        var playIcon = new VisualElement { pickingMode = PickingMode.Ignore };
        playIcon.AddToClassList(PlayIconUssClassName);
        videoBox.Add(playIcon);
        Add(videoBox);

        var coins = new VisualElement { pickingMode = PickingMode.Ignore };
        coins.AddToClassList(CoinsUssClassName);
        Add(coins);

        actionButton = new VisualElement();
        actionButton.AddToClassList(ActionBtnUssClassName);
        actionLabel = new Label("WATCH NOW") { pickingMode = PickingMode.Ignore };
        actionLabel.AddToClassList(ActionLabelUssClassName);
        actionButton.Add(actionLabel);
        actionButton.AddManipulator(new Clickable(() => watchClicked?.Invoke()));
        Add(actionButton);

        RefreshReward();
    }

    // Bật hoặc tắt nút xem quảng cáo và đổi chữ nếu cần
    public void SetAvailable(bool available, string customButtonText = null)
    {
        actionButton.SetEnabled(available);
        if (!string.IsNullOrEmpty(customButtonText)) actionLabel.text = customButtonText;
    }

    // Ghép phần thưởng và đơn vị thành rich text
    void RefreshReward()
    {
        rewardLabel.text = string.IsNullOrEmpty(currentUnit) ? currentReward : string.Format(UnitFormat, currentReward, currentUnit);
    }
}
