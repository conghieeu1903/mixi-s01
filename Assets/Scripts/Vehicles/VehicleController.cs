using UnityEngine;
using UnityEngine.InputSystem;

// Điều khiển xe: lên xe, xuống xe, lái
public class VehicleController : MonoBehaviour, IInteractable
{
    [SerializeField] Transform seat;
    [SerializeField] float maxSpeed = 14f;
    [SerializeField] float turnSpeed = 90f;

    public string Prompt => IsDriven ? "Xuống xe [F]" : "Lên xe lái";
    public bool IsDriven { get; private set; }

    PlayerMotor driverMotor;
    CharacterController driverCc;

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

        driverCc = driverMotor.GetComponent<CharacterController>();
        if (driverCc != null) driverCc.enabled = false;

        IsDriven = true;
        SnapDriverToSeat();
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

        if (Mathf.Abs(throttle) > 0.01f)
        {
            transform.position += transform.forward * (throttle * maxSpeed * Time.deltaTime);
            transform.Rotate(0f, steer * turnSpeed * Mathf.Sign(throttle) * Time.deltaTime, 0f);
        }

        SnapDriverToSeat();
    }

    // Dịch chuyển xe một đoạn (dùng cho cả điều khiển và kiểm thử)
    public void DriveForward(float distance)
    {
        transform.position += transform.forward * distance;
        if (IsDriven) SnapDriverToSeat();
    }

    void SnapDriverToSeat()
    {
        if (driverMotor == null) return;
        Vector3 targetPos = seat != null ? seat.position : transform.position + new Vector3(0f, 0.6f, 0f);
        driverMotor.transform.position = targetPos;
    }

    public void Exit()
    {
        if (!IsDriven) return;
        IsDriven = false;
        if (driverMotor != null)
        {
            driverMotor.transform.position = transform.position + transform.right * 1.6f + new Vector3(0f, 0.15f, 0f);
            if (driverCc != null) driverCc.enabled = true;
        }
        driverMotor = null;
        driverCc = null;
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã xuống xe", true);
    }
}
