using UnityEngine;

// Một bước hướng dẫn: nội dung và điều kiện hoàn thành
[CreateAssetMenu(menuName = "Game/Tutorial Step")]
public class TutorialStepData : ScriptableObject
{
    public string stepId;
    [TextArea] public string message;
}
