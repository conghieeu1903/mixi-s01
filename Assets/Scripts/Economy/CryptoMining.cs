using System;

// Đào Bitcoin: cộng coin định kỳ theo sức đào của các rig, cần có router
public class CryptoMining
{
    public double Bitcoin { get; private set; }

    public event Action<double> BitcoinChanged;

    // Gọi định kỳ để cộng coin
    public void Tick(float deltaSeconds, float totalMiningPower, bool hasRouter)
    {
    }

    // Bán coin lấy tiền
    public float Sell(double amount)
    {
        return 0f;
    }
}
