using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Truyện tranh sưu tầm đặt trên kệ sách trong phòng
public class ComicBook : MonoBehaviour, IInteractable
{
    public static HashSet<string> CollectedIds { get; } = new HashSet<string>();

    [SerializeField] string comicId = "comic_vol_1";
    [SerializeField] GameObject comicVisual;

    public string ComicId => comicId;
    public bool IsCollected => CollectedIds.Contains(comicId);
    public string Prompt => $"Nhặt truyện tranh sưu tầm [{comicId}]";

    void Awake()
    {
        EnsureComicVisual();
        RefreshVisualState();
    }

    void OnEnable()
    {
        EnsureComicVisual();
        RefreshVisualState();
    }

    // Tạo mô hình cuốn truyện tranh nằm trên kệ nếu script đang gắn trực tiếp vào tủ sách
    void EnsureComicVisual()
    {
        if (comicVisual != null) return;

        Transform existing = transform.Find("ComicBookVisual");
        if (existing != null)
        {
            comicVisual = existing.gameObject;
            return;
        }

        bool isShelf = gameObject.name.IndexOf("Shelf", StringComparison.OrdinalIgnoreCase) >= 0
                       || transform.Find("Mesh_Board2") != null;
        if (!isShelf)
        {
            comicVisual = gameObject;
            return;
        }

        GameObject bookRoot = new GameObject("ComicBookVisual");
        bookRoot.transform.SetParent(transform, false);
        bookRoot.transform.localPosition = new Vector3(0.04f, 0.935f, 0.03f);
        bookRoot.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = "Cover";
        cover.transform.SetParent(bookRoot.transform, false);
        cover.transform.localPosition = Vector3.zero;
        cover.transform.localScale = new Vector3(0.19f, 0.028f, 0.25f);
        Collider coverCol = cover.GetComponent<Collider>();
        if (coverCol != null) Destroy(coverCol);
        MeshRenderer coverMr = cover.GetComponent<MeshRenderer>();
        if (coverMr != null && sh != null)
        {
            Material coverMat = new Material(sh) { color = new Color(0.86f, 0.20f, 0.16f, 1f) };
            coverMr.sharedMaterial = coverMat;
            coverMr.shadowCastingMode = ShadowCastingMode.Off;
        }

        GameObject pages = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pages.name = "Pages";
        pages.transform.SetParent(bookRoot.transform, false);
        pages.transform.localPosition = new Vector3(0.006f, 0.002f, 0f);
        pages.transform.localScale = new Vector3(0.178f, 0.022f, 0.238f);
        Collider pagesCol = pages.GetComponent<Collider>();
        if (pagesCol != null) Destroy(pagesCol);
        MeshRenderer pagesMr = pages.GetComponent<MeshRenderer>();
        if (pagesMr != null && sh != null)
        {
            Material pagesMat = new Material(sh) { color = new Color(0.95f, 0.93f, 0.86f, 1f) };
            pagesMr.sharedMaterial = pagesMat;
            pagesMr.shadowCastingMode = ShadowCastingMode.Off;
        }

        comicVisual = bookRoot;
    }

    void RefreshVisualState()
    {
        if (comicVisual != null && comicVisual != gameObject)
            comicVisual.SetActive(!IsCollected);
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsCollected;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsCollected) return;
        CollectedIds.Add(comicId);
        AudioManager.PlayAt("item_pickup", transform.position);
        AudioManager.PlayUi("ui_success");
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(5f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã sưu tầm truyện tranh [{comicId}] ({CollectedIds.Count} cuốn)! (+5 Mood)", true);

        EnsureComicVisual();
        if (comicVisual != null && comicVisual != gameObject)
            comicVisual.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
