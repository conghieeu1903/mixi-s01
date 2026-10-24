using System;
using UnityEngine;

// Đào Bitcoin và sàn giao dịch CryptoWeb: cộng coin định kỳ theo sức đào, tỷ giá BTC biến động theo thời gian thực giống SLS2
public class CryptoMining
{
    public const float TickIntervalSeconds = 2f;
    public const float CoinPerPowerPerTick = 0.0025f;
    public const float UsdPerBitcoin = 40000f;
    public const float MinBtcPrice = 26000f;
    public const float MaxBtcPrice = 68000f;

    public double Bitcoin { get; private set; }
    public float BtcPrice { get; private set; } = UsdPerBitcoin;
    public float LastBtcPrice { get; private set; } = UsdPerBitcoin;
    public float PriceChangePercent => LastBtcPrice > 1f ? ((BtcPrice - LastBtcPrice) / LastBtcPrice) * 100f : 0f;
    public bool IsPriceUp => BtcPrice >= LastBtcPrice;

    public event Action<double> BitcoinChanged;
    public event Action<float> PriceChanged;

    float timer;
    float priceTickTimer;

    // Gọi định kỳ để cộng coin và cập nhật biến động nhẹ tỷ giá BTC giống CryptoWeb.MinorBitcoinPriceChanges của SLS2
    public void Tick(float deltaSeconds, float totalMiningPower, bool hasRouter)
    {
        if (deltaSeconds <= 0f) return;

        priceTickTimer += deltaSeconds;
        if (priceTickTimer >= 2.2f)
        {
            priceTickTimer = 0f;
            MinorBitcoinPriceChanges();
        }

        if (!hasRouter || totalMiningPower <= 0f) return;

        timer += deltaSeconds;
        while (timer >= TickIntervalSeconds)
        {
            timer -= TickIntervalSeconds;
            Bitcoin += totalMiningPower * CoinPerPowerPerTick;
            BitcoinChanged?.Invoke(Bitcoin);
        }
    }

    // Dao động tỷ giá ngắn hạn (0.05% - 0.15%) mỗi vài giây giống CryptoWeb của SLS2
    public void MinorBitcoinPriceChanges()
    {
        float pct = UnityEngine.Random.Range(0.05f, 0.15f);
        float delta = BtcPrice * (pct / 100f);
        if (BtcPrice <= MinBtcPrice)
            BtcPrice += delta;
        else if (BtcPrice >= MaxBtcPrice)
            BtcPrice -= delta;
        else
            BtcPrice += UnityEngine.Random.value > 0.48f ? delta : -delta;

        BtcPrice = Mathf.Clamp(BtcPrice, MinBtcPrice, MaxBtcPrice);
        PriceChanged?.Invoke(BtcPrice);
    }

    // Biến động tỷ giá BTC mỗi ngày mới (1% - 18%) giống CryptoWeb.DailyBitcoinPriceChanges của SLS2
    public void DailyBitcoinPriceChanges()
    {
        LastBtcPrice = BtcPrice;
        float pct = UnityEngine.Random.Range(1.5f, 18f);
        float delta = BtcPrice * (pct / 100f);
        if (BtcPrice < MinBtcPrice * 1.1f)
            BtcPrice += delta;
        else if (BtcPrice > MaxBtcPrice * 0.9f)
            BtcPrice -= delta;
        else
            BtcPrice += UnityEngine.Random.value > 0.47f ? delta : -delta;

        BtcPrice = Mathf.Clamp(BtcPrice, MinBtcPrice, MaxBtcPrice);
        PriceChanged?.Invoke(BtcPrice);
    }

    // Bán một lượng BTC lấy tiền USD theo tỷ giá BtcPrice hiện tại
    public float Sell(double amount)
    {
        if (amount <= 0.0 || Bitcoin <= 0.0) return 0f;
        double actual = Math.Min(amount, Bitcoin);
        Bitcoin -= actual;
        float rate = BtcPrice > 100f ? BtcPrice : UsdPerBitcoin;
        float usd = Mathf.Round((float)(actual * rate) * 100f) / 100f;
        if (GameWorld.HasInstance && usd > 0f)
        {
            GameWorld.Instance.Economy.Earn(usd);
            AudioManager.Play("coin_gain");
        }
        BitcoinChanged?.Invoke(Bitcoin);
        return usd;
    }
}
