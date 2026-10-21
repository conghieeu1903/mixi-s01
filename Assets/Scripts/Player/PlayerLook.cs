using System;
using System.Collections;
using UnityEngine;

// Xoay camera góc nhìn thứ nhất và hỗ trợ chuyển góc máy mượt khi ngồi, nằm ngủ, tắm, vệ sinh
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerLook : MonoBehaviour
{
    [Tooltip("Transform của camera (con của nhân vật), xoay theo trục X để nhìn lên xuống")]
    [SerializeField] Transform cameraPivot;

    [Tooltip("Độ nhạy chuột (độ trên mỗi đơn vị delta)")]
    [SerializeField] float mouseSensitivity = 0.1f;

    [Tooltip("Tốc độ xoay tối đa với tay cầm (độ/giây)")]
    [SerializeField] float gamepadDegreesPerSecond = 180f;

    [Tooltip("Giới hạn góc nhìn lên (độ)")]
    [SerializeField] float maxPitchUp = 85f;

    [Tooltip("Giới hạn góc nhìn xuống (độ)")]
    [SerializeField] float maxPitchDown = 85f;

    [Tooltip("Đảo trục Y")]
    [SerializeField] bool invertY;

    [Tooltip("Khoảng cách từ mắt tới đỉnh capsule, camera = chiều cao capsule trừ giá trị này")]
    [SerializeField] float eyeOffsetFromTop = 0.1f;

    [Header("Nhịp bước chân & nghiêng đầu")]
    [SerializeField] float walkBobFrequency = 9.5f;
    [SerializeField] float sprintBobFrequency = 13.5f;
    [SerializeField] float crouchBobFrequency = 6.5f;
    [SerializeField] float walkBobAmplitudeY = 0.032f;
    [SerializeField] float sprintBobAmplitudeY = 0.055f;
    [SerializeField] float walkBobAmplitudeX = 0.016f;
    [SerializeField] float strafeTiltAngle = 2.2f;

    PlayerInputReader input;
    PlayerMotor motor;
    Camera viewCamera;
    float defaultFov = 70f;
    float pitch;
    float bobTimer;
    Vector3 currentBobOffset;
    float currentRoll;
    float landingDip;
    bool wasGrounded = true;
    float smoothedEyeY = 1.7f;

    Coroutine activePoseRoutine;
    bool isPoseOverridden;
    Vector3 poseWorldPos;
    Quaternion poseWorldRot;

    public Transform CameraPivot => cameraPivot;
    public bool IsTransitioning => isPoseOverridden;
    public float Pitch => pitch;

    // Độ nhạy chuột, cho menu Settings chỉnh
    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Max(0.001f, value);
    }

    public bool InvertY
    {
        get => invertY;
        set => invertY = value;
    }

    // Lấy component cần dùng
    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
        if (cameraPivot != null)
            viewCamera = cameraPivot.GetComponentInChildren<Camera>();
        if (viewCamera == null)
            viewCamera = Camera.main;
        if (viewCamera != null)
            defaultFov = viewCamera.fieldOfView;
        if (motor != null)
            smoothedEyeY = motor.CurrentHeight - eyeOffsetFromTop;
    }

    // Xoay camera sau khi nhân vật đã di chuyển trong Update, kèm nhịp bước chân và nghiêng đầu
    void LateUpdate()
    {
        if (isPoseOverridden)
        {
            if (cameraPivot != null)
                cameraPivot.SetPositionAndRotation(poseWorldPos, poseWorldRot);
            return;
        }

        float dt = Time.deltaTime;
        Vector2 delta = input != null ? input.ReadLook(mouseSensitivity, gamepadDegreesPerSecond) : Vector2.zero;
        transform.Rotate(0f, delta.x, 0f, Space.Self);

        pitch += invertY ? delta.y : -delta.y;
        pitch = Mathf.Clamp(pitch, -maxPitchUp, maxPitchDown);

        // Tính nhịp nhún bước chân và độ nhún khi vừa tiếp đất
        float baseEyeY = motor != null ? motor.CurrentHeight - eyeOffsetFromTop : 1.7f;
        smoothedEyeY = Mathf.Lerp(smoothedEyeY, baseEyeY, 14f * dt);

        if (motor != null && motor.enabled)
        {
            bool grounded = motor.IsGrounded;
            if (!wasGrounded && grounded)
                landingDip = -0.085f;
            wasGrounded = grounded;

            landingDip = Mathf.Lerp(landingDip, 0f, 10f * dt);

            float speed = motor.PlanarSpeed;
            if (grounded && speed > 0.15f)
            {
                float freq = motor.IsSprinting ? sprintBobFrequency : motor.IsCrouching ? crouchBobFrequency : walkBobFrequency;
                float ampY = motor.IsSprinting ? sprintBobAmplitudeY : motor.IsCrouching ? walkBobAmplitudeY * 0.55f : walkBobAmplitudeY;
                float ampX = motor.IsSprinting ? walkBobAmplitudeX * 1.6f : walkBobAmplitudeX;
                float speedFactor = Mathf.Clamp01(speed / 3.2f);

                bobTimer += dt * freq;
                Vector3 targetBob = new Vector3(
                    Mathf.Cos(bobTimer * 0.5f) * ampX * speedFactor,
                    Mathf.Sin(bobTimer) * ampY * speedFactor,
                    0f);
                currentBobOffset = Vector3.Lerp(currentBobOffset, targetBob, 14f * dt);
            }
            else
            {
                currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, 10f * dt);
            }
        }
        else
        {
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, 12f * dt);
            landingDip = Mathf.Lerp(landingDip, 0f, 12f * dt);
        }

        // Nghiêng đầu nhẹ khi di chuyển ngang (A/D) hoặc lia chuột nhanh
        float moveX = (input != null && motor != null && motor.enabled) ? input.Move.x : 0f;
        float targetRoll = -moveX * strafeTiltAngle - Mathf.Clamp(delta.x * 0.3f, -1.6f, 1.6f);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, 10f * dt);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);
            cameraPivot.localPosition = new Vector3(
                currentBobOffset.x,
                smoothedEyeY + currentBobOffset.y + landingDip,
                0f);
        }
    }

    // Đặt hướng nhìn trực tiếp (khi dịch chuyển hoặc ra khỏi màn hình máy tính)
    public void SetRotation(float yaw, float newPitch)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitch = Mathf.Clamp(newPitch, -maxPitchUp, maxPitchDown);
        currentRoll = 0f;
        if (cameraPivot != null && !isPoseOverridden)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // Lia camera một chiều từ vị trí cũ sang vị trí mắt mới (dùng khi bước qua cửa, lên/xuống xe)
    public void PlayOneWayTransition(
        Vector3 fromWorldPos,
        Quaternion fromWorldRot,
        float duration = 0.45f,
        float arcHeight = 0.04f,
        Action onCompleted = null)
    {
        if (cameraPivot == null)
        {
            onCompleted?.Invoke();
            return;
        }

        if (activePoseRoutine != null)
            StopCoroutine(activePoseRoutine);

        activePoseRoutine = StartCoroutine(OneWayTransitionRoutine(
            fromWorldPos,
            fromWorldRot,
            duration,
            arcHeight,
            onCompleted));
    }

    IEnumerator OneWayTransitionRoutine(
        Vector3 fromWorldPos,
        Quaternion fromWorldRot,
        float duration,
        float arcHeight,
        Action onCompleted)
    {
        isPoseOverridden = true;
        poseWorldPos = fromWorldPos;
        poseWorldRot = fromWorldRot;

        float elapsed = 0f;
        float dur = Mathf.Max(0.05f, duration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = t * t * (3f - 2f * t);

            float eyeHeight = motor != null ? motor.CurrentHeight - eyeOffsetFromTop : 1.7f;
            Vector3 targetPos = transform.position + Vector3.up * eyeHeight;
            Quaternion targetRot = transform.rotation * Quaternion.Euler(pitch, 0f, 0f);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);

            poseWorldPos = Vector3.Lerp(fromWorldPos, targetPos, s) + arc;
            poseWorldRot = Quaternion.Slerp(fromWorldRot, targetRot, s);
            yield return null;
        }

        float finalEyeY = motor != null ? motor.CurrentHeight - eyeOffsetFromTop : 1.7f;
        smoothedEyeY = finalEyeY;
        isPoseOverridden = false;
        cameraPivot.localPosition = new Vector3(0f, finalEyeY, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        activePoseRoutine = null;
        onCompleted?.Invoke();
    }

    // Giữ camera ở tư thế mục tiêu (ví dụ cúi xuống nhìn vào trong thùng PC) cho tới khi gọi ReleaseCameraPose
    public void HoldCameraPose(
        Vector3 targetWorldPos,
        Quaternion targetWorldRot,
        float duration = 0.45f,
        float targetFov = 0f)
    {
        if (cameraPivot == null) return;
        if (activePoseRoutine != null)
            StopCoroutine(activePoseRoutine);
        activePoseRoutine = StartCoroutine(HoldCameraPoseRoutine(targetWorldPos, targetWorldRot, duration, targetFov));
    }

    IEnumerator HoldCameraPoseRoutine(
        Vector3 targetWorldPos,
        Quaternion targetWorldRot,
        float duration,
        float targetFov)
    {
        Vector3 startPos = cameraPivot.position;
        Quaternion startRot = cameraPivot.rotation;
        if (viewCamera == null) viewCamera = Camera.main;
        float startFov = viewCamera != null ? viewCamera.fieldOfView : defaultFov;
        float endFov = targetFov > 1f ? targetFov : defaultFov;

        isPoseOverridden = true;
        poseWorldPos = startPos;
        poseWorldRot = startRot;

        float elapsed = 0f;
        float dur = Mathf.Max(0.05f, duration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = t * t * (3f - 2f * t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * -0.05f);
            poseWorldPos = Vector3.Lerp(startPos, targetWorldPos, s) + arc;
            poseWorldRot = Quaternion.Slerp(startRot, targetWorldRot, s);
            if (viewCamera != null)
                viewCamera.fieldOfView = Mathf.Lerp(startFov, endFov, s);
            yield return null;
        }

        poseWorldPos = targetWorldPos;
        poseWorldRot = targetWorldRot;
        if (viewCamera != null)
            viewCamera.fieldOfView = endFov;
        activePoseRoutine = null;
    }

    // Đưa camera từ tư thế đang giữ trở về góc nhìn đứng thứ nhất bình thường
    public void ReleaseCameraPose(float duration = 0.42f)
    {
        if (cameraPivot == null || !isPoseOverridden) return;
        if (activePoseRoutine != null)
            StopCoroutine(activePoseRoutine);
        activePoseRoutine = StartCoroutine(ReleaseCameraPoseRoutine(duration));
    }

    IEnumerator ReleaseCameraPoseRoutine(float duration)
    {
        Vector3 startPos = poseWorldPos;
        Quaternion startRot = poseWorldRot;
        if (viewCamera == null) viewCamera = Camera.main;
        float startFov = viewCamera != null ? viewCamera.fieldOfView : defaultFov;

        float elapsed = 0f;
        float dur = Mathf.Max(0.05f, duration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = t * t * (3f - 2f * t);

            float eyeHeight = motor != null ? motor.CurrentHeight - eyeOffsetFromTop : 1.7f;
            Vector3 endPos = transform.position + Vector3.up * eyeHeight;
            Quaternion endRot = transform.rotation * Quaternion.Euler(pitch, 0f, 0f);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.04f);

            poseWorldPos = Vector3.Lerp(startPos, endPos, s) + arc;
            poseWorldRot = Quaternion.Slerp(startRot, endRot, s);
            if (viewCamera != null)
                viewCamera.fieldOfView = Mathf.Lerp(startFov, defaultFov, s);
            yield return null;
        }

        float finalEyeY = motor != null ? motor.CurrentHeight - eyeOffsetFromTop : 1.7f;
        smoothedEyeY = finalEyeY;
        isPoseOverridden = false;
        cameraPivot.localPosition = new Vector3(0f, finalEyeY, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (viewCamera != null)
            viewCamera.fieldOfView = defaultFov;
        activePoseRoutine = null;
    }

    // Chạy hiệu ứng lia camera mượt từ vị trí đứng sang tư thế mục tiêu (ngồi/nằm) rồi đứng dậy
    public void PlayPoseTransition(
        Vector3 targetWorldPos,
        Quaternion targetWorldRot,
        float moveInDuration,
        float holdDuration,
        float moveOutDuration,
        Action onReachedTarget = null,
        Action onCompleted = null)
    {
        if (cameraPivot == null)
        {
            onReachedTarget?.Invoke();
            onCompleted?.Invoke();
            return;
        }

        if (activePoseRoutine != null)
            StopCoroutine(activePoseRoutine);

        activePoseRoutine = StartCoroutine(PoseTransitionRoutine(
            targetWorldPos,
            targetWorldRot,
            moveInDuration,
            holdDuration,
            moveOutDuration,
            onReachedTarget,
            onCompleted));
    }

    IEnumerator PoseTransitionRoutine(
        Vector3 targetWorldPos,
        Quaternion targetWorldRot,
        float moveInDuration,
        float holdDuration,
        float moveOutDuration,
        Action onReachedTarget,
        Action onCompleted)
    {
        Vector3 startPos = cameraPivot.position;
        Quaternion startRot = cameraPivot.rotation;
        isPoseOverridden = true;
        poseWorldPos = startPos;
        poseWorldRot = startRot;

        float elapsed = 0f;
        float inDur = Mathf.Max(0.05f, moveInDuration);
        while (elapsed < inDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / inDur);
            float s = t * t * (3f - 2f * t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * -0.05f);
            poseWorldPos = Vector3.Lerp(startPos, targetWorldPos, s) + arc;
            poseWorldRot = Quaternion.Slerp(startRot, targetWorldRot, s);
            yield return null;
        }

        poseWorldPos = targetWorldPos;
        poseWorldRot = targetWorldRot;
        onReachedTarget?.Invoke();

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        float eyeHeight = motor != null ? motor.CurrentHeight - eyeOffsetFromTop : 1.7f;
        Vector3 endPos = transform.position + Vector3.up * eyeHeight;
        Quaternion endRot = transform.rotation * Quaternion.Euler(pitch, 0f, 0f);

        elapsed = 0f;
        float outDur = Mathf.Max(0.05f, moveOutDuration);
        while (elapsed < outDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / outDur);
            float s = t * t * (3f - 2f * t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.04f);
            poseWorldPos = Vector3.Lerp(targetWorldPos, endPos, s) + arc;
            poseWorldRot = Quaternion.Slerp(targetWorldRot, endRot, s);
            yield return null;
        }

        isPoseOverridden = false;
        cameraPivot.localPosition = new Vector3(0f, eyeHeight, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        activePoseRoutine = null;
        onCompleted?.Invoke();
    }
}
