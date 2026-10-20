using UnityEngine;

// Đặt sẵn tham số Animator để xem thử animation prototype khi vào Play mode
[RequireComponent(typeof(Animator))]
public class PrototypeAnimatorPreview : MonoBehaviour
{
    [SerializeField] private float speed; // Giá trị tham số Speed (0 đứng, 1.5 đi, 4.5 chạy)
    [SerializeField] private int action; // Giá trị tham số Action của nhân vật
    [SerializeField] private string boolParameter; // Tham số bool cần bật, ví dụ IsOpen hoặc IsSpinning
    [SerializeField] private float toggleInterval; // Lớn hơn 0 thì tự bật tắt tham số bool theo chu kỳ này

    private Animator animator;
    private float timer;

    private void Start()
    {
        animator = GetComponent<Animator>();
        SetIfExists("Speed", AnimatorControllerParameterType.Float, () => animator.SetFloat("Speed", speed));
        SetIfExists("Action", AnimatorControllerParameterType.Int, () => animator.SetInteger("Action", action));
        if (!string.IsNullOrEmpty(boolParameter))
            SetIfExists(boolParameter, AnimatorControllerParameterType.Bool, () => animator.SetBool(boolParameter, true));
    }

    private void Update()
    {
        if (toggleInterval <= 0f || string.IsNullOrEmpty(boolParameter))
            return;

        timer += Time.deltaTime;
        if (timer < toggleInterval)
            return;

        timer = 0f;
        animator.SetBool(boolParameter, !animator.GetBool(boolParameter));
    }

    private void SetIfExists(string parameterName, AnimatorControllerParameterType type, System.Action apply)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
            {
                apply();
                return;
            }
        }
    }
}
