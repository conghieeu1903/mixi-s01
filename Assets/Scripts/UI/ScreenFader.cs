using System;
using UnityEngine;
using UnityEngine.UIElements;

// Làm tối và sáng màn hình khi ngủ hoặc chuyển cảnh
[RequireComponent(typeof(UIDocument))]
public class ScreenFader : MonoBehaviour
{
    [Tooltip("Thời gian mờ dần (giây)")]
    [SerializeField] float duration = 0.5f;

    // Làm tối màn hình, xong thì gọi onDone
    public void FadeOut(Action onDone)
    {
    }

    // Làm sáng màn hình
    public void FadeIn(Action onDone = null)
    {
    }
}
