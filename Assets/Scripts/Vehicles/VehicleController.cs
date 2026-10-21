using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Điều khiển xe có quán tính vật lý: gia tốc, trớn trôi, phanh, độ trễ đánh lái, nhún phuộc/nghiêng thân và phản hồi camera
[DefaultExecutionOrder(100)]
public class VehicleController : MonoBehaviour, IInteractable
{
    [SerializeField] Transform seat;
    [SerializeField] float maxSpeed = 15f;
    [SerializeField] float turnSpeed = 95f;
    [SerializeField] float acceleration = 9.5f;
    [SerializeField] float rollingFriction = 3.4f;
    [SerializeField] float brakeDeceleration = 24f;
    [SerializeField] float tireGrip = 6.5f;

    public string Prompt => IsDriven ? "Xuống xe [F]" : "Lên xe lái";
    public bool IsDriven { get; private set; }
    public float CurrentSpeed => currentSpeed;

    struct WheelSlot
    {
        public Transform transform;
        public bool isFront;
        public float spinAngle;
    }

    PlayerMotor driverMotor;
    PlayerLook driverLook;
    Camera driverCamera;
    CharacterController driverCc;

    readonly List<WheelSlot> wheels = new List<WheelSlot>();
    Transform handlebar;
    Quaternion handlebarBaseRot;
    bool isTwoWheeler;
    bool isBicycle;
    bool isMotorbike;

    float currentSpeed;
    float smoothedSteer;
    float currentYaw;
    float currentPitch;
    float currentRoll;
    Vector3 planarVelocity;
    float smoothedAccel;
    Vector3 cameraInertiaOffset;
    float roadRumbleTimer;
    float engineAudioTimer;
    float brakeAudioCooldown;
    float baseFov = 70f;

    void Awake()
    {
        currentYaw = transform.eulerAngles.y;
        CacheVehicleParts();
    }

