using System.Collections;
using UnityEngine;

// Thú cưng: đi theo người chơi kèm chuyển động bước chân, vẫy đuôi và nhảy vui mừng khi vuốt ve/cho ăn
public class Pet : MonoBehaviour, IInteractable
{
    [SerializeField] string petName = "Mèo Mốc";
    [SerializeField] float followSpeed = 2.2f;

    public string PetName => petName;
    public float Hunger { get; private set; }
    public float Happiness { get; private set; } = 100f;
    public string Prompt => $"Vuốt ve {petName} (Vui: {Happiness:0}%)";

    Transform playerTarget;
    Transform bodyMesh;
    Transform headMesh;
    Transform tailMesh;
    Transform legFL;
    Transform legFR;
    Transform legBL;
    Transform legBR;

    Vector3 bodyBasePos;
    Vector3 headBasePos;
    Quaternion tailBaseRot = Quaternion.Euler(-35f, 0f, 0f);
    float animPhase;
    float happyBoostTimer;
    Coroutine happyRoutine;

    void Awake()
    {
        CacheParts();
    }

    void CacheParts()
    {
        bodyMesh = transform.Find("Mesh_Body");
        headMesh = transform.Find("Mesh_Head");
        tailMesh = transform.Find("Mesh_Tail");
        legFL = transform.Find("Mesh_LegFL");
        legFR = transform.Find("Mesh_LegFR");
        legBL = transform.Find("Mesh_LegBL");
        legBR = transform.Find("Mesh_LegBR");

        if (bodyMesh != null) bodyBasePos = bodyMesh.localPosition;
        if (headMesh != null) headBasePos = headMesh.localPosition;
        if (tailMesh != null) tailBaseRot = tailMesh.localRotation;
    }

    void OnEnable()
    {
        if (GameWorld.HasInstance && !GameWorld.Instance.Pets.Pets.Contains(this))
            GameWorld.Instance.Pets.Pets.Add(this);
    }

    void OnDisable()
    {
        if (GameWorld.HasInstance)
            GameWorld.Instance.Pets.Pets.Remove(this);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (happyBoostTimer > 0f)
            happyBoostTimer -= dt;

        if (playerTarget == null)
        {
            PlayerMotor pm = FindFirstObjectByType<PlayerMotor>();
            if (pm != null) playerTarget = pm.transform;
        }

        bool isWalking = false;
        if (playerTarget != null)
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist > 1.8f && dist < 8f)
            {
                isWalking = true;
                Vector3 dir = toPlayer.normalized;
                transform.position += dir * (followSpeed * dt);
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * dt);
            }
        }

        AnimateCatParts(dt, isWalking);
    }

    void AnimateCatParts(float dt, bool isWalking)
    {
        float tailSpeed = happyBoostTimer > 0f ? 14f : (isWalking ? 9f : 4.2f);
        animPhase += dt * (isWalking ? 12f : 3.2f);

        if (tailMesh != null)
        {
            float wag = Mathf.Sin(Time.time * tailSpeed) * (happyBoostTimer > 0f ? 32f : 20f);
            tailMesh.localRotation = tailBaseRot * Quaternion.Euler(0f, wag, wag * 0.4f);
        }

        if (bodyMesh != null)
        {
            float bob = isWalking ? Mathf.Abs(Mathf.Sin(animPhase)) * 0.022f : Mathf.Sin(Time.time * 2.6f) * 0.006f;
            bodyMesh.localPosition = bodyBasePos + new Vector3(0f, bob, 0f);
        }

        if (headMesh != null)
        {
            float nod = isWalking ? Mathf.Sin(animPhase + 0.6f) * 0.015f : Mathf.Sin(Time.time * 2.2f) * 0.005f;
            float tilt = happyBoostTimer > 0f ? Mathf.Sin(Time.time * 10f) * 14f : 0f;
            headMesh.localPosition = headBasePos + new Vector3(0f, nod, 0f);
            headMesh.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        float legSwing = isWalking ? Mathf.Sin(animPhase) * 28f : 0f;
        if (legFL != null) legFL.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
        if (legBR != null) legBR.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
        if (legFR != null) legFR.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
        if (legBL != null) legBL.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Happiness = Mathf.Min(100f, Happiness + 20f);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(4f);

        PlayPetVoice();
        PlayHappyJump();
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã vuốt ve {petName}! (+4 Mood, +Viewer khi stream)", true);
    }

    public void Feed()
    {
        Hunger = 0f;
        Happiness = 100f;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(3f);
        PlayPetVoice();
        AudioManager.Play("taming_success");
        PlayHappyJump();
    }

    // Phát tiếng kêu theo loài thú cưng (chó hoặc mèo) tại vị trí đứng
    void PlayPetVoice()
    {
        bool isDog = petName.IndexOf("Chó", System.StringComparison.OrdinalIgnoreCase) >= 0
                  || name.IndexOf("Dog", System.StringComparison.OrdinalIgnoreCase) >= 0;
        AudioManager.PlayAt(isDog ? "dog_bark" : "cat_meow", transform.position);
    }

    void PlayHappyJump()
    {
        happyBoostTimer = 1.6f;
        if (!isActiveAndEnabled) return;
        if (happyRoutine != null)
            StopCoroutine(happyRoutine);
        happyRoutine = StartCoroutine(HappyJumpRoutine());
    }

    IEnumerator HappyJumpRoutine()
    {
        float baseY = transform.position.y;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        const float duration = 0.45f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float hop = Mathf.Sin(t * Mathf.PI * 2f);
            Vector3 pos = transform.position;
            pos.y = baseY + Mathf.Max(0f, hop) * 0.16f;
            transform.position = pos;
            transform.rotation = startRot * Quaternion.Euler(0f, Mathf.Sin(t * Mathf.PI * 2f) * 16f, 0f);
            yield return null;
        }

        Vector3 endPos = transform.position;
        endPos.y = baseY;
        transform.position = endPos;
        happyRoutine = null;
    }
}
