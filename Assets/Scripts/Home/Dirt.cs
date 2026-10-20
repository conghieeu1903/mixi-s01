using UnityEngine;

// Vết bẩn trong nhà, lau bằng cây lau nhà
public class Dirt : MonoBehaviour
{
    [Range(0f, 1f)] [SerializeField] float amount = 1f;

    public float Amount => amount;

    // Lau bớt vết bẩn
    public void Clean(float power)
    {
    }
}
