using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Quản lý panel đang mở, thứ tự xử lý ESC và khóa nhân vật khi có panel mở
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] PlayerInputReader playerInput;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] BillPanel billPanel;
    [SerializeField] InventoryView inventoryView;
    [SerializeField] DesktopOS desktopOS;
    [SerializeField] SkillTreeView skillTreeView;
    [SerializeField] PhoneController phoneController;

    readonly List<UIPanel> openPanels = new List<UIPanel>();

    // Có panel nào đang mở không
    public bool HasOpenPanel => openPanels.Count > 0;

    void Awake()
    {
        Instance = this;
        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInputReader>();
        if (pauseMenu == null)
            pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (billPanel == null)
            billPanel = FindFirstObjectByType<BillPanel>();
        if (inventoryView == null)
            inventoryView = FindFirstObjectByType<InventoryView>();
        if (desktopOS == null)
            desktopOS = FindFirstObjectByType<DesktopOS>();
        if (skillTreeView == null)
            skillTreeView = FindFirstObjectByType<SkillTreeView>();
        if (phoneController == null)
            phoneController = FindFirstObjectByType<PhoneController>();
    }

    void Start()
    {
        GameSettings.Load().ApplyToRuntime();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            HandleEscape();
            return;
        }

        if (kb.bKey.wasPressedThisFrame && billPanel != null)
        {
            TogglePanel(billPanel);
            return;
        }

        if ((kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) && inventoryView != null)
        {
            TogglePanel(inventoryView);
            return;
        }

        if (kb.cKey.wasPressedThisFrame && desktopOS != null)
        {
            TogglePanel(desktopOS);
            return;
        }

        if (kb.kKey.wasPressedThisFrame && skillTreeView != null)
        {
            TogglePanel(skillTreeView);
            return;
        }

        if ((kb.pKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame) && phoneController != null)
        {
            TogglePanel(phoneController);
            return;
        }
    }

    void TogglePanel(UIPanel panel)
    {
        if (panel == null) return;
        if (panel.IsOpen)
            Close(panel);
        else if (!HasOpenPanel)
            Open(panel);
    }

    // Mở một panel và đưa lên trên cùng
    public void Open(UIPanel panel)
    {
        if (panel == null) return;

        if (!openPanels.Contains(panel))
        {
            openPanels.Add(panel);
            if (panel.FreezePlayer && playerInput != null)
                playerInput.Freeze();
        }
        else
        {
            openPanels.Remove(panel);
            openPanels.Add(panel);
        }

        panel.Show();
    }

    // Đóng một panel
    public void Close(UIPanel panel)
    {
        if (panel == null) return;

        if (openPanels.Remove(panel))
        {
            panel.Hide();
            if (panel.FreezePlayer && playerInput != null)
                playerInput.Unfreeze();
        }
        else if (panel.IsOpen)
        {
            panel.Hide();
        }
    }

    // ESC: đóng panel trên cùng, không còn panel nào thì mở Pause
    public void HandleEscape()
    {
        if (openPanels.Count > 0)
        {
            UIPanel top = openPanels[openPanels.Count - 1];
            Close(top);
            return;
        }

        if (pauseMenu != null)
            Open(pauseMenu);
    }
}
