using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Màn lắp ráp PC: danh sách slot, lắp và tháo linh kiện
public class PcBuildView : UIPanel
{
    PcCase currentCase;
    VisualElement buildPanel;
    VisualElement rowsContainer;
    Label statusLabel;
    bool initialized;

    void OnEnable()
    {
        initialized = false;
        SetupPanel();
    }

    void SetupPanel()
    {
        if (initialized || Root == null) return;
        initialized = true;

        VisualElement board = Root.Q<VisualElement>("worldPanels");
        buildPanel = Root.Q<VisualElement>("buildCasePanel");
        if (board != null && buildPanel != null)
        {
            board.style.backgroundColor = new Color(0f, 0f, 0f, 0.16f);
            board.style.alignItems = Align.FlexEnd;
            board.style.justifyContent = Justify.Center;
            board.style.paddingRight = 44;

            foreach (VisualElement child in board.Children())
            {
                bool hasBuild = child.Q<VisualElement>("buildCasePanel") != null || child == buildPanel;
                child.style.display = hasBuild ? DisplayStyle.Flex : DisplayStyle.None;
                Label caption = child.Q<Label>(className: "wf-board__caption");
                if (caption != null) caption.style.display = DisplayStyle.None;
            }
        }

        if (buildPanel == null) return;
        buildPanel.style.width = 500;
        buildPanel.style.minHeight = 460;

        Button closeBtn = buildPanel.Q<Button>("closeButton");
        if (closeBtn != null)
            closeBtn.clicked += CloseSelf;

        VisualElement footer = buildPanel.Q<VisualElement>(className: "world-panel__footer");
        if (footer != null)
        {
            statusLabel = footer.Q<Label>();
            Button doneBtn = footer.Q<Button>();
            if (doneBtn != null)
                doneBtn.clicked += CloseSelf;
        }

        List<VisualElement> oldRows = buildPanel.Query<VisualElement>(className: "world-panel__row").ToList();
        for (int i = 0; i < oldRows.Count; i++)
            oldRows[i].RemoveFromHierarchy();

        rowsContainer = new ScrollView { name = "pcSlotsScroll" };
        rowsContainer.style.flexGrow = 1;
        buildPanel.Insert(1, rowsContainer);
    }

    // Mở cho một vỏ case
    public void Open(PcCase pcCase)
    {
        currentCase = pcCase != null ? pcCase : FindFirstObjectByType<PcCase>();
        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    public override void Show()
    {
        SetupPanel();
        if (currentCase == null)
            currentCase = FindFirstObjectByType<PcCase>();

        if (currentCase != null)
        {
            currentCase.SetSidePanelOpen(true);
            PlayerLook look = FindFirstObjectByType<PlayerLook>();
            if (look != null)
            {
                Vector3 camPos = currentCase.transform.TransformPoint(new Vector3(0.56f, 0.30f, 0.08f));
                Vector3 lookTarget = currentCase.transform.TransformPoint(new Vector3(0.0f, 0.24f, -0.10f));
                Vector3 dir = lookTarget - camPos;
                Quaternion camRot = dir.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                    : currentCase.transform.rotation;
                look.HoldCameraPose(camPos, camRot, 0.45f, 54f);
            }
        }

        Rebuild();
        base.Show();
    }

    public override void Hide()
    {
        if (currentCase != null)
            currentCase.SetSidePanelOpen(false);
        FindFirstObjectByType<PlayerLook>()?.ReleaseCameraPose(0.42f);
        base.Hide();
    }

    // Dựng lại danh sách slot
    void Rebuild()
    {
        SetupPanel();
        if (rowsContainer == null || currentCase == null) return;

        rowsContainer.Clear();
        IReadOnlyList<PcSlot> slots = currentCase.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            PcSlot slot = slots[i];
            VisualElement row = new VisualElement();
            row.AddToClassList("world-panel__row");

            Label typeLabel = new Label(SlotName(slot.type));
            typeLabel.AddToClassList("wf-text");
            typeLabel.AddToClassList("world-panel__row-name");
            typeLabel.style.width = 130;
            typeLabel.style.flexShrink = 0;

            string desc = slot.IsEmpty
                ? "Trống"
                : $"{slot.installed.displayName} (Chất lượng +{slot.installed.quality:0})";
            Label valLabel = new Label(desc);
            valLabel.AddToClassList("wf-caption");
            valLabel.AddToClassList("world-panel__row-value");

            Button actionBtn = new Button(() => OnSlotAction(slot))
            {
                text = slot.IsEmpty ? "Lắp" : "Tháo"
            };
            actionBtn.AddToClassList("wf-button");
            actionBtn.AddToClassList("wf-button--small");
            if (slot.IsEmpty)
                actionBtn.SetEnabled(FindPartInInventory(slot.type) != null);

            row.Add(typeLabel);
            row.Add(valLabel);
            row.Add(actionBtn);
            rowsContainer.Add(row);
        }

        if (statusLabel != null)
        {
            float qual = RigPerformance.Quality(currentCase);
            int fps = RigPerformance.Fps(currentCase);
            string boot = currentCase.IsBootable ? "Đủ linh kiện chạy" : "Chưa đủ linh kiện";
            statusLabel.text = $"{boot} · Chất lượng: {qual:0}/100 · {fps} FPS";
        }
    }

    // Tên tiếng Việt của khe linh kiện
    static string SlotName(PcPartType type)
    {
        switch (type)
        {
            case PcPartType.Motherboard: return "Mainboard";
            case PcPartType.Cpu: return "CPU";
            case PcPartType.CpuCooler: return "Tản nhiệt";
            case PcPartType.Ram: return "RAM";
            case PcPartType.GraphicsCard: return "Card đồ họa";
            case PcPartType.Psu: return "Nguồn";
            case PcPartType.Storage: return "Ổ cứng";
            case PcPartType.Optical: return "Ổ đĩa";
            default: return type.ToString();
        }
    }

    PcPartData FindPartInInventory(PcPartType type)
    {
        if (!GameWorld.HasInstance) return null;
        List<ItemData> items = GameWorld.Instance.Inventory.Items;
        PcPartData best = null;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is PcPartData part && part.partType == type)
            {
                if (best == null || part.quality > best.quality)
                    best = part;
            }
        }
        return best;
    }

    void OnSlotAction(PcSlot slot)
    {
        if (slot == null || currentCase == null || !GameWorld.HasInstance) return;
        HudView hud = FindFirstObjectByType<HudView>();

        if (!slot.IsEmpty)
        {
            PcPartData removed = currentCase.Remove(slot);
            if (removed != null)
            {
                if (!GameWorld.Instance.Inventory.Add(removed))
                    WorldItem.Spawn(removed, currentCase.transform.position + Vector3.up * 0.3f, Quaternion.identity);
                hud?.ShowInfo($"Đã tháo {removed.displayName}", true);
            }
            Rebuild();
            return;
        }

        PcPartData candidate = FindPartInInventory(slot.type);
        if (candidate != null && currentCase.Install(candidate))
        {
            GameWorld.Instance.Inventory.Remove(candidate);
            hud?.ShowInfo($"Đã lắp {candidate.displayName} (+{candidate.quality:0} Chất lượng)", true);
            Rebuild();
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
