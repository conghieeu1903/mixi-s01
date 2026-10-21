using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng Kỹ năng trên điện thoại theo cơ chế SkillAppsPanel của SLS2: chọn kỹ năng, xem hộp thông tin và mở khóa
public class PhoneSkillApp : PhoneApp
{
    SkillData selectedSkill;
    VisualElement currentContent;

    public override string IconGlyph => "⭐";
    public override Color AccentColor => new Color(0.96f, 0.72f, 0.16f, 1f);
    public override bool BlockWhileDriving => false;

    void Awake()
    {
        SetDefaultAppName("Skill");
    }

    // Chọn một kỹ năng để hiển thị thanh thông tin chi tiết ở đáy giống OpenInfoBar của SLS2
    public void OpenInfoBar(SkillData skill)
    {
        selectedSkill = skill;
        AudioManager.PlayUi("phone_tap");
        RefreshView();
    }

    // Mở khóa kỹ năng đang chọn giống UnlockSkillBtn của SLS2
    public void UnlockSkillBtn()
    {
        if (selectedSkill == null || !GameWorld.HasInstance) return;
        SkillSystem skills = GameWorld.Instance.Skills;

        if (skills.Has(selectedSkill.id))
            return;

        if (skills.Points < selectedSkill.cost)
        {
            AudioManager.PlayUi("ui_error");
            Phone?.ShowPhoneWarning("Không đủ điểm kỹ năng (★) để mở khóa kỹ năng này!");
            return;
        }

        if (skills.Learn(selectedSkill))
        {
            AudioManager.Play("delivery_founded");
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã mở khóa kỹ năng: {selectedSkill.displayName}!", true);
            RefreshView();
        }
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

        SkillSystem skills = GameWorld.HasInstance ? GameWorld.Instance.Skills : null;
        int sp = skills != null ? skills.Points : 0;
        int learnedCount = skills != null ? skills.Learned.Count : 0;

        // Thanh trạng thái Điểm kỹ năng ★ và số lượng đã học
        VisualElement headerBox = new VisualElement();
        headerBox.style.flexDirection = FlexDirection.Row;
        headerBox.style.justifyContent = Justify.SpaceBetween;
        headerBox.style.alignItems = Align.Center;
        headerBox.style.backgroundColor = new Color(0.16f, 0.19f, 0.26f, 0.95f);
        headerBox.style.borderTopLeftRadius = 8;
        headerBox.style.borderTopRightRadius = 8;
        headerBox.style.borderBottomLeftRadius = 8;
        headerBox.style.borderBottomRightRadius = 8;
        headerBox.style.paddingLeft = 10;
        headerBox.style.paddingRight = 10;
        headerBox.style.paddingTop = 6;
        headerBox.style.paddingBottom = 6;
        headerBox.style.marginBottom = 8;

        Label spLbl = new Label($"★ Điểm kỹ năng: {sp}");
        spLbl.style.color = new Color(0.98f, 0.82f, 0.25f, 1f);
        spLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        spLbl.style.fontSize = 12;

        Label countLbl = new Label($"Đã học: {learnedCount}/22");
        countLbl.style.color = new Color(0.78f, 0.84f, 0.92f, 1f);
        countLbl.style.fontSize = 11;

        headerBox.Add(spLbl);
        headerBox.Add(countLbl);
        content.Add(headerBox);

        IReadOnlyList<SkillData> all = SkillSystem.AllSkills();
        if (selectedSkill == null && all.Count > 0)
            selectedSkill = all[0];

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        for (int i = 0; i < all.Count; i++)
        {
            SkillData skill = all[i];
            if (skill == null) continue;
            bool isLearned = skills != null && skills.Has(skill.id);
            bool isSelected = selectedSkill != null && selectedSkill.id == skill.id;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = isSelected
                ? new Color(0.25f, 0.32f, 0.45f, 0.98f)
                : new Color(0.15f, 0.17f, 0.22f, 0.92f);
            row.style.borderTopLeftRadius = 8;
            row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8;
            row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom = 4;

            Label nameLbl = new Label($"{(isLearned ? "✓ " : "★ ")}{skill.displayName}");
            nameLbl.style.color = isLearned ? new Color(0.38f, 0.95f, 0.56f, 1f) : Color.white;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.fontSize = 12;

            Button selectBtn = new Button(() => OpenInfoBar(skill))
            {
                text = isLearned ? "Đã mở" : $"{skill.cost} ★"
            };
            selectBtn.style.backgroundColor = isLearned
                ? new Color(0.18f, 0.48f, 0.3f, 1f)
                : new Color(0.96f, 0.72f, 0.16f, 1f);
            selectBtn.style.color = isLearned ? Color.white : new Color(0.1f, 0.1f, 0.12f, 1f);
            selectBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            selectBtn.style.fontSize = 10;
            selectBtn.style.borderTopLeftRadius = 6;
            selectBtn.style.borderTopRightRadius = 6;
            selectBtn.style.borderBottomLeftRadius = 6;
            selectBtn.style.borderBottomRightRadius = 6;
            selectBtn.style.paddingLeft = 8;
            selectBtn.style.paddingRight = 8;
            selectBtn.style.paddingTop = 3;
            selectBtn.style.paddingBottom = 3;

            row.Add(nameLbl);
            row.Add(selectBtn);
            scroll.Add(row);
        }

        content.Add(scroll);

        // Hộp chi tiết kỹ năng infoArea ở đáy màn hình giống SkillAppsPanel của SLS2
        if (selectedSkill != null)
        {
            bool learned = skills != null && skills.Has(selectedSkill.id);

            VisualElement infoArea = new VisualElement();
            infoArea.style.backgroundColor = new Color(0.12f, 0.14f, 0.19f, 0.98f);
            infoArea.style.borderTopLeftRadius = 10;
            infoArea.style.borderTopRightRadius = 10;
            infoArea.style.borderBottomLeftRadius = 10;
            infoArea.style.borderBottomRightRadius = 10;
            infoArea.style.paddingLeft = 10;
            infoArea.style.paddingRight = 10;
            infoArea.style.paddingTop = 8;
            infoArea.style.paddingBottom = 8;
            infoArea.style.marginTop = 6;
            infoArea.style.borderTopWidth = 1;
            infoArea.style.borderTopColor = new Color(0.96f, 0.72f, 0.16f, 0.45f);

            Label sName = new Label($"★ {selectedSkill.displayName} (Yêu cầu: {selectedSkill.cost} ★)");
            sName.style.color = new Color(0.98f, 0.82f, 0.25f, 1f);
            sName.style.unityFontStyleAndWeight = FontStyle.Bold;
            sName.style.fontSize = 12;
            sName.style.marginBottom = 3;

            Label sDesc = new Label(selectedSkill.description);
            sDesc.style.color = new Color(0.82f, 0.86f, 0.94f, 1f);
            sDesc.style.fontSize = 11;
            sDesc.style.whiteSpace = WhiteSpace.Normal;
            sDesc.style.marginBottom = 6;

            Button unlockBtn = new Button(UnlockSkillBtn)
            {
                text = learned ? "✓ Kỹ năng đã được kích hoạt" : $"Mở khóa kỹ năng ({selectedSkill.cost} ★)"
            };
            unlockBtn.SetEnabled(!learned);
            unlockBtn.style.backgroundColor = learned
                ? new Color(0.22f, 0.45f, 0.3f, 1f)
                : new Color(0.96f, 0.72f, 0.16f, 1f);
            unlockBtn.style.color = learned ? Color.white : new Color(0.1f, 0.1f, 0.12f, 1f);
            unlockBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            unlockBtn.style.fontSize = 12;
            unlockBtn.style.borderTopLeftRadius = 8;
            unlockBtn.style.borderTopRightRadius = 8;
            unlockBtn.style.borderBottomLeftRadius = 8;
            unlockBtn.style.borderBottomRightRadius = 8;
            unlockBtn.style.paddingTop = 6;
            unlockBtn.style.paddingBottom = 6;

            infoArea.Add(sName);
            infoArea.Add(sDesc);
            infoArea.Add(unlockBtn);
            content.Add(infoArea);
        }
    }
}
