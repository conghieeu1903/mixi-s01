using UnityEngine;

// Thú cưng: đi theo người chơi, có chỉ số đói và vui
public class Pet : MonoBehaviour, IInteractable
{
    [SerializeField] string petName = "Mèo Mốc";
    [SerializeField] float followSpeed = 2.2f;

    public string PetName => petName;
    public float Hunger { get; private set; }
    public float Happiness { get; private set; } = 100f;
    public string Prompt => $"Vuốt ve {petName} (Vui: {Happiness:0}%)";

    Transform playerTarget;

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
        if (playerTarget == null)
        {
            PlayerMotor pm = FindFirstObjectByType<PlayerMotor>();
            if (pm != null) playerTarget = pm.transform;
        }
        if (playerTarget == null) return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        if (dist > 1.8f && dist < 8f)
        {
            Vector3 dir = toPlayer.normalized;
            transform.position += dir * (followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(dir);
        }
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
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã vuốt ve {petName}! (+4 Mood, +Viewer khi stream)", true);
    }

    public void Feed()
    {
        Hunger = 0f;
        Happiness = 100f;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(3f);
    }
}
