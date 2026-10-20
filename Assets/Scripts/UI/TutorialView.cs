using UnityEngine;
using UnityEngine.UIElements;

// Hộp hướng dẫn hiện ở góc màn hình
[RequireComponent(typeof(UIDocument))]
public class TutorialView : MonoBehaviour
{
    // Hiện nội dung một bước
    public void Show(TutorialStepData step)
    {
    }

    public void Hide()
    {
    }
}
