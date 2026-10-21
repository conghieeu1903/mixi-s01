using System.Collections;
using UnityEngine;

// Vết bẩn trên tường và sàn nhà: hiển thị vết bẩn loang lổ và mờ dần kèm hiệu ứng lấp lánh khi dùng chổi quét
public class Dirt : MonoBehaviour, IInteractable
{
    [Range(0f, 1f)] [SerializeField] float amount = 1f;

    static Texture2D sharedSplatterTex;
    static Texture2D sharedSparkleTex;
    static Material sharedSparkleMat;
    static Mesh sharedFlatDecalMesh;

    Material instanceMat;
    Vector3 baseScale = new Vector3(1.25f, 0.005f, 1.25f);
    bool initializedVisuals;
    float lastSparkleTime;

    public float Amount => amount;

    public string Prompt
    {
        get
        {
            Mop mop = FindFirstObjectByType<Mop>();
            if (mop != null && mop.IsHeld)
                return $"Giữ Chuột trái để quét ({Mathf.RoundToInt(amount * 100f)}%)";
            return "Cần cầm cây lau nhà để quét vết bẩn";
        }
    }

    void Awake()
    {
        if (transform.localScale.x > 0.1f)
            baseScale = new Vector3(
                Mathf.Max(1.1f, transform.localScale.x),
                0.005f,
                Mathf.Max(1.1f, transform.localScale.z));
        EnsureVisuals();
        UpdateVisualState();
    }

    void OnEnable()
    {
        EnsureVisuals();
        UpdateVisualState();
    }

    void OnDestroy()
    {
        if (instanceMat != null)
            Destroy(instanceMat);
    }

    public void ConfigurePose(Vector3 localPos, Quaternion localRot, Vector3 scale, float initialAmount)
    {
        transform.localPosition = localPos;
        transform.localRotation = localRot;
        baseScale = scale;
        amount = Mathf.Clamp01(initialAmount);
        EnsureVisuals();
        gameObject.SetActive(amount > 0.01f);
        UpdateVisualState();
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Mop mop = FindFirstObjectByType<Mop>();
        if (mop != null && mop.IsHeld)
        {
            Scrub(0.35f, transform.position);
            return;
        }

        FindFirstObjectByType<HudView>()?.ShowInfo("Hãy lại góc phòng cầm cây lau nhà [E] rồi giữ Chuột trái để quét!", false);
    }

    // Quét làm sạch dần vết bẩn tại vị trí đầu chổi và tạo hạt lấp lánh
    public void Scrub(float power, Vector3 hitWorldPos)
    {
        if (power <= 0f || amount <= 0f) return;

        if (Time.time - lastSparkleTime > 0.09f)
        {
            lastSparkleTime = Time.time;
            SpawnSparkleBurst(hitWorldPos, 2);
        }

        Clean(power);
    }

    // Lau bớt vết bẩn
    public void Clean(float power)
    {
        if (power <= 0f || amount <= 0f) return;
        amount = Mathf.Clamp01(amount - power);

        if (amount <= 0.02f)
        {
            amount = 0f;
            AudioManager.PlayAt("clean_shine", transform.position);
            SpawnSparkleBurst(transform.position, 4);
            if (GameWorld.HasInstance)
                GameWorld.Instance.Mood.Change(1.5f);
            gameObject.SetActive(false);
        }
        else
        {
            UpdateVisualState();
        }

        CleaningSystem sys = FindFirstObjectByType<CleaningSystem>();
        if (sys != null)
            sys.RefreshCleanliness();
    }

    public void RestoreDirty(float newAmount = 1f)
    {
        amount = Mathf.Clamp01(newAmount);
        gameObject.SetActive(amount > 0.01f);
        EnsureVisuals();
        UpdateVisualState();
    }

