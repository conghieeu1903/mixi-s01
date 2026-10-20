using UnityEngine;

// Đồ ăn: giảm đói, có thể đổi stress
[CreateAssetMenu(menuName = "Game/Items/Food")]
public class FoodData : ItemData
{
    public int hungerRelief = 3;
    public float stressDelta;
}
