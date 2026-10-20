using UnityEngine;

// Xoay camera góc nhìn thứ nhất: yaw xoay cả thân, pitch chỉ xoay camera
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerLook : MonoBehaviour
{
    [Tooltip("Transform của camera (con của nhân vật), xoay theo trục X để nhìn lên xuống")]
    [SerializeField] Transform cameraPivot;

    [Tooltip("Độ nhạy chuột (độ trên mỗi đơn vị delta)")]
    [SerializeField] float mouseSensitivity = 0.1f;

    [Tooltip("Tốc độ xoay tối đa với tay cầm (độ/giây)")]
    [SerializeField] float gamepadDegreesPerSecond = 180f;

    [Tooltip("Giới hạn góc nhìn lên (độ)")]
    [SerializeField] float maxPitchUp = 85f;

    [Tooltip("Giới hạn góc nhìn xuống (độ)")]
    [SerializeField] float maxPitchDown = 85f;

    [Tooltip("Đảo trục Y")]
    [SerializeField] bool invertY;

    [Tooltip("Khoảng cách từ mắt tới đỉnh capsule, camera = chiều cao capsule trừ giá trị này")]
    [SerializeField] float eyeOffsetFromTop = 0.1f;

    PlayerInputReader input;
    PlayerMotor motor;
    float pitch;

    // Độ nhạy chuột, cho menu Settings chỉnh
    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Max(0.001f, value);
    }

    public bool InvertY
    {
        get => invertY;
        set => invertY = value;
    }

    // Lấy component cần dùng
    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
    }

    // Xoay camera sau khi nhân vật đã di chuyển trong Update, tránh giật hình
    void LateUpdate()
    {
        Vector2 delta = input.ReadLook(mouseSensitivity, gamepadDegreesPerSecond);
        transform.Rotate(0f, delta.x, 0f, Space.Self);

        pitch += invertY ? delta.y : -delta.y;
        pitch = Mathf.Clamp(pitch, -maxPitchUp, maxPitchDown);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (motor != null)
        {
            Vector3 local = cameraPivot.localPosition;
            local.y = motor.CurrentHeight - eyeOffsetFromTop;
            cameraPivot.localPosition = local;
        }
    }

    // Đặt hướng nhìn trực tiếp (khi dịch chuyển hoặc ra khỏi màn hình máy tính)
    public void SetRotation(float yaw, float newPitch)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitch = Mathf.Clamp(newPitch, -maxPitchUp, maxPitchDown);
    }
}
