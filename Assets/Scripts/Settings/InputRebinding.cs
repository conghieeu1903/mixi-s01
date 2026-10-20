using UnityEngine;
using UnityEngine.InputSystem;

// Đổi phím: rebind action, lưu và tải cấu hình phím
public class InputRebinding : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;

    public void StartRebind(string actionName, int bindingIndex)
    {
    }

    public void ResetAll()
    {
    }
}