    void CacheVehicleParts()
    {
        wheels.Clear();
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform) continue;
            if (child.name.Contains("Wheel"))
            {
                wheels.Add(new WheelSlot
                {
                    transform = child,
                    isFront = child.localPosition.z > 0.05f,
                    spinAngle = 0f
                });
            }
            else if (child.name.Contains("Handlebar"))
            {
                handlebar = child;
                handlebarBaseRot = child.localRotation;
            }
        }

        isBicycle = name.IndexOf("Bicycle", System.StringComparison.OrdinalIgnoreCase) >= 0;
        isMotorbike = name.IndexOf("Motor", System.StringComparison.OrdinalIgnoreCase) >= 0;
        isTwoWheeler = wheels.Count <= 2 || isMotorbike || isBicycle;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsDriven;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsDriven)
        {
            Exit();
            return;
        }

        driverMotor = interactor != null ? interactor.GetComponent<PlayerMotor>() : FindFirstObjectByType<PlayerMotor>();
        if (driverMotor == null) return;

        driverLook = driverMotor.GetComponent<PlayerLook>();
        driverCamera = driverMotor.GetComponentInChildren<Camera>() ?? Camera.main;
        if (driverCamera != null)
            baseFov = 70f;

        Vector3 startCamPos = driverLook != null && driverLook.CameraPivot != null
            ? driverLook.CameraPivot.position
            : driverMotor.transform.position + Vector3.up * 1.7f;
        Quaternion startCamRot = driverLook != null && driverLook.CameraPivot != null
            ? driverLook.CameraPivot.rotation
            : driverMotor.transform.rotation;

        driverCc = driverMotor.GetComponent<CharacterController>();
        if (driverCc != null) driverCc.enabled = false;
        driverMotor.enabled = false;

        currentYaw = transform.eulerAngles.y;
        IsDriven = true;
        SnapDriverToSeat();
        if (isBicycle)
        {
            AudioManager.PlayAt("bicycle_bell", transform.position);
        }
        else if (isMotorbike)
        {
            AudioManager.PlayAt("motorbike_start", transform.position);
        }
        else
        {
            AudioManager.PlayAt("car_door_open", transform.position);
            AudioManager.PlayAt("car_start", transform.position);
        }
        AudioManager.Instance.PlayMusic(isBicycle ? "bgm_downtown" : "bgm_race");

        if (driverLook != null)
        {
            driverLook.SetRotation(currentYaw, 4f);
            driverLook.PlayOneWayTransition(startCamPos, startCamRot, 0.46f, -0.05f);
        }
        else
        {
            driverMotor.transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        FindFirstObjectByType<HudView>()?.ShowInfo("Đang lái xe (WASD lái, Space phanh tay, H còi, F xuống xe)", true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Keyboard kb = Keyboard.current;
        if (IsDriven && kb != null && kb.fKey.wasPressedThisFrame)
        {
            Exit();
            return;
        }

        float throttleInput = 0f;
        float steerInput = 0f;
        bool handbrake = false;

        if (IsDriven && driverMotor != null && kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttleInput += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttleInput -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerInput += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerInput -= 1f;
            handbrake = kb.spaceKey.isPressed;

            if (kb.hKey.wasPressedThisFrame)
                AudioManager.PlayAt(isBicycle ? "bicycle_bell" : "car_horn", transform.position);
        }

        SimulateVehiclePhysics(dt, throttleInput, steerInput, handbrake);
        UpdateDrivingAudio(dt, handbrake);

        if (IsDriven)
            SnapDriverToSeat();
    }

    // Phát tiếng động cơ hoặc bàn đạp xe và tiếng phanh theo tốc độ thực tế
    void UpdateDrivingAudio(float dt, bool handbrake)
    {
        if (!IsDriven) return;

        if (brakeAudioCooldown > 0f)
            brakeAudioCooldown -= dt;

        float absSpeed = Mathf.Abs(currentSpeed);
        if (handbrake && absSpeed > 2.2f && brakeAudioCooldown <= 0f)
        {
            brakeAudioCooldown = 0.45f;
            AudioManager.PlayAt(isBicycle ? "bicycle_pedal" : "car_brake", transform.position);
        }

        if (absSpeed < 0.35f) return;

        engineAudioTimer -= dt;
        if (engineAudioTimer <= 0f)
        {
            float speedRatio = Mathf.Clamp01(absSpeed / Mathf.Max(1f, maxSpeed));
            if (isBicycle)
            {
                engineAudioTimer = Mathf.Lerp(0.48f, 0.24f, speedRatio);
                AudioManager.PlayAt("bicycle_pedal", transform.position, Mathf.Lerp(0.9f, 1.25f, speedRatio));
            }
            else
            {
                engineAudioTimer = Mathf.Lerp(0.52f, 0.28f, speedRatio);
                AudioManager.PlayAt("car_engine_idle", transform.position, Mathf.Lerp(0.85f, 1.45f, speedRatio));
            }
        }
    }

    void SimulateVehiclePhysics(float dt, float throttleInput, float steerInput, bool handbrake)
    {
        float prevSpeed = currentSpeed;

        // 1. Tính vận tốc dọc: gia tốc, phanh ngược chiều, phanh tay và lực cản lăn (trớn trôi)
        if (handbrake)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * 1.25f * dt);
        }
        else if (throttleInput > 0.01f)
        {
            if (currentSpeed < -0.1f)
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * dt);
            else
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * dt);
        }
        else if (throttleInput < -0.01f)
        {
            if (currentSpeed > 0.1f)
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * dt);
            else
                currentSpeed = Mathf.MoveTowards(currentSpeed, -maxSpeed * 0.45f, acceleration * 0.75f * dt);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, rollingFriction * dt);
        }

        // 2. Độ trễ đánh lái và tự trả lái
        float steerRate = Mathf.Abs(steerInput) > 0.01f ? 5.2f : 7.5f;
        smoothedSteer = Mathf.MoveTowards(smoothedSteer, steerInput, steerRate * dt);

        // 3. Xoay hướng xe phụ thuộc vào vận tốc thực tế (đứng yên không xoay tại chỗ)
        float absSpeed = Mathf.Abs(currentSpeed);
        float speedRatio = Mathf.Clamp01(absSpeed / Mathf.Max(1f, maxSpeed));
        if (absSpeed > 0.06f)
        {
            float lowSpeedCurve = Mathf.Sin(Mathf.Clamp01(absSpeed / 2.2f) * Mathf.PI * 0.5f);
            float highSpeedDamp = Mathf.Lerp(1f, 0.72f, speedRatio);
            float directionSign = Mathf.Sign(currentSpeed);
            float yawDelta = smoothedSteer * turnSpeed * lowSpeedCurve * highSpeedDamp * directionSign * dt;

            currentYaw += yawDelta;
            if (IsDriven && driverMotor != null)
                driverMotor.transform.Rotate(0f, yawDelta, 0f, Space.World);
        }

        // 4. Quán tính trượt ngang (drift/grip) và dịch chuyển vị trí
        Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
        Vector3 desiredVelocity = flatForward * currentSpeed;
        float currentGrip = handbrake ? tireGrip * 0.42f : tireGrip;
        planarVelocity = Vector3.Lerp(planarVelocity, desiredVelocity, 1f - Mathf.Exp(-currentGrip * dt));

        if (planarVelocity.sqrMagnitude > 0.00001f)
            transform.position += planarVelocity * dt;

        // 5. Nhún phuộc dọc (Pitch khi tăng tốc/phanh) và nghiêng thân xe (Roll khi vào cua)
        float rawAccel = (currentSpeed - prevSpeed) / dt;
        smoothedAccel = Mathf.Lerp(smoothedAccel, rawAccel, 10f * dt);

        float targetPitch = Mathf.Clamp(-smoothedAccel * 0.26f, -3.5f, 4.2f);
        float signedSpeedRatio = Mathf.Clamp(currentSpeed / Mathf.Max(1f, maxSpeed), -1f, 1f);
        float targetRoll = isTwoWheeler
            ? -smoothedSteer * Mathf.Abs(signedSpeedRatio) * 17f
            : smoothedSteer * signedSpeedRatio * 3.8f;

        currentPitch = Mathf.Lerp(currentPitch, targetPitch, 9f * dt);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, 8f * dt);
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);

        // 6. Xoay bánh xe và bẻ góc bánh trước / ghi-đông
        UpdateWheelsAndSteering(currentSpeed * dt, smoothedSteer);
    }

    void LateUpdate()
    {
        if (!IsDriven || driverLook == null || driverLook.IsTransitioning || driverLook.CameraPivot == null)
            return;

        float dt = Time.deltaTime;
        float absSpeed = Mathf.Abs(currentSpeed);
        float speedRatio = Mathf.Clamp01(absSpeed / Mathf.Max(1f, maxSpeed));

        // Quán tính đầu người lái: ngả nhẹ về sau khi vọt ga, nhao nhẹ về trước khi phanh gấp, nghiêng theo góc cua
        Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
        Vector3 flatRight = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.right;
        float surgeOffset = Mathf.Clamp(-smoothedAccel * 0.0055f, -0.065f, 0.075f);
        float swayOffset = -smoothedSteer * speedRatio * 0.045f;

        if (absSpeed > 0.2f)
            roadRumbleTimer += dt * (18f + speedRatio * 22f);
        float rumbleY = Mathf.Sin(roadRumbleTimer) * 0.0045f * speedRatio;

        Vector3 targetOffset = flatForward * surgeOffset + flatRight * swayOffset + Vector3.up * rumbleY;
        cameraInertiaOffset = Vector3.Lerp(cameraInertiaOffset, targetOffset, 10f * dt);

        driverLook.CameraPivot.position += cameraInertiaOffset;

        float cameraRoll = isTwoWheeler ? currentRoll * 0.65f : -currentRoll * 0.45f;
        float cameraPitchOffset = currentPitch * 0.4f;
        driverLook.CameraPivot.rotation *= Quaternion.Euler(cameraPitchOffset, 0f, cameraRoll);

        if (driverCamera != null)
        {
            float targetFov = baseFov + speedRatio * 8f;
            driverCamera.fieldOfView = Mathf.Lerp(driverCamera.fieldOfView, targetFov, 8f * dt);
        }
    }

    // Dịch chuyển xe một đoạn (dùng cho cả điều khiển và kiểm thử)
    public void DriveForward(float distance)
    {
        Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
        transform.position += flatForward * distance;
        UpdateWheelsAndSteering(distance, smoothedSteer);
        if (IsDriven) SnapDriverToSeat();
    }

    void UpdateWheelsAndSteering(float distance, float steer)
    {
        if (wheels.Count == 0) CacheVehicleParts();
        float spinDelta = (distance / 0.33f) * Mathf.Rad2Deg;
        float steerAngle = steer * 28f;

        for (int i = 0; i < wheels.Count; i++)
        {
            WheelSlot slot = wheels[i];
            if (slot.transform == null) continue;
            slot.spinAngle = Mathf.Repeat(slot.spinAngle + spinDelta, 360f);
            float yaw = slot.isFront ? steerAngle : 0f;
            slot.transform.localRotation = Quaternion.Euler(0f, yaw, 90f) * Quaternion.Euler(0f, slot.spinAngle, 0f);
            wheels[i] = slot;
        }

        if (handlebar != null)
            handlebar.localRotation = handlebarBaseRot * Quaternion.Euler(0f, steer * 20f, 0f);
    }

    void SnapDriverToSeat()
    {
        if (driverMotor == null) return;
        Vector3 targetPos = seat != null ? seat.position : transform.position + new Vector3(0f, 0.25f, 0f);
        driverMotor.transform.position = targetPos;
    }

    public void Exit()
    {
        if (!IsDriven) return;
        IsDriven = false;
        smoothedSteer = 0f;
        smoothedAccel = 0f;
        cameraInertiaOffset = Vector3.zero;

        if (handlebar != null)
            handlebar.localRotation = handlebarBaseRot;
        if (driverCamera != null)
            driverCamera.fieldOfView = baseFov;

        if (driverMotor != null)
        {
            Vector3 seatCamPos = driverLook != null && driverLook.CameraPivot != null
                ? driverLook.CameraPivot.position
                : driverMotor.transform.position + Vector3.up * 1.4f;
            Quaternion seatCamRot = driverLook != null && driverLook.CameraPivot != null
                ? driverLook.CameraPivot.rotation
                : driverMotor.transform.rotation;

            Vector3 flatRight = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.right;
            Vector3 exitPosition = transform.position + flatRight * 1.6f + new Vector3(0f, 0.15f, 0f);
            float exitYaw = currentYaw + 20f;
            driverMotor.enabled = true;
            driverMotor.Teleport(exitPosition, exitYaw);

            if (driverLook != null)
            {
                driverLook.SetRotation(exitYaw, 0f);
                driverLook.PlayOneWayTransition(seatCamPos, seatCamRot, 0.42f, 0.05f);
            }
        }
        driverMotor = null;
        driverLook = null;
        driverCamera = null;
        driverCc = null;
        if (!isTwoWheeler)
            AudioManager.PlayAt("car_door_close", transform.position);
        AudioManager.Instance.PlayMusic("bgm_downtown");
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã xuống xe", true);
    }
}
