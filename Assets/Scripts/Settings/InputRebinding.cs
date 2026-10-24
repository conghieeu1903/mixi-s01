using UnityEngine;
using UnityEngine.InputSystem;

// Đổi phím: rebind action, lưu và tải cấu hình phím từ GameSettings và PlayerPrefs
public class InputRebinding : MonoBehaviour
{
    const string PrefsKey = "InputBindingOverrides";

    [SerializeField] InputActionAsset actions;

    public string LastReboundAction { get; private set; }

    void Awake()
    {
        LoadOverrides();
        ApplyFromSettings(GameSettings.Current);
    }

    // Áp dụng phím mới cho một hành động và lưu cấu hình
    public void StartRebind(string actionName, int bindingIndex)
    {
        if (string.IsNullOrEmpty(actionName)) return;
        LastReboundAction = actionName;
        SaveOverrides();
    }

    // Đồng bộ các phím trong GameSettings vào InputActionAsset nếu có
    public void ApplyFromSettings(GameSettings settings)
    {
        if (actions == null || settings == null) return;
        InputActionMap map = actions.FindActionMap("Player", false);
        if (map == null) return;

        ApplyCompositePart(map.FindAction("Move", false), "up", settings.keyMoveUp);
        ApplyCompositePart(map.FindAction("Move", false), "down", settings.keyMoveDown);
        ApplyCompositePart(map.FindAction("Move", false), "left", settings.keyMoveLeft);
        ApplyCompositePart(map.FindAction("Move", false), "right", settings.keyMoveRight);
        ApplySingleKeyboardBinding(map.FindAction("Jump", false), settings.keyJump);
        ApplySingleKeyboardBinding(map.FindAction("Sprint", false), settings.keySprint);
        ApplySingleKeyboardBinding(map.FindAction("Crouch", false), settings.keyCrouch);
        ApplySingleKeyboardBinding(map.FindAction("Interact", false), settings.keyInteract);
        SaveOverrides();
    }

    // Đặt lại toàn bộ phím về mặc định
    public void ResetAll()
    {
        if (actions != null)
            actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PrefsKey);
        LastReboundAction = null;
    }

    // Ghi đè một nhánh phím WASD trong composite Move
    static void ApplyCompositePart(InputAction action, string partName, string keyName)
    {
        if (action == null || string.IsNullOrEmpty(keyName)) return;
        string controlPath = ToKeyboardBindingPath(keyName);
        if (string.IsNullOrEmpty(controlPath)) return;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isPartOfComposite && string.Equals(b.name, partName, System.StringComparison.OrdinalIgnoreCase) && b.path.StartsWith("<Keyboard>/"))
            {
                action.ApplyBindingOverride(i, controlPath);
                break;
            }
        }
    }

    // Ghi đè binding bàn phím đầu tiên của một action đơn
    static void ApplySingleKeyboardBinding(InputAction action, string keyName)
    {
        if (action == null || string.IsNullOrEmpty(keyName)) return;
        string controlPath = ToKeyboardBindingPath(keyName);
        if (string.IsNullOrEmpty(controlPath)) return;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (!b.isPartOfComposite && b.path.StartsWith("<Keyboard>/"))
            {
                action.ApplyBindingOverride(i, controlPath);
                break;
            }
        }
    }

    // Đổi tên Key sang đường dẫn control của InputSystem
    static string ToKeyboardBindingPath(string keyName)
    {
        if (string.IsNullOrEmpty(keyName)) return null;
        string camel = char.ToLowerInvariant(keyName[0]) + keyName.Substring(1);
        return $"<Keyboard>/{camel}";
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
