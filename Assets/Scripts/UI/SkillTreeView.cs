using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Màn kỹ năng: cây kỹ năng, điểm còn lại, nút học
public class SkillTreeView : UIPanel
{
    VisualElement listContainer;
    Label pointsLabel;
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
        box.style.width = 600;
        box.style.height = 520;
        box.style.paddingLeft = 20;
        box.style.paddingRight = 20;
        box.style.paddingTop = 16;
        box.style.paddingBottom = 16;

        VisualElement header = new VisualElement();
        header.AddToClassList("wf-panel__header");

        Label title = new Label("Cây Kỹ Năng Streamer (Phím K)");
        title.AddToClassList("wf-subheading");
        title.AddToClassList("wf-grow");

        Button closeBtn = new Button(CloseSelf) { text = "×" };
        closeBtn.AddToClassList("wf-button");
        closeBtn.AddToClassList("wf-button--square");

        header.Add(title);
        header.Add(closeBtn);
        box.Add(header);

        pointsLabel = new Label("Điểm kỹ năng khả dụng: 1");
        pointsLabel.AddToClassList("wf-subheading");
        pointsLabel.style.marginBottom = 8;
        box.Add(pointsLabel);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        listContainer = scroll;
        box.Add(scroll);

        host.Add(box);
    }

    public override void Show()
    {
        EnsureFrame();
        Rebuild();
        base.Show();
    }

    // Dựng lại cây kỹ năng
    void Rebuild()
    {
        EnsureFrame();
        if (listContainer == null || !GameWorld.HasInstance) return;

        SkillSystem skills = GameWorld.Instance.Skills;
        if (pointsLabel != null)
            pointsLabel.text = $"Điểm kỹ năng khả dụng: {skills.Points} · Đã học: {skills.Learned.Count}/22";

        listContainer.Clear();
        IReadOnlyList<SkillData> all = SkillSystem.AllSkills();
        for (int i = 0; i < all.Count; i++)
        {
            SkillData skill = all[i];
            bool learned = skills.Has(skill.id);

            VisualElement row = new VisualElement();
            row.AddToClassList("world-panel__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;

            Label text = new Label($"{skill.displayName} — {skill.description}");
            text.AddToClassList("wf-text");
            text.style.flexGrow = 1;

            Button btn = new Button(() =>
            {
                if (skills.Learn(skill))
                {
                    FindFirstObjectByType<HudView>()?.ShowInfo($"Đã học kỹ năng {skill.displayName}!", true);
                    Rebuild();
                }
            })
            {
                text = learned ? "Đã học" : $"Học ({skill.cost} Đ)"
            };
            btn.AddToClassList("wf-button");
            btn.AddToClassList("wf-button--small");
            if (!learned) btn.AddToClassList("wf-button--primary");
            btn.SetEnabled(!learned && skills.Points >= skill.cost);

            row.Add(text);
            row.Add(btn);
            listContainer.Add(row);
        }
    }

    void CloseSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }
}
