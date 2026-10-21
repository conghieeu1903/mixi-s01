using UnityEngine;

// Vết bẩn trong nhà, lau bằng cây lau nhà
public class Dirt : MonoBehaviour, IInteractable
{
    [Range(0f, 1f)] [SerializeField] float amount = 1f;

    public float Amount => amount;
    public string Prompt => $"Lau vết bẩn ({Mathf.RoundToInt(amount * 100f)}%)";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return amount > 0f;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Clean(0.6f);
        FindFirstObjectByType<Mop>()?.PlaySweepAnimation(transform.position);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã lau dọn vết bẩn trên sàn!", true);
    }

    // Lau bớt vết bẩn
    public void Clean(float power)
    {
        if (power <= 0f) return;
        amount = Mathf.Clamp01(amount - power);
        if (amount <= 0.01f)
        {
            amount = 0f;
            gameObject.SetActive(false);
        }
        else
        {
            transform.localScale = new Vector3(0.6f * amount + 0.2f, 0.02f, 0.6f * amount + 0.2f);
        }

        CleaningSystem sys = FindFirstObjectByType<CleaningSystem>();
        if (sys != null)
            sys.RefreshCleanliness();
    }

    public void RestoreDirty(float newAmount = 1f)
    {
        amount = Mathf.Clamp01(newAmount);
        gameObject.SetActive(true);
        transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
    }
}
