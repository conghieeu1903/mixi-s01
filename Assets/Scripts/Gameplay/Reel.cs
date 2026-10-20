using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Hiển thị một khối reel 3D dạng lăng trụ đều lấy từ pool và đồng bộ theo dữ liệu ReelState
[ExecuteAlways]
public class Reel : MonoBehaviour
{
    public const int MinFaces = 3;
    public const int MaxFaces = 24;

    static readonly int HighlightId = Shader.PropertyToID("_Highlight");
    static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");

    [Tooltip("Transform khối lăng trụ xoay quanh trục X")]
    [SerializeField] Transform drum;

    [Tooltip("Chiều cao của mỗi mặt phẳng trên lăng trụ")]
    [SerializeField] float faceHeight = 1f;

    [Tooltip("Bề rộng ngang của khối lăng trụ reel")]
    [SerializeField] float width = 1f;

    [Tooltip("Tỷ lệ kích thước icon biểu tượng so với diện tích mặt lăng trụ")]
    [SerializeField, Range(0.1f, 1f)] float iconFill = 0.8f;

    [Tooltip("Thời gian giảm tốc (giây) để dừng hẳn vào mặt đích")]
    [SerializeField] float stopDuration = 0.4f;

    [Tooltip("Thời gian tăng tốc (giây) khi bắt đầu quay")]
    [SerializeField] float spinUpDuration = 0.3f;

    [Tooltip("Độ lệch ngẫu nhiên của tốc độ quay khi chạy độc lập")]
    [SerializeField, Range(0f, 0.5f)] float speedJitter = 0.15f;

    [Tooltip("Số mặt đa giác hiện tại của khối reel")]
    [SerializeField, HideInInspector] int faceCount = 7;

    [Tooltip("Dữ liệu biểu tượng đang gắn trên khối reel")]
    [SerializeField, HideInInspector] SymbolData symbols;

    [Tooltip("Danh sách các SpriteRenderer hiển thị biểu tượng trên từng mặt")]
    [SerializeField, HideInInspector] List<SpriteRenderer> icons = new List<SpriteRenderer>();

    public event Action<Reel> Stopped;

    public int Index { get; private set; }
    public bool IsSpinning { get; private set; }
    public int StoppedSymbol { get; private set; } = -1;
    public int FaceCount => faceCount;
    public float StopDuration => stopDuration;
    public float SpinUpDuration => spinUpDuration;
    public float SpeedJitter => speedJitter;
    public float WorldWidth => width * transform.lossyScale.x;
    public Vector3 TopWorld => drum != null
        ? drum.parent.TransformPoint(drum.localPosition + Vector3.up * faceHeight * 0.5f / Mathf.Sin(Mathf.PI / faceCount))
        : transform.position;

    int[] layout;
    float speed;
    float position;
    float spinElapsed;
    bool builtOnce;
    Mesh mesh;
    MaterialPropertyBlock block;

    float DegreesPerFace => 360f / faceCount;

    // Chỉ dựng lại lưới và icon nếu cấu hình số mặt hoặc dữ liệu biểu tượng thay đổi
    public void EnsureBuilt(int faces, SymbolData symbolData)
    {
        int clamped = Mathf.Clamp(faces, MinFaces, MaxFaces);
        if (builtOnce && faceCount == clamped && symbols == symbolData && mesh != null) return;
        Build(clamped, symbolData);
    }

    // Dựng lại lưới lăng trụ và pool các icon theo số mặt
    public void Build(int faces, SymbolData symbolData)
    {
        faceCount = Mathf.Clamp(faces, MinFaces, MaxFaces);
        symbols = symbolData;
        builtOnce = true;
        BuildMesh();
        BuildIcons();
        ApplyRotation();
    }

