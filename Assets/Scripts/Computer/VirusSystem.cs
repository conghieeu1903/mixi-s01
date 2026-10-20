using System;

// Virus trên máy tính: nhiễm virus gây Blue Screen, quét bằng app diệt virus
public class VirusSystem
{
    public bool IsInfected { get; private set; }

    public event Action BlueScreen;

    // Thử nhiễm virus khi tải file lạ
    public void TryInfect(float chance)
    {
    }

    // Quét và diệt virus
    public void Scan()
    {
    }
}
