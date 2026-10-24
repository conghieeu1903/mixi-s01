using UnityEngine;

// Chuông cửa có màn hình Camera quan sát ngoài hiên nhà và nút mở khóa cửa từ xa giống DoorIntercom của SLS2
public class DoorIntercom : MonoBehaviour, IInteractable
{
    Camera outdoorCam;
    RenderTexture intercomRt;
    Material screenOffMat;
    Material screenOnMat;
    MeshRenderer screenRenderer;
    float autoOffTimer;

    public bool IsScreenActive { get; private set; }

    public string Prompt
    {
        get
        {
            DoorCharactersManager mgr = FindFirstObjectByType<DoorCharactersManager>();
            string visitorInfo = mgr != null && mgr.HasWaitingVisitor ? $" (Khách: {mgr.CurrentVisitorTitle})" : string.Empty;
            return IsScreenActive
                ? (mgr != null && mgr.HasWaitingVisitor
                    ? $"Intercom Cửa: Mở khóa cửa Buzzer & Tắt màn hình{visitorInfo}"
                    : "Intercom Cửa: Tắt màn hình")
                : $"Intercom Cửa: Bật Camera ngoài hiên{visitorInfo}";
        }
    }

    void Awake()
    {
        SetupIntercomCamera();
        SetScreenActive(false);
        autoOffTimer = 0f;
    }

    void OnDestroy()
    {
        if (intercomRt != null)
        {
            intercomRt.Release();
            Destroy(intercomRt);
        }
        if (screenOffMat != null) Destroy(screenOffMat);
        if (screenOnMat != null) Destroy(screenOnMat);
    }

    void SetupIntercomCamera()
    {
        Transform screenTr = transform.Find("IntercomScreen");
        if (screenTr != null)
            screenRenderer = screenTr.GetComponent<MeshRenderer>();

        if (intercomRt == null)
        {
            intercomRt = new RenderTexture(256, 192, 16, RenderTextureFormat.ARGB32)
            {
                name = "DoorIntercom_RT"
            };
            intercomRt.Create();
        }

        GameObject camObj = GameObject.Find("IntercomOutdoorCamera");
        if (camObj == null)
        {
            camObj = new GameObject("IntercomOutdoorCamera");
            Transform env = transform.parent;
            if (env != null)
                camObj.transform.SetParent(env, false);
        }
        camObj.transform.localPosition = new Vector3(-0.35f, 1.72f, 3.02f);
        camObj.transform.localRotation = Quaternion.Euler(8f, 12f, 0f);

        outdoorCam = camObj.GetComponent<Camera>();
        if (outdoorCam == null)
            outdoorCam = camObj.AddComponent<Camera>();

        outdoorCam.targetTexture = intercomRt;
        outdoorCam.fieldOfView = 62f;
        outdoorCam.nearClipPlane = 0.08f;
        outdoorCam.farClipPlane = 18f;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
        screenOnMat = new Material(sh) { color = Color.white };
        if (screenOnMat.HasProperty("_BaseMap")) screenOnMat.SetTexture("_BaseMap", intercomRt);
        if (screenOnMat.HasProperty("_MainTex")) screenOnMat.SetTexture("_MainTex", intercomRt);

        screenOffMat = new Material(sh) { color = new Color(0.06f, 0.08f, 0.10f, 1f) };
    }

    void Update()
    {
        if (!IsScreenActive) return;
        if (autoOffTimer > 0f)
        {
            autoOffTimer -= Time.deltaTime;
            if (autoOffTimer <= 0f)
                SetScreenActive(false);
        }
    }

    // Đổ chuông cửa và tự động bật màn hình Camera ngoài hiên trong 20 giây giống DoorIntercom.DoorRing của SLS2
    public void DoorRing(string visitorName = null)
    {
        AudioManager.PlayUi("ui_notification");
        SetScreenActive(true);
        autoOffTimer = 20f;

        string msg = string.IsNullOrEmpty(visitorName)
            ? "🔔 Chuông cửa reo! Có khách đang đứng trước cửa nhà."
            : $"🔔 Chuông cửa reo! [{visitorName}] đang đứng chờ ngoài cửa nhà.";
        FindFirstObjectByType<HudView>()?.ShowInfo(msg, true);
    }

    public void SetScreenActive(bool active)
    {
        IsScreenActive = active;
        if (outdoorCam != null)
            outdoorCam.enabled = active;
        if (screenRenderer != null)
            screenRenderer.sharedMaterial = active ? screenOnMat : screenOffMat;
    }

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        AudioManager.PlayUi("mouse_click");
        Door door = FindFirstObjectByType<Door>();

        if (!IsScreenActive)
        {
            SetScreenActive(true);
            autoOffTimer = 20f;
            FindFirstObjectByType<HudView>()?.ShowInfo("Đã bật màn hình Camera Intercom.", true);
        }
        else
        {
            DoorCharactersManager mgr = FindFirstObjectByType<DoorCharactersManager>();
            bool hasVisitor = mgr != null && mgr.HasWaitingVisitor;
            SetScreenActive(false);
            if (hasVisitor)
            {
                if (door != null && !door.IsOpen)
                    door.SetOpenState(true);
                FindFirstObjectByType<HudView>()?.ShowInfo("Đã bấm Buzzer mở khóa cửa chính!", true);
            }
            else
            {
                FindFirstObjectByType<HudView>()?.ShowInfo("Đã tắt màn hình Camera Intercom.", true);
            }
        }
    }
}
