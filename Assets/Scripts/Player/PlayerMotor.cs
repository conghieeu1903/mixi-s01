using UnityEngine;

// Di chuyển nhân vật góc nhìn thứ nhất: đi, chạy, cúi, nhảy, trọng lực
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Tốc độ")]
    [Tooltip("Tốc độ đi bộ (m/s)")]
    [SerializeField] float walkSpeed = 3.5f;

    [Tooltip("Tốc độ chạy (m/s)")]
    [SerializeField] float sprintSpeed = 6f;

    [Tooltip("Tốc độ khi cúi (m/s)")]
    [SerializeField] float crouchSpeed = 1.8f;

    [Tooltip("Độ gắt khi tăng giảm tốc, càng lớn càng dừng/đổi hướng nhanh")]
    [SerializeField] float acceleration = 12f;

    [Header("Nhảy và trọng lực")]
    [Tooltip("Chiều cao cú nhảy (m)")]
    [SerializeField] float jumpHeight = 1.1f;

    [Tooltip("Gia tốc trọng lực (m/s²), giá trị âm")]
    [SerializeField] float gravity = -20f;

    [Tooltip("Cho phép nhảy thêm một chút sau khi vừa rời mặt đất (giây)")]
    [SerializeField] float coyoteTime = 0.1f;

    [Header("Cúi")]
    [Tooltip("Chiều cao capsule khi đứng")]
    [SerializeField] float standHeight = 1.8f;

    [Tooltip("Chiều cao capsule khi cúi")]
    [SerializeField] float crouchHeight = 1.1f;

    [Tooltip("Tốc độ chuyển giữa đứng và cúi (m/s)")]
    [SerializeField] float crouchTransitionSpeed = 6f;

    [Tooltip("Layer coi là vật cản khi đứng dậy")]
    [SerializeField] LayerMask obstacleMask = ~0;

    CharacterController controller;
    PlayerInputReader input;

    Vector3 planarVelocity;
    float verticalVelocity;
    float timeSinceGrounded;
    bool wantsCrouch;

    public bool IsGrounded => controller.isGrounded;
    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

    // Tốc độ ngang hiện tại (m/s), dùng cho animation và tiếng bước chân
    public float PlanarSpeed => planarVelocity.magnitude;

    // Chiều cao capsule hiện tại, PlayerLook dùng để đặt độ cao camera
    public float CurrentHeight => controller.height;

    // Lấy component cần dùng
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        ApplyHeight(standHeight);
    }

    // Cập nhật cúi, tốc độ ngang, trọng lực rồi di chuyển
    void Update()
    {
        float dt = Time.deltaTime;
        UpdateCrouch(dt);
        UpdateHorizontal(dt);
        UpdateVertical(dt);

        Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * dt);
    }

    // Đổi giữa đứng và cúi, chỉ đứng dậy khi trên đầu không bị chặn
    void UpdateCrouch(float dt)
    {
        wantsCrouch = input.CrouchHeld;
        if (wantsCrouch) IsCrouching = true;
        else if (IsCrouching && CanStandUp()) IsCrouching = false;

        float target = IsCrouching ? crouchHeight : standHeight;
        if (!Mathf.Approximately(controller.height, target))
            ApplyHeight(Mathf.MoveTowards(controller.height, target, crouchTransitionSpeed * dt));
    }

    // Kiểm tra có đủ chỗ trống phía trên đầu để đứng dậy không
    bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standHeight - radius);
        return !Physics.CheckCapsule(bottom, top, radius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    // Đặt chiều cao capsule, giữ chân đứng yên ở gốc
    void ApplyHeight(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height * 0.5f, 0f);
    }

    // Tính vận tốc ngang theo hướng nhìn của thân, có tăng giảm tốc
    void UpdateHorizontal(float dt)
    {
        Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
        IsSprinting = input.SprintHeld && !IsCrouching && move.y > 0.1f;

        float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
        Vector3 wish = (transform.right * move.x + transform.forward * move.y) * speed;
        planarVelocity = Vector3.Lerp(planarVelocity, wish, 1f - Mathf.Exp(-acceleration * dt));
    }

    // Trọng lực và nhảy
    void UpdateVertical(float dt)
    {
        if (controller.isGrounded)
        {
            timeSinceGrounded = 0f;
            if (verticalVelocity < 0f) verticalVelocity = -2f; // Ép nhẹ xuống đất để isGrounded ổn định
        }
        else
        {
            timeSinceGrounded += dt;
        }

        if (input.JumpPressed && timeSinceGrounded <= coyoteTime && !IsCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            timeSinceGrounded = coyoteTime + 1f; // Chặn nhảy đôi
        }

        verticalVelocity += gravity * dt;
    }

    // Dịch chuyển tức thời (về nhà, cửa hàng...), tắt CharacterController tạm để không bị ghi đè vị trí
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        controller.enabled = true;
        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }
}