    // Đồng bộ trực tiếp trạng thái từ phần tử mảng ReelState sang đối tượng hiển thị
    public void BindState(int index, in ReelState state, int[] faceLayout)
    {
        bool layoutChanged = layout != faceLayout;
        Index = index;
        layout = faceLayout;
        IsSpinning = state.IsSpinning;
        StoppedSymbol = state.stoppedSymbol;
        position = state.position;
        if (layoutChanged) RefreshIcons();
        ApplyRotation();
    }

    // Cài đặt thông số cho lượt quay độc lập và làm mới icon
    public void Setup(int index, int[] faceLayout, float facesPerSecond)
    {
        Index = index;
        layout = faceLayout;
        speed = facesPerSecond * UnityEngine.Random.Range(1f - speedJitter, 1f + speedJitter);
        StoppedSymbol = -1;
        RefreshIcons();
    }

    // Kích hoạt trạng thái quay độc lập
    public void StartSpin()
    {
        IsSpinning = true;
        StoppedSymbol = -1;
        spinElapsed = 0f;
    }

    // Dừng tại biểu tượng chỉ định khi chạy độc lập
    public void StopOn(int symbolId)
    {
        if (!IsSpinning) return;
        StartCoroutine(StopRoutine(symbolId));
    }

    // Tô sáng khối reel qua MaterialPropertyBlock mà không nhân bản material
    public void SetHighlight(Color color, float amount)
    {
        if (drum == null || !drum.TryGetComponent(out MeshRenderer drumRenderer)) return;
        block ??= new MaterialPropertyBlock();
        drumRenderer.GetPropertyBlock(block);
        block.SetColor(HighlightColorId, color);
        block.SetFloat(HighlightId, amount);
        drumRenderer.SetPropertyBlock(block);
    }

    // Khôi phục lưới khi đối tượng được bật lại từ pool
    void OnEnable()
    {
        if (mesh == null) BuildMesh();
        ApplyRotation();
    }

    // Giải phóng lưới tạm khi hủy đối tượng
    void OnDestroy()
    {
        if (mesh != null) Dispose(mesh);
        mesh = null;
    }

