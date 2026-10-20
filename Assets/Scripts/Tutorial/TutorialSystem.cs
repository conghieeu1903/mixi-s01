using System;
using System.Collections.Generic;
using UnityEngine;

// Chạy chuỗi bước hướng dẫn cho người chơi mới
public class TutorialSystem : MonoBehaviour
{
    [SerializeField] List<TutorialStepData> steps = new List<TutorialStepData>();

    public TutorialStepData Current { get; private set; }

    public event Action<TutorialStepData> StepStarted;

    // Báo một bước đã xong
    public void Complete(string stepId)
    {
    }

    // Bỏ qua toàn bộ hướng dẫn
    public void Skip()
    {
    }
}
