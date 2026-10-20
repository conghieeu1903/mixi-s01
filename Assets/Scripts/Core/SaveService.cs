using UnityEngine;

// Lưu và tải dữ liệu game bằng Easy Save 3
public static class SaveService
{
    const string FileName = "world_save.es3";
    const string Key = "world";

    // Có file save hay chưa
    public static bool HasSave() => ES3.FileExists(FileName) && ES3.KeyExists(Key, FileName);

    // Ghi dữ liệu xuống file save
    public static void Save(SaveData data)
    {
        ES3.Save(Key, data, FileName);
    }

    // Đọc dữ liệu từ file save, trả về null nếu chưa có hoặc file lỗi
    public static SaveData Load()
    {
        if (!HasSave()) return null;
        try
        {
            return ES3.Load<SaveData>(Key, FileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveService] Load failed: {e.Message}");
            return null;
        }
    }

    // Xóa file save
    public static void Delete()
    {
        if (ES3.FileExists(FileName)) ES3.DeleteFile(FileName);
    }
}
