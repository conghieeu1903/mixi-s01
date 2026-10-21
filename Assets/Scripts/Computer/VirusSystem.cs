using System;
using UnityEngine;

// Virus trên máy tính: nhiễm virus gây Blue Screen, quét bằng app diệt virus
public class VirusSystem
{
    public bool IsInfected { get; private set; }

    public event Action BlueScreen;

    // Thử nhiễm virus khi tải file lạ
    public void TryInfect(float chance)
    {
        if (IsInfected || chance <= 0f) return;
        if (UnityEngine.Random.value <= chance)
        {
            IsInfected = true;
            AudioManager.PlayUi("ui_bsod");
            if (GameWorld.HasInstance)
                GameWorld.Instance.Stream.HasProblem = true;
            BlueScreen?.Invoke();
        }
    }

    // Quét và diệt virus
    public void Scan()
    {
        IsInfected = false;
        AudioManager.PlayUi("ui_success");
        if (GameWorld.HasInstance)
            GameWorld.Instance.Stream.HasProblem = false;
    }
}
