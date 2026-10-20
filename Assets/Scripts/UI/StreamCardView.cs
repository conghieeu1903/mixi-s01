using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Hiện các thẻ hành động và minigame tương tác khi stream
public class StreamCardView : UIPanel
{
    VisualElement cardsContainer;
    Label titleLabel;
    bool built;

    void OnEnable()
    {
        built = false;
        EnsureFrame();
    }

    void EnsureFrame()
    {
        if (built || Root == null) return;
        built = true;

        VisualElement host = Root.Q<VisualElement>("worldPanels") ?? Root;
        host.Clear();
        host.AddToClassList("wf-screen");
        host.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        host.style.alignItems = Align.Center;
        host.style.justifyContent = Justify.Center;

        VisualElement box = new VisualElement();
        box.AddToClassList("wf-panel");
        box.style.width = 620;
        box.style.paddingLeft = 20;
        box.style.paddingRight = 20;
        box.style.paddingTop = 16;
        box.style.paddingBottom = 16;

        VisualElement header = new VisualElement();
        header.AddToClassList("wf-panel__header");

        titleLabel = new Label("Chọn thẻ hành động trên sóng");
        titleLabel.AddToClassList("wf-subheading");
        titleLabel.AddToClassList("wf-grow");

        Button closeBtn = new Button(CloseSelf) { text = "×" };
        closeBtn.AddToClassList("wf-button");
        closeBtn.AddToClassList("wf-button--square");

        header.Add(titleLabel);
        header.Add(closeBtn);
        box.Add(header);

        cardsContainer = new VisualElement();
        cardsContainer.style.marginTop = 10;
        box.Add(cardsContainer);

        host.Add(box);
    }

    // Hiện danh sách thẻ
    public void ShowCards(List<StreamCardData> cards)
    {
        EnsureFrame();
        if (cardsContainer == null) return;
        if (titleLabel != null)
            titleLabel.text = "Chọn thẻ hành động trên sóng";
        cardsContainer.Clear();

        if (cards != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                StreamCardData card = cards[i];
                if (card == null) continue;

                VisualElement row = new VisualElement();
                row.AddToClassList("world-panel__row");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.paddingTop = 8;
                row.style.paddingBottom = 8;

                Label info = new Label($"{card.title} ({Mathf.RoundToInt(card.successChance * 100f)}% thành công: +{card.viewerPercentOnSuccess}% viewer / thất bại {card.viewerPercentOnFail}%)");
                info.AddToClassList("wf-text");
                info.style.flexGrow = 1;

                Button playBtn = new Button(() => PlayCard(card)) { text = "Chọn" };
                playBtn.AddToClassList("wf-button");
                playBtn.AddToClassList("wf-button--primary");
                playBtn.AddToClassList("wf-button--small");

                row.Add(info);
                row.Add(playBtn);
                cardsContainer.Add(row);
            }
        }

        OpenSelf();
    }

    // Hiện giao diện minigame tương ứng với chủ đề đang stream
    public void ShowMinigame(StreamMinigame minigame)
    {
        if (minigame == null) return;
        EnsureFrame();
        if (cardsContainer == null) return;
        cardsContainer.Clear();

        minigame.Begin();

        if (minigame is JustChattingGame jc && jc.CurrentQuestion != null)
        {
            if (titleLabel != null) titleLabel.text = "Minigame Just Chatting — Trả lời khán giả";
            Label qLabel = new Label(jc.CurrentQuestion.question);
            qLabel.AddToClassList("wf-subheading");
            qLabel.style.marginBottom = 10;
            cardsContainer.Add(qLabel);

            for (int i = 0; i < jc.CurrentQuestion.answers.Length; i++)
            {
                int idx = i;
                Button ansBtn = new Button(() =>
                {
                    jc.Answer(idx);
                    FindFirstObjectByType<HudView>()?.ShowInfo("Đã trả lời câu hỏi khán giả!", true);
                    CloseSelf();
                })
                {
                    text = jc.CurrentQuestion.answers[i]
                };
                ansBtn.AddToClassList("wf-button");
                ansBtn.style.marginBottom = 6;
                cardsContainer.Add(ansBtn);
            }
        }
        else if (minigame is AsmrGame asmr)
        {
            if (titleLabel != null) titleLabel.text = "Minigame ASMR — Cân chỉnh tần số âm thanh";
            for (int i = 0; i < asmr.Targets.Count; i++)
            {
                int idx = i;
                float target = asmr.Targets[i];
                Slider slider = new Slider($"Tần số #{i + 1} (Mục tiêu {target:0})", 0f, 100f)
                {
                    value = asmr.Values[i]
                };
                slider.RegisterValueChangedCallback(evt => asmr.SetSlider(idx, evt.newValue));
                cardsContainer.Add(slider);
            }

            Button matchBtn = new Button(() =>
            {
                for (int i = 0; i < asmr.Targets.Count; i++)
                    asmr.SetSlider(i, asmr.Targets[i]);
                asmr.SubmitSliders();
                FindFirstObjectByType<HudView>()?.ShowInfo("Cân chỉnh ASMR chuẩn xác! (+Viewer)", true);
                CloseSelf();
            })
            {
                text = "Khớp âm thanh hoàn tất"
            };
            matchBtn.AddToClassList("wf-button");
            matchBtn.AddToClassList("wf-button--primary");
            matchBtn.style.marginTop = 10;
            cardsContainer.Add(matchBtn);
        }
        else if (minigame is DanceGame dance)
        {
            if (titleLabel != null) titleLabel.text = "Minigame Dance — Bấm nhịp vũ đạo";
            Label scoreLabel = new Label($"Điểm: {dance.Score} · Combo x{dance.Combo}");
            scoreLabel.AddToClassList("wf-subheading");
            scoreLabel.style.marginBottom = 10;
            cardsContainer.Add(scoreLabel);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 10;
            string[] keys = { "← Trái", "↓ Xuống", "↑ Lên", "→ Phải" };
            for (int i = 0; i < keys.Length; i++)
            {
                Button keyBtn = new Button(() =>
                {
                    dance.HitNote(true);
                    scoreLabel.text = $"Điểm: {dance.Score} · Combo x{dance.Combo}";
                })
                {
                    text = keys[i]
                };
                keyBtn.AddToClassList("wf-button");
                keyBtn.style.marginRight = 8;
                row.Add(keyBtn);
            }
            cardsContainer.Add(row);

            Button doneBtn = new Button(() =>
            {
                if (dance.Combo == 0) dance.HitNote(true);
                dance.CompleteDance();
                FindFirstObjectByType<HudView>()?.ShowInfo("Hoàn thành bài nhảy sôi động! (+Viewer)", true);
                CloseSelf();
            })
            {
                text = "Kết thúc màn nhảy"
            };
            doneBtn.AddToClassList("wf-button");
            doneBtn.AddToClassList("wf-button--primary");
            cardsContainer.Add(doneBtn);
        }

        OpenSelf();
    }

    void OpenSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    void PlayCard(StreamCardData card)
    {
        if (card == null || !GameWorld.HasInstance) return;
        bool ok = GameWorld.Instance.Cards.Play(card);
        FindFirstObjectByType<HudView>()?.ShowInfo(
            ok ? $"Thẻ [{card.title}] thành công! (+{card.viewerPercentOnSuccess}% viewer)" : $"Thẻ [{card.title}] thất bại!",
            ok);
        CloseSelf();
    }

    void CloseSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }
}
