using UnityEngine;

// Rig đào coin đặt trong nhà
public class MiningRig : MonoBehaviour
{
    [SerializeField] float miningPower = 1f;

    public float MiningPower => miningPower;
    public bool IsRunning { get; private set; }

    public void SetRunning(bool running)
    {
    }
}
