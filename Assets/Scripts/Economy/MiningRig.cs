using UnityEngine;

// Rig đào coin đặt trong nhà
public class MiningRig : MonoBehaviour, IInteractable
{
    [SerializeField] float miningPower = 1f;
    [SerializeField] bool startRunning = true;

    public float MiningPower => miningPower;
    public bool IsRunning { get; private set; }

    public string Prompt => IsRunning
        ? $"Tắt Rig đào Bitcoin ({miningPower:0.#} TH/s)"
        : $"Bật Rig đào Bitcoin ({miningPower:0.#} TH/s)";

    void Awake()
    {
        IsRunning = startRunning;
    }

    public void SetRunning(bool running)
    {
        IsRunning = running;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        SetRunning(!IsRunning);
        AudioManager.PlayAt(IsRunning ? "mining_beep" : "pc_shutdown", transform.position);
        FindFirstObjectByType<HudView>()?.ShowInfo(
            IsRunning ? "Đã bật Rig đào Bitcoin" : "Đã tắt Rig đào Bitcoin",
            true);
    }
}
