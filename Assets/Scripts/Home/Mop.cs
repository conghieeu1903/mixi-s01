using UnityEngine;

// Cây lau nhà, cầm trên tay để lau vết bẩn
public class Mop : MonoBehaviour
{
    [SerializeField] float cleanPower = 0.5f;

    // Lau vết bẩn đang nhắm tới
    public void Use(Dirt target)
    {
    }
}
