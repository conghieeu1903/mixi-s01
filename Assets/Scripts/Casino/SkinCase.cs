using System.Collections.Generic;
using UnityEngine;

// Hòm skin: mở hòm nhận skin ngẫu nhiên
public class SkinCase : MonoBehaviour
{
    static readonly string[] SkinPool =
    {
        "Karambit Neon",
        "AK-47 Gold Mixi",
        "M4A1 Cyber Dragon",
        "Găng tay RGB Pro"
    };

    public List<string> UnlockedSkins { get; } = new List<string>();
    public string LastSkin { get; private set; }

    public void Open()
    {
        if (GameWorld.HasInstance && !GameWorld.Instance.Economy.TrySpend(10f))
            return;

        LastSkin = SkinPool[Random.Range(0, SkinPool.Length)];
        UnlockedSkins.Add(LastSkin);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(3f);
    }
}
