using UnityEngine;
using UnityEngine.InputSystem;

// Đọc input của nhân vật từ InputSystem_Actions và cho khóa input khi mở UI
public class PlayerInputReader : MonoBehaviour
{
    const string PlayerMap = "Player";

    [Tooltip("Asset input chứa action map Player (InputSystem_Actions)")]
    [SerializeField] InputActionAsset actions;

    [Tooltip("Tự khóa và ẩn chuột khi không bị đóng băng")]
    [SerializeField] bool lockCursor = true;

    InputAction move;
    InputAction look;
    InputAction jump;
    InputAction sprint;
    InputAction crouch;
    InputAction interact;

    // Số UI đang yêu cầu đóng băng nhân vật, lớn hơn 0 thì bỏ qua input
    int freezeCount;

    public bool IsFrozen => freezeCount > 0;

    // Hướng di chuyển (x trái phải, y tiến lùi), bằng 0 khi bị đóng băng
    public Vector2 Move => IsFrozen ? Vector2.zero : ReadMoveVector();

    // Có đang giữ chạy nhanh không
    public bool SprintHeld => !IsFrozen && ((sprint != null && sprint.IsPressed()) || GameSettings.IsKeyHeld(GameSettings.Current.keySprint));

    // Có đang giữ cúi không
    public bool CrouchHeld => !IsFrozen && ((crouch != null && crouch.IsPressed()) || GameSettings.IsKeyHeld(GameSettings.Current.keyCrouch));

    // Vừa bấm nhảy trong frame này
    public bool JumpPressed => !IsFrozen && ((jump != null && jump.WasPressedThisFrame()) || GameSettings.WasKeyPressedThisFrame(GameSettings.Current.keyJump));

    // Vừa bấm tương tác trong frame này
    public bool InteractPressed => !IsFrozen && ((interact != null && interact.WasPressedThisFrame()) || GameSettings.WasKeyPressedThisFrame(GameSettings.Current.keyInteract));

    Vector2 ReadMoveVector()
    {
        Vector2 v = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        if (v.sqrMagnitude > 0.001f) return v;
        GameSettings s = GameSettings.Current;
        float x = (GameSettings.IsKeyHeld(s.keyMoveRight) ? 1f : 0f) - (GameSettings.IsKeyHeld(s.keyMoveLeft) ? 1f : 0f);
        float y = (GameSettings.IsKeyHeld(s.keyMoveUp) ? 1f : 0f) - (GameSettings.IsKeyHeld(s.keyMoveDown) ? 1f : 0f);
        Vector2 custom = new Vector2(x, y);
        return custom.sqrMagnitude > 1f ? custom.normalized : custom;
    }

    // Lấy action từ asset và bật action map
    void Awake()
    {
        if (actions == null) return;
        InputActionMap map = actions.FindActionMap(PlayerMap, true);
        move = map.FindAction("Move", true);
        look = map.FindAction("Look", true);
        jump = map.FindAction("Jump", true);
        sprint = map.FindAction("Sprint", true);
        crouch = map.FindAction("Crouch", true);
        interact = map.FindAction("Interact", false);
    }

    // Bật input và cập nhật con trỏ
    void OnEnable()
    {
        if (actions != null) actions.FindActionMap(PlayerMap, true).Enable();
        ApplyCursor();
    }

    // Tắt input và trả con trỏ về bình thường
    void OnDisable()
    {
        if (actions != null) actions.FindActionMap(PlayerMap)?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Đọc input xoay camera, trả về độ xoay (độ) đã nhân độ nhạy và đã bù deltaTime với tay cầm
    public Vector2 ReadLook(float mouseSensitivity, float gamepadDegreesPerSecond)
    {
        if (IsFrozen || look == null) return Vector2.zero;
        Vector2 value = look.ReadValue<Vector2>();
        bool isGamepad = look.activeControl != null && look.activeControl.device is Gamepad;
        return isGamepad ? value * (gamepadDegreesPerSecond * Time.deltaTime) : value * mouseSensitivity;
    }

    // Yêu cầu đóng băng nhân vật (mỗi lần mở UI gọi một lần, phải gọi Unfreeze tương ứng)
    public void Freeze()
    {
        freezeCount++;
        ApplyCursor();
    }

    // Hủy một yêu cầu đóng băng
    public void Unfreeze()
    {
        freezeCount = Mathf.Max(0, freezeCount - 1);
        ApplyCursor();
    }

    // Khóa chuột khi chơi, hiện chuột khi có UI (riêng màn hình PC dùng con trỏ ảo riêng nên ẩn chuột hệ thống)
    void ApplyCursor()
    {
        if (!lockCursor) return;
        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        bool pcActive = desktop != null && desktop.IsOpen;
        Cursor.lockState = IsFrozen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsFrozen && !pcActive;
    }
}