    // Giảm tốc mượt và dừng đúng mặt chứa biểu tượng khi chạy độc lập
    IEnumerator StopRoutine(int symbolId)
    {
        IsSpinning = false;
        int face = layout != null ? Mathf.Max(0, Array.IndexOf(layout, symbolId)) : 0;
        float from = position;
        float to = Mathf.Ceil(from) + Mod(face - Mathf.Ceil(from), faceCount);
        for (float t = 0f; t < stopDuration; t += Time.deltaTime)
        {
            position = Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - t / stopDuration, 3f));
            ApplyRotation();
            yield return null;
        }
        position = to % faceCount;
        ApplyRotation();
        StoppedSymbol = symbolId;
        Stopped?.Invoke(this);
    }

    // Sinh lưới lăng trụ đều gồm các mặt bên và hai nắp
    void BuildMesh()
    {
        if (drum == null || !drum.TryGetComponent(out MeshFilter filter)) return;
        if (mesh == null) mesh = new Mesh { name = "ReelDrum", hideFlags = HideFlags.DontSave };
        mesh.Clear();

        float radius = faceHeight * 0.5f / Mathf.Sin(Mathf.PI / faceCount);
        Vector3 half = Vector3.right * width * 0.5f;
        var verts = new List<Vector3>(faceCount * 6 + 2);
        var normals = new List<Vector3>(faceCount * 6 + 2);
        var uvs = new List<Vector2>(faceCount * 6 + 2);
        var tris = new List<int>(faceCount * 12);

        for (int i = 0; i < faceCount; i++)
        {
            Vector3 top = Corner(i) * radius, bottom = Corner(i + 1) * radius, normal = FaceNormal(i);
            int v = verts.Count;
            verts.Add(top - half);
            verts.Add(top + half);
            verts.Add(bottom - half);
            verts.Add(bottom + half);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            tris.Add(v + 2);
            tris.Add(v);
            tris.Add(v + 1);
            tris.Add(v + 2);
            tris.Add(v + 1);
            tris.Add(v + 3);
        }
        AddCap(half, radius, true, verts, normals, uvs, tris);
        AddCap(-half, radius, false, verts, normals, uvs, tris);

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
    }

    // Thêm một nắp đa giác ở đầu lăng trụ
    void AddCap(Vector3 offset, float radius, bool right, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris)
    {
        int center = verts.Count;
        Vector3 normal = right ? Vector3.right : Vector3.left;
        verts.Add(offset);
        normals.Add(normal);
        uvs.Add(new Vector2(0.5f, 0.5f));
        for (int k = 0; k < faceCount; k++)
        {
            Vector3 p = Corner(k) * radius;
            verts.Add(p + offset);
            normals.Add(normal);
            uvs.Add(new Vector2(0.5f + p.z / (2f * radius), 0.5f + p.y / (2f * radius)));
        }
        for (int k = 0; k < faceCount; k++)
        {
            int a = center + 1 + k, b = center + 1 + (k + 1) % faceCount;
            if (right)
            {
                tris.Add(center);
                tris.Add(b);
                tris.Add(a);
            }
            else
            {
                tris.Add(center);
                tris.Add(a);
                tris.Add(b);
            }
        }
    }

    // Tái sử dụng hoặc tạo thêm các đối tượng SpriteRenderer cho từng mặt
    void BuildIcons()
    {
        if (drum == null) return;
        icons.Clear();
        for (int i = 0; i < drum.childCount; i++)
            if (drum.GetChild(i).TryGetComponent(out SpriteRenderer sr)) icons.Add(sr);

        while (icons.Count < faceCount)
        {
            var icon = new GameObject($"Icon {icons.Count}").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(drum, false);
            icons.Add(icon);
        }

        float apothem = faceHeight * 0.5f / Mathf.Tan(Mathf.PI / faceCount);
        for (int i = 0; i < icons.Count; i++)
        {
            SpriteRenderer icon = icons[i];
            if (icon == null) continue;
            bool active = i < faceCount;
            if (icon.gameObject.activeSelf != active) icon.gameObject.SetActive(active);
            if (!active) continue;
            icon.transform.localPosition = FaceNormal(i) * (apothem + 0.005f);
            icon.transform.localRotation = Quaternion.Euler(-i * DegreesPerFace, 0f, 0f);
        }
        RefreshIcons();
    }

    // Gán sprite và co giãn icon vừa mặt theo bố cục hiện tại
    void RefreshIcons()
    {
        float size = Mathf.Min(faceHeight, width) * iconFill;
        int count = Mathf.Min(faceCount, icons.Count);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer icon = icons[i];
            if (icon == null) continue;
            Sprite sprite = symbols != null ? symbols.Get(SymbolAt(i)).icon : null;
            icon.sprite = sprite;
            float extent = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            icon.transform.localScale = Vector3.one * (size / extent);
        }
    }

    // Lấy mã biểu tượng hiển thị trên một mặt
    int SymbolAt(int face) => layout != null && layout.Length > 0
        ? layout[face % layout.Length]
        : (face == faceCount - 1 ? SymbolData.SevenId : face % SymbolData.SevenId);

    // Hướng tới góc thứ k của đa giác tiết diện
    Vector3 Corner(int k) => Quaternion.Euler(-(k - 0.5f) * DegreesPerFace, 0f, 0f) * Vector3.back;

    // Pháp tuyến của mặt thứ i
    Vector3 FaceNormal(int i) => Quaternion.Euler(-i * DegreesPerFace, 0f, 0f) * Vector3.back;

    // Gán góc xoay cục bộ cho trục quay
    void ApplyRotation()
    {
        if (drum != null) drum.localRotation = Quaternion.Euler(position * DegreesPerFace, 0f, 0f);
    }

    // Hủy đối tượng an toàn cả trong Edit mode lẫn Play mode
    static void Dispose(Object target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    // Chia lấy dư luôn dương
    static float Mod(float a, int m) => (a % m + m) % m;
}
