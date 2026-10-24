using UnityEngine;

// Gom PC, router và thiết bị trên một bàn máy thành điểm chất lượng stream
public class StreamSetup : MonoBehaviour
{
    [SerializeField] ComputerDesk desk;
    [SerializeField] PcCase pcCase;
    [SerializeField] Router router;

    public PcCase Case => pcCase;
    public Router ActiveRouter => router;

    // Có stream được không (đủ máy và mạng)
    public bool CanStream
    {
        get
        {
            RefreshComponents();
            return pcCase != null && pcCase.IsBootable && router != null && router.gameObject.activeInHierarchy;
        }
    }

    void Awake()
    {
        RefreshComponents();
    }

    void Start()
    {
        RefreshComponents();
    }

    // Tự tìm các thành phần trong phòng và đồng bộ hệ số viewer cho StreamSystem
    public void RefreshComponents()
    {
        if (desk == null)
            desk = GetComponent<ComputerDesk>() ?? FindFirstObjectByType<ComputerDesk>();
        if (pcCase == null)
            pcCase = FindFirstObjectByType<PcCase>();
        if (pcCase != null && !pcCase.IsBootable)
            pcCase.EnsureDefaultStarterParts();
        if (router == null || !router.gameObject.activeInHierarchy)
            router = FindFirstObjectByType<Router>();
        if (router == null && Application.isPlaying)
            router = gameObject.GetComponent<Router>() ?? gameObject.AddComponent<Router>();

        if (GameWorld.HasInstance)
            GameWorld.Instance.Stream.RigViewerBonus = ViewerBonus();
    }

    // Điểm chất lượng stream (0–100)
    public float StreamQuality()
    {
        if (pcCase == null)
            pcCase = FindFirstObjectByType<PcCase>();
        if (router == null || !router.gameObject.activeInHierarchy)
            router = FindFirstObjectByType<Router>();

        float score = RigPerformance.Quality(pcCase) * 0.65f;

        if (router != null && router.Data != null)
            score += Mathf.Clamp(router.Data.uploadMbps * 0.4f, 2f, 15f);
        else if (router != null)
            score += 5f;

        StreamGear[] gears = FindObjectsByType<StreamGear>(FindObjectsSortMode.None);
        for (int i = 0; i < gears.Length; i++)
        {
            if (gears[i] != null && gears[i].gameObject.activeInHierarchy)
                score += gears[i].QualityBonus;
        }

        return Mathf.Clamp(score, 0f, 100f);
    }

    // Điểm phần trăm viewer cộng thêm cho StreamSystem mỗi đợt 30 phút
    public int ViewerBonus()
    {
        if (pcCase == null || !pcCase.IsBootable) return 0;
        return Mathf.RoundToInt(StreamQuality() / 12f);
    }
}
