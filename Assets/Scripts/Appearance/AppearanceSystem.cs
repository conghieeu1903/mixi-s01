using UnityEngine;

// Áp ngoại hình lên model nhân vật
public class AppearanceSystem : MonoBehaviour
{
    public AppearanceData Current { get; private set; } = new AppearanceData();

    public void Apply(AppearanceData data)
    {
    }
}
