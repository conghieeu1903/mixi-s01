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

    // Số UI đang yêu cầu đóng băng nhân vật, lớn hơn 0 thì bỏ qua input
    int freezeCount;

    public bool IsFrozen => freezeCount > 0;

    // Hướng di chuyển (x trái phải, y tiến lùi), bằng 0 khi bị đóng băng
    public Vector2 Move => IsFrozen ? Vector2.zero : move.ReadValue<Vector2>();

    // Có đang giữ chạy nhanh không
    public bool SprintHeld => !IsFrozen && sprint.IsPressed();

    // Có đang giữ cúi không
    public bool CrouchHeld => !IsFrozen && crouch.IsPressed();

    // Vừa bấm nhảy trong frame này
    public bool JumpPressed => !IsFrozen && jump.WasPressedThisFrame();

    // Lấy action từ asset và bật action map
    void Awake()
    {
        InputActionMap map = actions.FindActionMap(PlayerMap, true);
        move = map.FindAction("Move", true);
        look = map.FindAction("Look", true);
        jump = map.FindAction("Jump", true);
        sprint = map.FindAction("Sprint", true);
        crouch = map.FindAction("Crouch", true);
    }

    // Bật input và cập nhật con trỏ
    void OnEnable()
    {
        actions.FindActionMap(PlayerMap, true).Enable();
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
        if (IsFrozen) return Vector2.zero;
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

    // Khóa chuột khi chơi, hiện chuột khi có UI
    void ApplyCursor()
    {
        if (!lockCursor) return;
        Cursor.lockState = IsFrozen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsFrozen;
    }
}
