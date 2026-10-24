using System.Collections;
using UnityEngine;

// Quản lý sự kiện đội đặc nhiệm SWAT phá cửa, ném lựu đạn choáng Flashbang và áp giải người chơi ra ngoài đường giống SwatManager, Swat & SwatCamera của SLS2
public class SwatManager : MonoBehaviour
{
    GameObject swat1Obj;
    GameObject swat2Obj;
    Transform swat1Head;
    GameObject flashBangObj;
    Camera swatCam;
    Transform lookAtTarget;
    GameObject swatTriggerObj;

    Vector3 swat1TargetPos;
    Vector3 swat2TargetPos;
    float swatMoveSpeed = 3.6f;
    bool swatMoving;
    bool isRaidRunning;
    bool triggerArmed;
    float autoRaidCountdown;

    public bool IsRaidRunning => isRaidRunning;
    public bool IsTriggerArmed => triggerArmed;

    void Awake()
    {
        EnsureSwatRig();
    }

    void Update()
    {
        if (triggerArmed && !isRaidRunning)
        {
            autoRaidCountdown -= Time.deltaTime;
            if (autoRaidCountdown <= 0f)
            {
                SwatOperation();
                return;
            }

            PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
            if (motor != null && swatTriggerObj != null)
            {
                Vector3 diff = motor.transform.position - swatTriggerObj.transform.position;
                diff.y = 0f;
                if (diff.sqrMagnitude <= 2.25f)
                    SwatOperation();
            }
        }

        if (isRaidRunning)
        {
            if (swatCam != null && swatCam.gameObject.activeSelf && lookAtTarget != null)
            {
                Vector3 dir = lookAtTarget.position - swatCam.transform.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    swatCam.transform.rotation = Quaternion.Slerp(swatCam.transform.rotation, targetRot, 5.5f * Time.deltaTime);
                }
            }

            if (swatMoving)
            {
                MoveSwatUnit(swat1Obj != null ? swat1Obj.transform : null, swat1TargetPos);
                MoveSwatUnit(swat2Obj != null ? swat2Obj.transform : null, swat2TargetPos);
            }
        }
    }

    void MoveSwatUnit(Transform unit, Vector3 targetWorldPos)
    {
        if (unit == null || !unit.gameObject.activeSelf) return;
        unit.position = Vector3.MoveTowards(unit.position, targetWorldPos, swatMoveSpeed * Time.deltaTime);
        Vector3 dir = targetWorldPos - unit.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            unit.rotation = Quaternion.Slerp(unit.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
    }

    // Kích hoạt bẫy sự kiện SWAT trước cửa phòng (hoặc tự động ập vào sau vài giây) giống SwatManager.OpenSwatTrigger của SLS2
    public void OpenSwatTrigger()
    {
        if (isRaidRunning) return;
        EnsureSwatRig();
        triggerArmed = true;
        autoRaidCountdown = 6f;
        if (swatTriggerObj != null)
            swatTriggerObj.SetActive(true);

        FindFirstObjectByType<DoorIntercom>()?.DoorRing("Đội Đặc Nhiệm SWAT");
        FindFirstObjectByType<HudView>()?.ShowInfo("🚨 CẢNH BÁO: Có tiếng bước chân dồn dập của Đội Đặc Nhiệm SWAT ngay ngoài cửa chính!", false);
    }

    // Bắt đầu chiến dịch SWAT phá cửa, ném Flashbang và áp giải người chơi ra khỏi nhà giống SwatManager.SwatOperation của SLS2
    public void SwatOperation()
    {
        if (isRaidRunning) return;
        EnsureSwatRig();
        triggerArmed = false;
        if (swatTriggerObj != null)
            swatTriggerObj.SetActive(false);

        StartCoroutine(SwatOperationRoutine());
    }

    IEnumerator SwatOperationRoutine()
    {
        isRaidRunning = true;

        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null && desktop.IsOpen)
            desktop.Close();

        ComputerDesk desk = FindFirstObjectByType<ComputerDesk>();
        if (desk != null && desk.IsOccupied)
            desk.StandUp();

        PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
        PlayerLook look = FindFirstObjectByType<PlayerLook>();
        Camera playerCam = Camera.main;

        AudioManager.Play("door_open");
        FindFirstObjectByType<HudView>()?.ShowInfo("💥 RẦM!! Đội đặc nhiệm SWAT đang phá cửa xông vào phòng!", false);

        if (motor != null) motor.enabled = false;
        if (look != null) look.enabled = false;

        if (swatCam != null && playerCam != null)
        {
            swatCam.transform.SetPositionAndRotation(playerCam.transform.position, playerCam.transform.rotation);
            swatCam.fieldOfView = 68f;
            swatCam.gameObject.SetActive(true);
            playerCam.enabled = false;
        }

        if (flashBangObj != null)
        {
            flashBangObj.transform.position = transform.TransformPoint(new Vector3(0f, 0.16f, 2.15f));
            lookAtTarget = flashBangObj.transform;
        }

        yield return new WaitForSeconds(0.5f);

        Door door = FindFirstObjectByType<Door>();
        if (door != null)
            door.SetOpenState(true);

        yield return new WaitForSeconds(0.2f);

        if (flashBangObj != null)
            flashBangObj.SetActive(true);

        if (swat1Obj != null)
        {
            swat1Obj.transform.position = transform.TransformPoint(new Vector3(-0.25f, 0.1f, 3.25f));
            swat1Obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            swat1Obj.SetActive(true);
        }

        if (swat2Obj != null)
        {
            swat2Obj.transform.position = transform.TransformPoint(new Vector3(0.28f, 0.1f, 3.45f));
            swat2Obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            swat2Obj.SetActive(true);
        }

        // Lựu đạn Flashbang lăn vào giữa sàn nhà trong 1.3 giây trước khi phát nổ
        float rollElapsed = 0f;
        Vector3 fbStart = transform.TransformPoint(new Vector3(0f, 0.22f, 2.65f));
        Vector3 fbEnd = transform.TransformPoint(new Vector3(0f, 0.14f, 1.35f));
        while (rollElapsed < 1.3f)
        {
            rollElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(rollElapsed / 1.3f);
            if (flashBangObj != null)
                flashBangObj.transform.position = Vector3.Lerp(fbStart, fbEnd, t);
            yield return null;
        }

        AudioManager.PlayUi("purchase_fail");
        yield return new WaitForSeconds(0.2f);

        ScreenFader fader = FindFirstObjectByType<ScreenFader>();
        if (fader != null)
            fader.FlashbangOn(0.18f);

        // Hai đặc nhiệm SWAT ập vào vị trí khống chế trong phòng giống swat1Pos, swat2Pos, swat3Pos của SLS2
        swatMoving = true;
        swat1TargetPos = transform.TransformPoint(new Vector3(-0.35f, 0.1f, 1.15f));
        swat2TargetPos = transform.TransformPoint(new Vector3(0.35f, 0.1f, 1.35f));

        yield return new WaitForSeconds(1.0f);

        if (swat1Head != null)
            lookAtTarget = swat1Head;

        yield return new WaitForSeconds(0.5f);

        if (fader != null)
            fader.FlashbangOff(0.65f);

        yield return new WaitForSeconds(1.0f);

        swat2TargetPos = transform.TransformPoint(new Vector3(0.85f, 0.1f, 0.15f));
        yield return new WaitForSeconds(1.0f);

        swat1TargetPos = transform.TransformPoint(new Vector3(-0.75f, 0.1f, -0.10f));

        if (fader != null)
            fader.FadeOut(null);

        yield return new WaitForSeconds(2.0f);

        // Đóng Stream nếu đang phát sóng và đưa người chơi ra ngoài vỉa hè trước cửa nhà giống playerTeleportPositionAfterSwat
        if (GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen)
            GameWorld.Instance.Stream.Close();

        if (motor != null)
        {
            Vector3 outsidePos = transform.TransformPoint(new Vector3(0f, 0.15f, 6.2f));
            motor.Teleport(outsidePos, 180f);
        }
        if (look != null)
            look.SetRotation(180f, 0f);

        yield return new WaitForSeconds(1.5f);

        swatMoving = false;
        if (flashBangObj != null) flashBangObj.SetActive(false);
        if (swat1Obj != null) swat1Obj.SetActive(false);
        if (swat2Obj != null) swat2Obj.SetActive(false);

        if (swatCam != null)
            swatCam.gameObject.SetActive(false);
        if (playerCam != null)
            playerCam.enabled = true;

        if (motor != null) motor.enabled = true;
        if (look != null) look.enabled = true;

        if (fader != null)
            fader.FadeIn();

        isRaidRunning = false;

        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(-8f);

        FindFirstObjectByType<HudView>()?.ShowInfo("🚨 Đội đặc nhiệm SWAT đã lục soát phòng do báo động giả và thả bạn ra ngoài đường (-8 Mood)!", false);
    }

    void EnsureSwatRig()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (swatTriggerObj == null)
        {
            Transform existingTrig = transform.Find("SwatTriggerZone");
            if (existingTrig != null)
                swatTriggerObj = existingTrig.gameObject;
            else
            {
                swatTriggerObj = new GameObject("SwatTriggerZone");
                swatTriggerObj.transform.SetParent(transform, false);
                swatTriggerObj.transform.localPosition = new Vector3(0f, 1.0f, 1.45f);
                BoxCollider trigCol = swatTriggerObj.AddComponent<BoxCollider>();
                trigCol.isTrigger = true;
                trigCol.size = new Vector3(2.4f, 2.0f, 2.2f);
                swatTriggerObj.SetActive(false);
            }
        }

        if (flashBangObj == null)
        {
            Transform existingFb = transform.Find("SwatFlashbang");
            if (existingFb != null)
                flashBangObj = existingFb.gameObject;
            else
            {
                flashBangObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flashBangObj.name = "SwatFlashbang";
                flashBangObj.transform.SetParent(transform, false);
                flashBangObj.transform.localScale = new Vector3(0.08f, 0.09f, 0.08f);
                if (flashBangObj.TryGetComponent<Collider>(out Collider fbc)) Destroy(fbc);
                flashBangObj.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = new Color(0.24f, 0.28f, 0.22f, 1f) };
                flashBangObj.SetActive(false);
            }
        }

        if (swat1Obj == null)
        {
            Transform existingS1 = transform.Find("SwatOfficer_1");
            swat1Obj = existingS1 != null ? existingS1.gameObject : CreateSwatOfficerMesh("SwatOfficer_1", sh);
            swat1Head = swat1Obj.transform.Find("SwatHead");
            swat1Obj.SetActive(false);
        }

        if (swat2Obj == null)
        {
            Transform existingS2 = transform.Find("SwatOfficer_2");
            swat2Obj = existingS2 != null ? existingS2.gameObject : CreateSwatOfficerMesh("SwatOfficer_2", sh);
            swat2Obj.SetActive(false);
        }

        if (swatCam == null)
        {
            Transform existingCam = transform.Find("SwatCutsceneCamera");
            GameObject camGo;
            if (existingCam != null)
                camGo = existingCam.gameObject;
            else
            {
                camGo = new GameObject("SwatCutsceneCamera");
                camGo.transform.SetParent(transform, false);
            }
            swatCam = camGo.GetComponent<Camera>();
            if (swatCam == null)
                swatCam = camGo.AddComponent<Camera>();
            camGo.SetActive(false);
        }
    }

    GameObject CreateSwatOfficerMesh(string unitName, Shader sh)
    {
        GameObject root = new GameObject(unitName);
        root.transform.SetParent(transform, false);

        Material armorMat = new Material(sh) { color = new Color(0.10f, 0.14f, 0.22f, 1f) };
        Material visorMat = new Material(sh) { color = new Color(0.18f, 0.62f, 0.88f, 1f) };
        Material gunMat = new Material(sh) { color = new Color(0.08f, 0.08f, 0.10f, 1f) };

        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "SwatTorso";
        torso.transform.SetParent(root.transform, false);
        torso.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        torso.transform.localScale = new Vector3(0.46f, 0.68f, 0.28f);
        if (torso.TryGetComponent<Collider>(out Collider c1)) Destroy(c1);
        torso.GetComponent<MeshRenderer>().sharedMaterial = armorMat;

        GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legs.name = "SwatLegs";
        legs.transform.SetParent(root.transform, false);
        legs.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        legs.transform.localScale = new Vector3(0.36f, 0.62f, 0.22f);
        if (legs.TryGetComponent<Collider>(out Collider c2)) Destroy(c2);
        legs.GetComponent<MeshRenderer>().sharedMaterial = armorMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "SwatHead";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.48f, 0f);
        head.transform.localScale = new Vector3(0.30f, 0.32f, 0.30f);
        if (head.TryGetComponent<Collider>(out Collider c3)) Destroy(c3);
        head.GetComponent<MeshRenderer>().sharedMaterial = armorMat;

        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "SwatVisor";
        visor.transform.SetParent(head.transform, false);
        visor.transform.localPosition = new Vector3(0f, 0.05f, 0.42f);
        visor.transform.localScale = new Vector3(0.75f, 0.32f, 0.25f);
        if (visor.TryGetComponent<Collider>(out Collider c4)) Destroy(c4);
        visor.GetComponent<MeshRenderer>().sharedMaterial = visorMat;

        GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifle.name = "SwatAimGun";
        rifle.transform.SetParent(root.transform, false);
        rifle.transform.localPosition = new Vector3(0.16f, 1.12f, 0.32f);
        rifle.transform.localScale = new Vector3(0.08f, 0.12f, 0.52f);
        if (rifle.TryGetComponent<Collider>(out Collider c5)) Destroy(c5);
        rifle.GetComponent<MeshRenderer>().sharedMaterial = gunMat;

        return root;
    }
}
