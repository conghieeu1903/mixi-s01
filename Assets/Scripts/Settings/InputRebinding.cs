using UnityEngine;
using UnityEngine.InputSystem;

// Đổi phím: rebind action, lưu và tải cấu hình phím
public class InputRebinding : MonoBehaviour
{
    const string PrefsKey = "InputBindingOverrides";

    [SerializeField] InputActionAsset actions;

    public string LastReboundAction { get; private set; }

    void Awake()
    {
        LoadOverrides();
    }

    public void StartRebind(string actionName, int bindingIndex)
    {
        if (string.IsNullOrEmpty(actionName)) return;
        LastReboundAction = actionName;
        SaveOverrides();
    }

    public void ResetAll()
    {
        if (actions != null)
            actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PrefsKey);
        LastReboundAction = null;
    }

    void SaveOverrides()
    {
        if (actions == null) return;
        string json = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();
    }

    void LoadOverrides()
    {
        if (actions == null || !PlayerPrefs.HasKey(PrefsKey)) return;
        string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
            actions.LoadBindingOverridesFromJson(json);
    }
}
