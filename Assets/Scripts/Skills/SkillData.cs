using UnityEngine;

// Dữ liệu một kỹ năng: tên, mô tả, giá điểm, kỹ năng cần học trước
[CreateAssetMenu(menuName = "Game/Skill")]
public class SkillData : ScriptableObject
{
    public SkillId id;
    public string displayName;
    [TextArea] public string description;
    public int cost = 1;
    public SkillData[] requires;
}
