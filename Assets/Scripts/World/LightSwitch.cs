using System.Collections.Generic;
using UnityEngine;

// Công tắc đèn gắn tường & điều khiển đèn thông minh RGB trong phòng giống LightSwitch & Lamp của SLS2
public class LightSwitch : MonoBehaviour, IInteractable
{
    static readonly (string name, Color color, float intensity)[] LightModes =
    {
        ("Trắng Ấm (Mặc định)", new Color(1.00f, 0.95f, 0.86f, 1f), 1.35f),
        ("Xanh Cyber (Stream RGB)", new Color(0.24f, 0.82f, 1.00f, 1f), 1.45f),
        ("Tím Neon (Gaming RGB)", new Color(0.76f, 0.32f, 1.00f, 1f), 1.45f),
        ("Cam Hoàng Hôn (Chill)", new Color(1.00f, 0.56f, 0.24f, 1f), 1.35f),
        ("Xanh Ngọc Lục Bảo", new Color(0.24f, 0.94f, 0.56f, 1f), 1.35f),
        ("Tắt Đèn Phòng", Color.black, 0f)
    };

    [SerializeField] List<Light> roomLights = new List<Light>();
    [SerializeField] int currentModeIndex;

    public bool IsLightOn => currentModeIndex < LightModes.Length - 1;
    public int CurrentModeIndex => currentModeIndex;

    public string Prompt
    {
        get
        {
            int nextIdx = (currentModeIndex + 1) % LightModes.Length;
            return $"Công tắc đèn: Chuyển sang [{LightModes[nextIdx].name}]";
        }
    }

    void Awake()
    {
        EnsureRoomLights();
        ApplyCurrentMode();
    }

    void EnsureRoomLights()
    {
        roomLights.RemoveAll(l => l == null);
        if (roomLights.Count > 0) return;

        Light existing = GetComponentInChildren<Light>(true);
        if (existing != null)
            roomLights.Add(existing);

        if (roomLights.Count == 0)
        {
            GameObject ceilingLightObj = GameObject.Find("RoomCeilingSmartLight");
            if (ceilingLightObj == null)
            {
                ceilingLightObj = new GameObject("RoomCeilingSmartLight");
                ceilingLightObj.transform.position = new Vector3(0f, 2.75f, 0f);
            }
            Light l = ceilingLightObj.GetComponent<Light>();
            if (l == null)
                l = ceilingLightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8.5f;
            l.shadows = LightShadows.None;
            roomLights.Add(l);
        }
    }

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        currentModeIndex = (currentModeIndex + 1) % LightModes.Length;
        ApplyCurrentMode();
        AudioManager.PlayUi("mouse_click");

        var mode = LightModes[currentModeIndex];
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đèn phòng: {mode.name}", true);
    }

    public void ApplyCurrentMode()
    {
        EnsureRoomLights();
        var mode = LightModes[Mathf.Clamp(currentModeIndex, 0, LightModes.Length - 1)];
        bool enabled = mode.intensity > 0.01f;

        for (int i = 0; i < roomLights.Count; i++)
        {
            Light l = roomLights[i];
            if (l == null) continue;
            l.enabled = enabled;
            if (enabled)
            {
                l.color = mode.color;
                l.intensity = mode.intensity;
            }
        }

        Transform toggleKnob = transform.Find("SwitchKnob");
        if (toggleKnob != null)
        {
            toggleKnob.localRotation = Quaternion.Euler(enabled ? -18f : 18f, 0f, 0f);
            if (toggleKnob.TryGetComponent<MeshRenderer>(out MeshRenderer mr) && mr.sharedMaterial != null)
                mr.sharedMaterial.color = enabled ? mode.color : new Color(0.25f, 0.25f, 0.28f, 1f);
        }
    }
}
