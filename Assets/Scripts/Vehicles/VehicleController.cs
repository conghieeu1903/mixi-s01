using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Điều khiển xe: lên xe, xuống xe mượt, lái xe và xoay bánh xe
public class VehicleController : MonoBehaviour, IInteractable
{
    [SerializeField] Transform seat;
    [SerializeField] float maxSpeed = 14f;
    [SerializeField] float turnSpeed = 90f;

    public string Prompt => IsDriven ? "Xuống xe [F]" : "Lên xe lái";
    public bool IsDriven { get; private set; }

    PlayerMotor driverMotor;
    PlayerLook driverLook;
    CharacterController driverCc;
    readonly List<Transform> wheels = new List<Transform>();
    Transform handlebar;
    Quaternion handlebarBaseRot;
    float currentSteerAngle;

    void Awake()
    {
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
                wheels.Add(child);
            else if (child.name.Contains("Handlebar"))
            {
                handlebar = child;
                handlebarBaseRot = child.localRotation;
            }
        }
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
        Vector3 startCamPos = driverLook != null && driverLook.CameraPivot != null
            ? driverLook.CameraPivot.position
            : driverMotor.transform.position + Vector3.up * 1.7f;
        Quaternion startCamRot = driverLook != null && driverLook.CameraPivot != null
            ? driverLook.CameraPivot.rotation
            : driverMotor.transform.rotation;

        driverCc = driverMotor.GetComponent<CharacterController>();
        if (driverCc != null) driverCc.enabled = false;
        driverMotor.enabled = false;

        IsDriven = true;
        SnapDriverToSeat();
        if (driverLook != null)
        {
            driverLook.SetRotation(transform.eulerAngles.y, 4f);
            driverLook.PlayOneWayTransition(startCamPos, startCamRot, 0.46f, -0.05f);
        }
        else
        {
            driverMotor.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        FindFirstObjectByType<HudView>()?.ShowInfo("Đang lái xe (WASD di chuyển, nhấn F để xuống xe)", true);
    }

    void Update()
    {
        if (!IsDriven || driverMotor == null) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame)
        {
            Exit();
            return;
        }

        float throttle = 0f;
        float steer = 0f;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttle -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
        }

        float dt = Time.deltaTime;
        if (Mathf.Abs(throttle) > 0.01f)
        {
            float moveDist = throttle * maxSpeed * dt;
            float yawDelta = steer * turnSpeed * Mathf.Sign(throttle) * dt;
            transform.position += transform.forward * moveDist;
            transform.Rotate(0f, yawDelta, 0f);
            driverMotor.transform.Rotate(0f, yawDelta, 0f, Space.World);
            SpinWheels(moveDist);
        }

        if (handlebar != null)
        {
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, steer * 18f, 10f * dt);
            handlebar.localRotation = handlebarBaseRot * Quaternion.Euler(0f, currentSteerAngle, 0f);
        }

        SnapDriverToSeat();
    }

    // Dịch chuyển xe một đoạn (dùng cho cả điều khiển và kiểm thử)
    public void DriveForward(float distance)
    {
        transform.position += transform.forward * distance;
        SpinWheels(distance);
        if (IsDriven) SnapDriverToSeat();
    }

    void SpinWheels(float distance)
    {
        if (wheels.Count == 0) CacheVehicleParts();
        float degrees = (distance / 0.33f) * Mathf.Rad2Deg;
        Vector3 axleWorld = transform.right;
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i] != null)
                wheels[i].Rotate(axleWorld, degrees, Space.World);
        }
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
        if (handlebar != null)
            handlebar.localRotation = handlebarBaseRot;

        if (driverMotor != null)
        {
            Vector3 seatCamPos = driverLook != null && driverLook.CameraPivot != null
                ? driverLook.CameraPivot.position
                : driverMotor.transform.position + Vector3.up * 1.4f;
            Quaternion seatCamRot = driverLook != null && driverLook.CameraPivot != null
                ? driverLook.CameraPivot.rotation
                : driverMotor.transform.rotation;

            // Bật lại motor rồi dịch chuyển qua Teleport để bật lại CharacterController và xóa vận tốc cũ
            Vector3 exitPosition = transform.position + transform.right * 1.6f + new Vector3(0f, 0.15f, 0f);
            float exitYaw = transform.eulerAngles.y + 20f;
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
        driverCc = null;
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã xuống xe", true);
    }
}
