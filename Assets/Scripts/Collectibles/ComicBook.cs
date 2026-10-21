using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Cuốn truyện tranh 3D độc lập đặt trên kệ: chỉ khi nhìn trúng cuốn sách mới hiện gợi ý nhặt
public class ComicBook : MonoBehaviour, IInteractable
{
    public static HashSet<string> CollectedIds { get; } = new HashSet<string>();

    [SerializeField] string comicId = "comic_vol_1";

    public string ComicId => comicId;
    public bool IsCollected => CollectedIds.Contains(comicId);
    public string Prompt => $"Nhặt truyện tranh sưu tầm [{comicId}]";

    void Awake()
    {
        if (IsAttachedToShelfRoot())
        {
            MigrateFromShelfToBookObject();
            return;
        }

        EnsureBookMeshAndCollider();
        if (IsCollected)
            gameObject.SetActive(false);
    }

    // Kiểm tra xem script có đang bị gắn nhầm lên cả cái tủ sách hay không
    bool IsAttachedToShelfRoot()
    {
        return gameObject.name.IndexOf("Shelf", StringComparison.OrdinalIgnoreCase) >= 0
               || transform.Find("Mesh_Board2") != null;
    }

    // Tách script khỏi tủ sách sang một object cuốn truyện tranh 3D riêng biệt nằm trên ngăn kệ
    public ComicBook MigrateFromShelfToBookObject()
    {
        MakeShelfHollowForRaycast(transform);

        Transform existing = transform.Find("ComicBook_Vol_1");
        GameObject bookObj;
        if (existing != null)
        {
            bookObj = existing.gameObject;
        }
        else
        {
            bookObj = new GameObject("ComicBook_Vol_1");
            bookObj.transform.SetParent(transform, false);
            bookObj.transform.localPosition = new Vector3(0.04f, 0.935f, 0.04f);
            bookObj.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
        }

        ComicBook bookComp = bookObj.GetComponent<ComicBook>();
        if (bookComp == null)
            bookComp = bookObj.AddComponent<ComicBook>();
        bookComp.comicId = comicId;
        bookComp.EnsureBookMeshAndCollider();
        bookObj.SetActive(!CollectedIds.Contains(comicId));

        if (Application.isPlaying)
            Destroy(this);

        return bookComp;
    }

    // Thay BoxCollider đặc phủ kín tủ bằng collider riêng trên từng tấm ván để tia nhìn xuyên vào vật đặt trên kệ
    public static void MakeShelfHollowForRaycast(Transform shelfTransform)
    {
        if (shelfTransform == null) return;

        BoxCollider rootCol = shelfTransform.GetComponent<BoxCollider>();
        if (rootCol != null)
            rootCol.enabled = false;

        for (int i = 0; i < shelfTransform.childCount; i++)
        {
            Transform ch = shelfTransform.GetChild(i);
            if (!ch.name.StartsWith("Mesh_", StringComparison.OrdinalIgnoreCase)) continue;
            if (ch.GetComponent<Collider>() == null)
                ch.gameObject.AddComponent<BoxCollider>();
        }
    }

    // Tạo mô hình 3D cuốn truyện tranh và BoxCollider vừa khít kích thước cuốn sách
    public void EnsureBookMeshAndCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(0.22f, 0.045f, 0.27f);
        box.isTrigger = false;

        if (transform.Find("Cover") != null) return;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = "Cover";
        cover.transform.SetParent(transform, false);
        cover.transform.localPosition = Vector3.zero;
        cover.transform.localScale = new Vector3(0.20f, 0.032f, 0.26f);
        Collider coverCol = cover.GetComponent<Collider>();
        if (coverCol != null)
        {
            if (Application.isPlaying) Destroy(coverCol);
            else DestroyImmediate(coverCol);
        }
        MeshRenderer coverMr = cover.GetComponent<MeshRenderer>();
        if (coverMr != null && sh != null)
        {
            Material coverMat = new Material(sh) { color = new Color(0.86f, 0.18f, 0.15f, 1f) };
            coverMr.sharedMaterial = coverMat;
            coverMr.shadowCastingMode = ShadowCastingMode.Off;
        }

        GameObject pages = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pages.name = "Pages";
        pages.transform.SetParent(transform, false);
        pages.transform.localPosition = new Vector3(0.008f, 0.001f, 0f);
        pages.transform.localScale = new Vector3(0.188f, 0.026f, 0.246f);
        Collider pagesCol = pages.GetComponent<Collider>();
        if (pagesCol != null)
        {
            if (Application.isPlaying) Destroy(pagesCol);
            else DestroyImmediate(pagesCol);
        }
        MeshRenderer pagesMr = pages.GetComponent<MeshRenderer>();
        if (pagesMr != null && sh != null)
        {
            Material pagesMat = new Material(sh) { color = new Color(0.96f, 0.94f, 0.86f, 1f) };
            pagesMr.sharedMaterial = pagesMat;
            pagesMr.shadowCastingMode = ShadowCastingMode.Off;
        }

        GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        badge.name = "CoverArt";
        badge.transform.SetParent(transform, false);
        badge.transform.localPosition = new Vector3(-0.01f, 0.017f, 0.02f);
        badge.transform.localScale = new Vector3(0.12f, 0.004f, 0.14f);
        Collider badgeCol = badge.GetComponent<Collider>();
        if (badgeCol != null)
        {
            if (Application.isPlaying) Destroy(badgeCol);
            else DestroyImmediate(badgeCol);
        }
        MeshRenderer badgeMr = badge.GetComponent<MeshRenderer>();
        if (badgeMr != null && sh != null)
        {
            Material badgeMat = new Material(sh) { color = new Color(0.98f, 0.82f, 0.18f, 1f) };
            badgeMr.sharedMaterial = badgeMat;
            badgeMr.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsCollected && !IsAttachedToShelfRoot();
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsCollected || IsAttachedToShelfRoot()) return;
        CollectedIds.Add(comicId);
        AudioManager.PlayAt("item_pickup", transform.position);
        AudioManager.PlayUi("ui_success");
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(5f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã sưu tầm truyện tranh [{comicId}] ({CollectedIds.Count} cuốn)! (+5 Mood)", true);
        gameObject.SetActive(false);
    }
}
