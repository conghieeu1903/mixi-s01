using System;
using UnityEngine;

// Đào Bitcoin: cộng coin định kỳ theo sức đào của các rig, cần có router
public class CryptoMining
{
    public const float TickIntervalSeconds = 2f;
    public const float CoinPerPowerPerTick = 0.0025f;
    public const float UsdPerBitcoin = 40000f;

    public double Bitcoin { get; private set; }

    public event Action<double> BitcoinChanged;

    float timer;

    // Gọi định kỳ để cộng coin
    public void Tick(float deltaSeconds, float totalMiningPower, bool hasRouter)
    {
        if (!hasRouter || totalMiningPower <= 0f || deltaSeconds <= 0f) return;

        timer += deltaSeconds;
        while (timer >= TickIntervalSeconds)
        {
            timer -= TickIntervalSeconds;
            Bitcoin += totalMiningPower * CoinPerPowerPerTick;
            BitcoinChanged?.Invoke(Bitcoin);
        }
    }

    // Bán coin lấy tiền
    public float Sell(double amount)
    {
        if (amount <= 0.0 || Bitcoin <= 0.0) return 0f;
        double actual = Math.Min(amount, Bitcoin);
        Bitcoin -= actual;
        float usd = Mathf.Round((float)(actual * UsdPerBitcoin) * 100f) / 100f;
        if (GameWorld.HasInstance && usd > 0f)
        {
            GameWorld.Instance.Economy.Earn(usd);
            AudioManager.Play("coin_gain");
        }
        BitcoinChanged?.Invoke(Bitcoin);
        return usd;
    }
}