    void EnsureVisuals()
    {
        EnsureTriggerCollider();
        if (initializedVisuals && instanceMat != null) return;
        initializedVisuals = true;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
            mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = GetOrCreateFlatDecalMesh();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
            mr = gameObject.AddComponent<MeshRenderer>();

        Texture2D tex = GetOrCreateSplatterTexture();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        instanceMat = new Material(shader);
        instanceMat.color = Color.white;
        if (instanceMat.HasProperty("_BaseMap")) instanceMat.SetTexture("_BaseMap", tex);
        if (instanceMat.HasProperty("_MainTex")) instanceMat.SetTexture("_MainTex", tex);
        if (instanceMat.HasProperty("_BaseColor")) instanceMat.SetColor("_BaseColor", Color.white);
        if (instanceMat.HasProperty("_Smoothness")) instanceMat.SetFloat("_Smoothness", 0.12f);
        if (instanceMat.HasProperty("_AlphaClip"))
        {
            instanceMat.SetFloat("_AlphaClip", 1f);
            instanceMat.EnableKeyword("_ALPHATEST_ON");
        }
        if (instanceMat.HasProperty("_Cutoff"))
            instanceMat.SetFloat("_Cutoff", 0.22f);

        mr.sharedMaterial = instanceMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Thay thế CapsuleCollider bằng BoxCollider dạng Trigger để vết bẩn không bao giờ cản bước di chuyển của người chơi
    void EnsureTriggerCollider()
    {
        Collider[] cols = GetComponents<Collider>();
        BoxCollider box = null;
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] is BoxCollider existingBox && box == null)
            {
                box = existingBox;
            }
            else if (cols[i] != null)
            {
                cols[i].enabled = false;
                if (Application.isPlaying)
                    Destroy(cols[i]);
            }
        }

        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = new Vector3(1f, 2f, 1f);
    }

    void UpdateVisualState()
    {
        float scaleRatio = Mathf.Lerp(0.42f, 1f, Mathf.Clamp01(amount));
        transform.localScale = new Vector3(baseScale.x * scaleRatio, baseScale.y, baseScale.z * scaleRatio);

        if (instanceMat != null && instanceMat.HasProperty("_Cutoff"))
        {
            float cutoff = Mathf.Lerp(0.86f, 0.22f, Mathf.Clamp01(amount));
            instanceMat.SetFloat("_Cutoff", cutoff);
        }
    }

    // Sinh hiệu ứng ngôi sao 4 cánh trắng lấp lánh như trong Streamer Life Simulator
    public static void SpawnSparkleBurst(Vector3 centerPos, int count)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        EnsureSparkleMaterial();
        for (int i = 0; i < count; i++)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CleanSparkleVFX";
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = sharedSparkleMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            Vector3 forward = cam.transform.forward;
            Vector2 rnd = Random.insideUnitCircle * 0.22f;
            Vector3 spawnPos = centerPos + right * rnd.x + up * (rnd.y - 0.04f) - forward * 0.08f;

            quad.transform.position = spawnPos;
            float size = Random.Range(0.11f, 0.22f);
            quad.transform.localScale = new Vector3(size, size, size);
            quad.transform.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));

            SparkleLifetime vfx = quad.AddComponent<SparkleLifetime>();
            vfx.Begin(size);
        }
    }

    static Mesh GetOrCreateFlatDecalMesh()
    {
        if (sharedFlatDecalMesh != null) return sharedFlatDecalMesh;
        sharedFlatDecalMesh = new Mesh { name = "DirtFlatDecalMesh" };
        sharedFlatDecalMesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f)
        };
        sharedFlatDecalMesh.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f)
        };
        sharedFlatDecalMesh.normals = new[]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            Vector3.down, Vector3.down, Vector3.down, Vector3.down
        };
        sharedFlatDecalMesh.triangles = new[]
        {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6
        };
        sharedFlatDecalMesh.RecalculateBounds();
        return sharedFlatDecalMesh;
    }

    static Texture2D GetOrCreateSplatterTexture()
    {
        if (sharedSplatterTex != null) return sharedSplatterTex;

        const int size = 128;
        sharedSplatterTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        sharedSplatterTex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            float ny = (y / (float)(size - 1)) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)(size - 1)) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                float n1 = Mathf.PerlinNoise(nx * 2.6f + 3.1f, ny * 2.6f + 5.7f);
                float n2 = Mathf.PerlinNoise(nx * 6.2f + 11.4f, ny * 6.2f + 8.3f);
                float n3 = Mathf.PerlinNoise(nx * 14f + 1.9f, ny * 14f + 19.2f);
                float blotch = n1 * 0.55f + n2 * 0.30f + n3 * 0.15f;

                float radial = Mathf.Clamp01(1.08f - dist * 0.98f);
                float alpha = Mathf.Clamp01((radial * 0.85f + blotch * 0.65f) - 0.32f);
                if (dist > 0.95f) alpha = 0f;

                Color darkMud = new Color(0.09f, 0.04f, 0.025f, alpha);
                Color midMud = new Color(0.20f, 0.09f, 0.045f, alpha);
                Color col = Color.Lerp(darkMud, midMud, n2);
                col.a = alpha;
                sharedSplatterTex.SetPixel(x, y, col);
            }
        }

        sharedSplatterTex.Apply();
        return sharedSplatterTex;
    }

    static void EnsureSparkleMaterial()
    {
        if (sharedSparkleMat != null && sharedSparkleTex != null) return;

        const int size = 64;
        sharedSparkleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        sharedSparkleTex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            float v = (y / (float)(size - 1)) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)(size - 1)) * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);

                float hRay = Mathf.Exp(-Mathf.Abs(v) * 15f) * Mathf.Clamp01(1f - Mathf.Abs(u));
                float vRay = Mathf.Exp(-Mathf.Abs(u) * 15f) * Mathf.Clamp01(1f - Mathf.Abs(v));
                float dRay1 = Mathf.Exp(-Mathf.Abs(u - v) * 22f) * Mathf.Clamp01(0.7f - d) * 0.5f;
                float dRay2 = Mathf.Exp(-Mathf.Abs(u + v) * 22f) * Mathf.Clamp01(0.7f - d) * 0.5f;
                float core = Mathf.Exp(-d * d * 22f);

                float a = Mathf.Clamp01(hRay + vRay + dRay1 + dRay2 + core);
                if (d > 0.96f) a = 0f;
                sharedSparkleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        sharedSparkleTex.Apply();

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
        sharedSparkleMat = new Material(sh);
        sharedSparkleMat.color = Color.white;
        if (sharedSparkleMat.HasProperty("_BaseMap")) sharedSparkleMat.SetTexture("_BaseMap", sharedSparkleTex);
        if (sharedSparkleMat.HasProperty("_MainTex")) sharedSparkleMat.SetTexture("_MainTex", sharedSparkleTex);
        if (sharedSparkleMat.HasProperty("_BaseColor")) sharedSparkleMat.SetColor("_BaseColor", Color.white);
        if (sharedSparkleMat.HasProperty("_AlphaClip"))
        {
            sharedSparkleMat.SetFloat("_AlphaClip", 1f);
            sharedSparkleMat.EnableKeyword("_ALPHATEST_ON");
        }
        if (sharedSparkleMat.HasProperty("_Cutoff"))
            sharedSparkleMat.SetFloat("_Cutoff", 0.16f);
    }
}

// Tự động co giãn và hủy hạt lấp lánh sau một khoảng thời gian ngắn
public class SparkleLifetime : MonoBehaviour
{
    float maxScale = 0.16f;
    float elapsed;
    const float Duration = 0.28f;

    public void Begin(float scale)
    {
        maxScale = scale;
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Duration);
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

        float pulse = Mathf.Sin((0.28f + t * 0.72f) * Mathf.PI);
        float s = maxScale * Mathf.Max(0.08f, pulse);
        transform.localScale = new Vector3(s, s, s);

        if (t >= 1f)
            Destroy(gameObject);
    }
}
