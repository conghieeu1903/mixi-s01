using System.Collections.Generic;
using UnityEngine;

// Quản lý Object Pool cho các Reel hiển thị trong tầm nhìn camera
public class ReelPool
{
    [Tooltip("Ngăn xếp chứa các đối tượng Reel đang rảnh trong pool")]
    readonly Stack<Reel> inactive = new Stack<Reel>();

    [Tooltip("Ánh xạ từ chỉ số reel trong mảng sang đối tượng Reel đang hiển thị")]
    readonly Dictionary<int, Reel> activeByIndex = new Dictionary<int, Reel>();

    [Tooltip("Danh sách các Reel đang hiển thị trong khung nhìn camera")]
    readonly List<Reel> activeList = new List<Reel>();

    [Tooltip("Danh sách tạm chứa các chỉ số reel cần thu hồi về pool")]
    readonly List<int> removeKeys = new List<int>();

    public IReadOnlyList<Reel> ActiveReels => activeList;

    // Thu gom các Reel có sẵn dưới nút gốc đưa vào pool để tái sử dụng
    public void CollectExisting(Transform root)
    {
        activeByIndex.Clear();
        activeList.Clear();
        inactive.Clear();
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            if (!root.GetChild(i).TryGetComponent(out Reel reel)) continue;
            reel.SetHighlight(Color.clear, 0f);
            reel.gameObject.SetActive(false);
            inactive.Push(reel);
        }
    }

    // Thu hồi các Reel nằm ngoài phạm vi hàng cột đang nhìn thấy về lại pool
    public void CullOutside(int minRow, int maxRow, int minCol, int maxCol, int cols, int totalReels)
    {
        removeKeys.Clear();
        foreach (KeyValuePair<int, Reel> pair in activeByIndex)
        {
            int idx = pair.Key;
            Reel reel = pair.Value;
            if (reel == null)
            {
                removeKeys.Add(idx);
                continue;
            }
            int row = idx / cols;
            int col = idx % cols;
            if (idx < 0 || idx >= totalReels || row < minRow || row > maxRow || col < minCol || col > maxCol)
                removeKeys.Add(idx);
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            int key = removeKeys[i];
            if (activeByIndex.TryGetValue(key, out Reel reel) && reel != null)
            {
                reel.SetHighlight(Color.clear, 0f);
                reel.gameObject.SetActive(false);
                inactive.Push(reel);
            }
            activeByIndex.Remove(key);
        }
    }

    // Lấy Reel đang gắn với chỉ số index hoặc mượn một Reel rảnh từ pool
    public Reel GetOrAcquire(int index, Reel prefab, Transform root, int faceCount, SymbolData symbols)
    {
        if (activeByIndex.TryGetValue(index, out Reel existing) && existing != null)
        {
            existing.EnsureBuilt(faceCount, symbols);
            return existing;
        }

        Reel reel = null;
        while (inactive.Count > 0 && reel == null)
            reel = inactive.Pop();

        if (reel == null)
        {
            if (prefab == null || root == null) return null;
            reel = Object.Instantiate(prefab, root);
        }

        reel.gameObject.name = $"Reel {index}";
        if (!reel.gameObject.activeSelf) reel.gameObject.SetActive(true);
        reel.EnsureBuilt(faceCount, symbols);
        activeByIndex[index] = reel;
        return reel;
    }

    // Đồng bộ lại danh sách các Reel đang hoạt động theo thứ tự hiển thị
    public void RebuildActiveList()
    {
        activeList.Clear();
        foreach (KeyValuePair<int, Reel> pair in activeByIndex)
            if (pair.Value != null) activeList.Add(pair.Value);
    }

    // Tìm Reel đang hiển thị ứng với chỉ số index
    public bool TryGetActive(int index, out Reel reel) => activeByIndex.TryGetValue(index, out reel) && reel != null;

    // Trả toàn bộ Reel đang hiển thị về trạng thái rảnh trong pool
    public void ReleaseAll()
    {
        foreach (KeyValuePair<int, Reel> pair in activeByIndex)
        {
            Reel reel = pair.Value;
            if (reel == null) continue;
            reel.SetHighlight(Color.clear, 0f);
            reel.gameObject.SetActive(false);
            inactive.Push(reel);
        }
        activeByIndex.Clear();
        activeList.Clear();
    }
}
