using UnityEngine;

// Âm bước chân, nhảy và tiếp đất cho người chơi, chọn âm theo mặt sàn đang đứng (gỗ, bê tông, cỏ)
[RequireComponent(typeof(PlayerMotor))]
public class FootstepAudio : MonoBehaviour
{
    [Tooltip("Quãng đường đi mỗi bước khi đi bộ (m)")]
    [SerializeField] float strideLength = 1.6f;

    [Tooltip("Tốc độ tối thiểu để tính là đang bước (m/s)")]
    [SerializeField] float minSpeed = 0.4f;

    static readonly string[] Wood = { "footstep_wood_01", "footstep_wood_02", "footstep_wood_03", "footstep_wood_04" };
    static readonly string[] Concrete = { "footstep_concrete_01", "footstep_concrete_02", "footstep_concrete_03", "footstep_concrete_04" };
    static readonly string[] Grass = { "footstep_grass_01", "footstep_grass_02" };

    PlayerMotor motor;
    float distanceSinceStep;
    bool wasGrounded = true;
    float lastY;
    int lastIndex = -1;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        lastY = transform.position.y;
    }

    void Update()
    {
        if (!motor.enabled) return;

        bool grounded = motor.IsGrounded;
        float y = transform.position.y;

        if (wasGrounded && !grounded && y > lastY)
            AudioManager.Play("jump");
        else if (!wasGrounded && grounded)
            PlayStep(0.9f);

        if (grounded && motor.PlanarSpeed > minSpeed)
        {
            distanceSinceStep += motor.PlanarSpeed * Time.deltaTime;
            float stride = motor.IsSprinting ? strideLength * 1.3f : motor.IsCrouching ? strideLength * 0.7f : strideLength;
            if (distanceSinceStep >= stride)
            {
                distanceSinceStep = 0f;
                PlayStep(motor.IsCrouching ? 0.6f : 1f);
            }
        }
        else if (grounded)
        {
            distanceSinceStep = strideLength * 0.5f;
        }

        wasGrounded = grounded;
        lastY = y;
    }

    // Phát một bước chân theo mặt sàn, không lặp lại đúng âm vừa phát, cao độ dao động nhẹ cho tự nhiên
    void PlayStep(float pitchScale)
    {
        string[] set = SurfaceSet();
        int index = Random.Range(0, set.Length);
        if (set.Length > 1 && index == lastIndex)
            index = (index + 1) % set.Length;
        lastIndex = index;
        AudioManager.Play(set[index], Random.Range(0.92f, 1.08f) * pitchScale);
    }

    // Raycast xuống chân để xác định loại sàn theo tên vật thể
    string[] SurfaceSet()
    {
        if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 1f, ~0, QueryTriggerInteraction.Ignore))
            return Wood;

        string surface = hit.collider.name.ToLowerInvariant();
        if (surface.Contains("grass") || surface.Contains("terrain") || surface.Contains("park"))
            return Grass;
        if (surface.Contains("road") || surface.Contains("asphalt") || surface.Contains("sidewalk") || surface.Contains("street") || surface.Contains("ground"))
            return Concrete;
        return Wood;
    }
}
