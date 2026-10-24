using System.Collections.Generic;
using UnityEngine;

// Sơn tường: đặt thùng sơn ở góc phòng để người chơi bấm E đổi màu toàn bộ tường nhà mà không bị hiện nhầm gợi ý khi nhìn vào tường trống
public class WallPainter : MonoBehaviour, IInteractable
{
    [SerializeField] List<Color> palette = new List<Color>
    {
        new Color(0.88f, 0.91f, 0.95f),
        new Color(0.95f, 0.89f, 0.82f),
        new Color(0.82f, 0.92f, 0.86f),
        new Color(0.86f, 0.84f, 0.94f),
        new Color(0.76f, 0.85f, 0.96f)
    };
    [SerializeField] float pricePerWall = 40f;

    int nextColorIndex;
    bool isRoomRootDelegated;
    Transform targetRoomRoot;
    Renderer swatchRenderer;
    readonly List<Renderer> wallRenderers = new List<Renderer>();

    public string Prompt => $"Đổi màu sơn tường phòng ($ {pricePerWall:0})";

    void Awake()
    {
        // Nếu component đang gắn trên object Room gốc thì tách riêng ra một thùng sơn 3D ở góc phòng để tránh nhìn vào tường nào cũng hiện nút E
        if (gameObject.name.Contains("Room") || transform.Find("Floor") != null || transform.Find("Mesh_Floor") != null || transform.Find("Wall_North") != null || transform.Find("Mesh_WallBack") != null)
        {
            SetupPaintBucketChild();
            return;
        }

        if (targetRoomRoot == null && transform.parent != null)
            targetRoomRoot = transform.parent;
        CacheWallRenderers();
    }

    void SetupPaintBucketChild()
    {
        isRoomRootDelegated = true;
        Transform existing = transform.Find("PaintBucket");
        if (existing != null)
        {
            WallPainter childPainter = existing.GetComponent<WallPainter>();
            if (childPainter == null)
                childPainter = existing.gameObject.AddComponent<WallPainter>();
            childPainter.BindRoomRoot(transform, palette, pricePerWall);
            return;
        }

        GameObject bucketRoot = new GameObject("PaintBucket");
        bucketRoot.transform.SetParent(transform, false);
        bucketRoot.transform.localPosition = new Vector3(-2.52f, 0.10f, 1.62f);

        BoxCollider triggerCol = bucketRoot.AddComponent<BoxCollider>();
        triggerCol.center = new Vector3(0f, 0.20f, 0f);
        triggerCol.size = new Vector3(0.36f, 0.42f, 0.36f);

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Thân thùng sơn hình trụ
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Mesh_BucketBody";
        Collider bCol = body.GetComponent<Collider>();
        if (bCol != null) Destroy(bCol);
        body.transform.SetParent(bucketRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        body.transform.localScale = new Vector3(0.26f, 0.16f, 0.26f);
        Renderer bodyRend = body.GetComponent<Renderer>();
        if (bodyRend != null && sh != null)
        {
            Material bodyMat = new Material(sh);
            Color bodyCol = new Color(0.92f, 0.93f, 0.95f, 1f);
            bodyMat.color = bodyCol;
            if (bodyMat.HasProperty("_BaseColor")) bodyMat.SetColor("_BaseColor", bodyCol);
            bodyRend.sharedMaterial = bodyMat;
        }

        // Lớp sơn màu bên trên miệng thùng hiển thị màu sơn tiếp theo
        GameObject paintTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        paintTop.name = "Mesh_PaintSwatch";
        Collider pCol = paintTop.GetComponent<Collider>();
        if (pCol != null) Destroy(pCol);
        paintTop.transform.SetParent(bucketRoot.transform, false);
        paintTop.transform.localPosition = new Vector3(0f, 0.315f, 0f);
        paintTop.transform.localScale = new Vector3(0.23f, 0.012f, 0.23f);

        WallPainter painter = bucketRoot.AddComponent<WallPainter>();
        painter.BindRoomRoot(transform, palette, pricePerWall);
    }

    public void BindRoomRoot(Transform roomRoot, List<Color> sourcePalette, float price)
    {
        isRoomRootDelegated = false;
        targetRoomRoot = roomRoot;
        if (sourcePalette != null && sourcePalette.Count > 0)
            palette = new List<Color>(sourcePalette);
        pricePerWall = price;

        Transform swatch = transform.Find("Mesh_PaintSwatch");
        if (swatch != null)
            swatchRenderer = swatch.GetComponent<Renderer>();

        CacheWallRenderers();
        UpdateSwatchPreview();
    }

    void CacheWallRenderers()
    {
        wallRenderers.Clear();
        Transform root = targetRoomRoot != null ? targetRoomRoot : transform;
        Renderer[] all = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;
            string n = r.gameObject.name;
            if (n.StartsWith("Wall") || n.Contains("Wall_"))
                wallRenderers.Add(r);
        }
    }

    void UpdateSwatchPreview()
    {
        if (swatchRenderer == null || palette == null || palette.Count == 0) return;
        Color nextCol = palette[Mathf.Clamp(nextColorIndex, 0, palette.Count - 1)];
        Material mat = swatchRenderer.material;
        if (mat != null)
        {
            mat.color = nextCol;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", nextCol);
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !isRoomRootDelegated;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isRoomRootDelegated) return;
        if (wallRenderers.Count == 0)
            CacheWallRenderers();

        Renderer primaryWall = wallRenderers.Count > 0 ? wallRenderers[0] : GetComponentInChildren<Renderer>();
        Paint(primaryWall, nextColorIndex);
        nextColorIndex = (nextColorIndex + 1) % Mathf.Max(1, palette.Count);
        UpdateSwatchPreview();
    }

    public void Paint(Renderer wall, int colorIndex)
    {
        if (palette == null || palette.Count == 0)
        {
            palette = new List<Color> { new Color(0.88f, 0.91f, 0.95f), new Color(0.82f, 0.92f, 0.86f) };
        }

        if (GameWorld.HasInstance && !GameWorld.Instance.Economy.TrySpend(pricePerWall))
        {
            AudioManager.PlayUi("purchase_fail");
            FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ $ {pricePerWall:0} để sơn tường", false);
            return;
        }

        Color chosen = palette[Mathf.Clamp(colorIndex, 0, palette.Count - 1)];
        if (wallRenderers.Count == 0)
            CacheWallRenderers();

        if (wallRenderers.Count > 0)
        {
            for (int i = 0; i < wallRenderers.Count; i++)
            {
                Renderer r = wallRenderers[i];
                if (r != null && r.material != null)
                {
                    r.material.color = chosen;
                    if (r.material.HasProperty("_BaseColor"))
                        r.material.SetColor("_BaseColor", chosen);
                }
            }
        }
        else if (wall != null && wall.material != null)
        {
            wall.material.color = chosen;
            if (wall.material.HasProperty("_BaseColor"))
                wall.material.SetColor("_BaseColor", chosen);
        }

        AudioManager.PlayAt("wall_paint", transform.position);
        AudioManager.Play("clean_shine");
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(4f);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã sơn màu tường mới cho phòng Streamer! (+4 Mood)", true);
    }
}
