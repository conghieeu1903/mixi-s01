using System;
using UnityEngine;

// Trạng thái hiển thị của một reel tại một thời điểm
public struct ReelState
{
    [Tooltip("Vị trí góc quay tính theo mặt, đã quy về 0..faceCount")]
    public float position;

    [Tooltip("Biểu tượng đã dừng, -1 nếu reel còn đang quay hoặc đang hãm")]
    public sbyte stoppedSymbol;

    public bool IsSpinning => stoppedSymbol < 0;
}

// Bộ đệm quỹ đạo các reel: quay đúng thông số SaiServer trả về, dừng dần trái sang phải và khớp mặt dừng server chốt
public class ReelStateBuffer
{
    public const int SymbolsPerReel = 7; // Mỗi reel có 7 hình, drum nhiều mặt hơn thì lặp lại layout

    const sbyte UnknownDelta = sbyte.MaxValue; // Chưa nhận mặt dừng từ server
    const double VelocityCarry = 0.6; // Giữ lại một phần vận tốc khi hãm để reel trôi nhẹ rồi khớp ô
    const double SettleDuration = 0.25; // Thời gian trượt sang ô server chốt khi khác dự đoán
    const double WobbleAmplitude = 0.06; // Biên độ rung của reel đang chờ server xác nhận số 7
    const double WobbleFrequency = 18.0; // Tần số rung (rad/giây)

    int reelCount = 1;
    int faceCount = 14;
    int[][] layouts = Array.Empty<int[]>();
    int[] restPositions = Array.Empty<int>();
    sbyte[] deltas = Array.Empty<sbyte>();

    bool active;
    double speed = 5.0;
    double spinUp = 0.3;
    double startInterval = 0.03;
    double stopInterval = 0.2;
    double stopDuration = 0.4;
    double stopAt = -1.0;
    double confirmAt = -1.0;

    public int ReelCount => reelCount;
    public bool IsActive => active;

    // Khởi tạo số reel, số mặt drum và layout mặc định khi dựng level
    public void Configure(int totalReels, int faces)
    {
        reelCount = Mathf.Max(1, totalReels);
        faceCount = Mathf.Max(SymbolsPerReel, faces);
        layouts = new int[reelCount][];
        restPositions = new int[reelCount];
        deltas = new sbyte[reelCount];
        for (int r = 0; r < reelCount; r++)
        {
            layouts[r] = DefaultLayout(r);
            deltas[r] = 0;
        }
        active = false;
        stopAt = -1.0;
        confirmAt = -1.0;
    }

    // Layout của một reel (mảng 7 biểu tượng)
    public int[] GetLayout(int reel) => layouts[reel];

    // Lấy mặt (0..6) và hình đang hiện của từng reel để gửi kèm spin_start
    public void FillRestFaces(int[] faces, int[] symbols)
    {
        for (int r = 0; r < reelCount; r++)
        {
            int face = ModInt(restPositions[r], SymbolsPerReel);
            faces[r] = face;
            symbols[r] = layouts[r][face];
        }
    }

    // Bắt đầu quỹ đạo quay theo layout và thông số server trả về
    public void BeginSpin(int[][] serverLayouts, int[] serverStartFaces, double spinSpeed, double spinUpTime, double startDelay, double stopDelay, float stopDur)
    {
        for (int r = 0; r < reelCount; r++)
        {
            if (serverLayouts != null && r < serverLayouts.Length && serverLayouts[r] != null && serverLayouts[r].Length == SymbolsPerReel)
                layouts[r] = (int[])serverLayouts[r].Clone();
            if (serverStartFaces != null && r < serverStartFaces.Length)
            {
                int face = ModInt(serverStartFaces[r], SymbolsPerReel);
                restPositions[r] += face - ModInt(restPositions[r], SymbolsPerReel);
            }
            deltas[r] = UnknownDelta;
        }
        speed = Math.Max(0.1, spinSpeed);
        spinUp = Math.Max(0.0001, spinUpTime);
        startInterval = Math.Max(0.0, startDelay);
        stopInterval = Math.Max(0.0, stopDelay);
        stopDuration = Math.Max(0.05, stopDur);
        stopAt = -1.0;
        confirmAt = -1.0;
        active = true;
    }

    // Người chơi bấm STOP tại thời điểm t: các reel bắt đầu dừng dần từ trái sang phải
    public void BeginStop(double t)
    {
        if (active && stopAt < 0.0) stopAt = t;
    }

    // Nhận mặt dừng server chốt (null thì giữ đúng dự đoán) và ghi lại độ lệch so với dự đoán của từng reel
    public void Confirm(int[] serverFaces, double t)
    {
        if (!active || stopAt < 0.0) return;
        confirmAt = t;
        for (int r = 0; r < reelCount; r++)
        {
            int predicted = ModInt(PredictedFace(r), SymbolsPerReel);
            int diff = serverFaces != null && r < serverFaces.Length
                ? ModInt(serverFaces[r] - predicted, SymbolsPerReel)
                : 0;
            deltas[r] = (sbyte)(diff > SymbolsPerReel / 2 ? diff - SymbolsPerReel : diff);
        }
    }

    // Toàn bộ reel đã dừng hẳn, kể cả reel phải trượt sang ô server chốt
    public bool AllFinished(double t)
    {
        if (!active || stopAt < 0.0) return false;
        for (int r = 0; r < reelCount; r++)
            if (t < FinishTime(r)) return false;
        return true;
    }

    // Chốt vị trí cuối của các reel làm vị trí nghỉ cho lượt sau
    public void EndSpin()
    {
        if (!active) return;
        if (stopAt >= 0.0)
        {
            for (int r = 0; r < reelCount; r++)
            {
                long final = PredictedFace(r) + (deltas[r] == UnknownDelta ? 0 : deltas[r]);
                restPositions[r] = ModInt(final, faceCount);
            }
        }
        for (int r = 0; r < reelCount; r++) deltas[r] = 0;
        active = false;
        stopAt = -1.0;
        confirmAt = -1.0;
    }

    // Các reel trừ reel cuối đều được dự đoán ra cùng một hình (để phát hiệu ứng hồi hộp)
    public bool AllButLastPredictedMatch()
    {
        if (!active || stopAt < 0.0 || reelCount <= 1) return false;
        int first = PredictedSymbol(0);
        for (int r = 1; r < reelCount - 1; r++)
            if (PredictedSymbol(r) != first) return false;
        return true;
    }

    // Vị trí con trỏ (0..reelCount-1) của reel đang dừng để camera bám theo, -1 nếu không trong pha dừng
    public double GetStoppingReelCursor(double t)
    {
        if (!active || stopAt < 0.0 || AllFinished(t)) return -1.0;
        double cursor = stopInterval > 0.0001 ? (t - stopAt) / stopInterval : reelCount - 1;
        return Math.Clamp(cursor, 0.0, reelCount - 1);
    }

    // Đếm số reel đã dừng có cùng hình với reel đầu tiên
    public int CountMatchingStopped(double t)
    {
        int first = Evaluate(0, t).stoppedSymbol;
        if (first < 0) return 0;
        int matched = 0;
        for (int r = 0; r < reelCount; r++)
            if (Evaluate(r, t).stoppedSymbol == first) matched++;
        return matched;
    }

    // Tính trạng thái hiển thị của reel r tại thời điểm t
    public ReelState Evaluate(int r, double t)
    {
        if (!active) return Stopped(r, restPositions[r]);

        double stopTime = StopTime(r);
        if (stopAt < 0.0 || t < stopTime) return Moving(SpinPos(r, t));

        double from = SpinPos(r, stopTime);
        long target = (long)Math.Floor(from + 0.5);
        sbyte delta = deltas[r];
        bool known = delta != UnknownDelta;
        bool knownAtStop = known && confirmAt <= stopTime;
        long to = knownAtStop ? target + delta : target;

        double u = (t - stopTime) / stopDuration;
        if (u < 1.0)
        {
            double carry = Velocity(r, stopTime) * stopDuration * VelocityCarry;
            return Moving(Hermite(from, carry, to, u));
        }
        if (knownAtStop || (known && delta == 0)) return Stopped(r, to);

        if (!known)
        {
            // Chưa có kết quả server: số 7 rung chờ xác nhận, hình thường coi như đã dừng theo dự đoán
            if (SymbolAt(r, target) == SymbolData.SevenId)
                return Moving(target + WobbleAmplitude * Math.Sin(t * WobbleFrequency));
            return Stopped(r, target);
        }

        // Server chốt khác dự đoán (số 7 trượt): trượt sang ô server chốt
        double settleStart = Math.Max(confirmAt, stopTime + stopDuration);
        double v = (t - settleStart) / SettleDuration;
        if (v < 1.0) return Moving(Hermite(target, 0.0, target + delta, Math.Max(0.0, v)));
        return Stopped(r, target + delta);
    }

    // Thời điểm reel r bắt đầu dừng
    double StopTime(int r) => stopAt + r * stopInterval;

    // Thời điểm reel r dừng hẳn, vô cực nếu còn chờ server
    double FinishTime(int r)
    {
        double stopEnd = StopTime(r) + stopDuration;
        sbyte delta = deltas[r];
        if (delta == UnknownDelta) return double.PositiveInfinity;
        if (delta == 0 || confirmAt <= StopTime(r)) return stopEnd;
        return Math.Max(confirmAt, stopEnd) + SettleDuration;
    }

    // Ô dự đoán reel r dừng (chưa quy về vòng), khớp công thức floor(pos + 0.5) trên server
    long PredictedFace(int r) => (long)Math.Floor(SpinPos(r, StopTime(r)) + 0.5);

    // Hình dự đoán reel r dừng
    int PredictedSymbol(int r) => SymbolAt(r, PredictedFace(r));

    // Vị trí reel r khi đang quay tại thời điểm t
    double SpinPos(int r, double t) => restPositions[r] + Distance(t - r * startInterval);

    // Quãng đường đã quay sau active giây, khớp spin_distance trong slot_game.lua
    double Distance(double activeTime)
    {
        if (activeTime <= 0.0) return 0.0;
        if (activeTime <= spinUp) return speed * 0.5 * activeTime * activeTime / spinUp;
        return speed * (0.5 * spinUp + (activeTime - spinUp));
    }

    // Vận tốc (ô/giây) của reel r tại thời điểm t
    double Velocity(int r, double t)
    {
        double activeTime = t - r * startInterval;
        if (activeTime <= 0.0) return 0.0;
        return activeTime <= spinUp ? speed * activeTime / spinUp : speed;
    }

    // Biểu tượng ở ô face của reel r
    int SymbolAt(int r, long face) => layouts[r][ModInt(face, SymbolsPerReel)];

    // Trạng thái reel đang quay hoặc đang hãm
    ReelState Moving(double position) => new ReelState { position = (float)ModD(position, faceCount), stoppedSymbol = -1 };

    // Trạng thái reel đã dừng ở ô face
    ReelState Stopped(int r, long face) => new ReelState { position = ModInt(face, faceCount), stoppedSymbol = (sbyte)SymbolAt(r, face) };

    // Nội suy Hermite từ from (vận tốc ban đầu carry) tới to (vận tốc 0), reel trôi tiếp rồi khớp ô
    static double Hermite(double from, double carry, double to, double u)
    {
        u = Math.Clamp(u, 0.0, 1.0);
        double u2 = u * u, u3 = u2 * u;
        return (2 * u3 - 3 * u2 + 1) * from + (u3 - 2 * u2 + u) * carry + (-2 * u3 + 3 * u2) * to;
    }

    // Layout mặc định trước lượt quay đầu tiên, mỗi reel lệch một hình cho đa dạng
    static int[] DefaultLayout(int reel)
    {
        int[] layout = new int[SymbolsPerReel];
        for (int i = 0; i < SymbolsPerReel; i++) layout[i] = (i + reel) % SymbolsPerReel;
        return layout;
    }

    // Chia lấy dư luôn dương cho số nguyên
    static int ModInt(long a, int m) => (int)(((a % m) + m) % m);

    // Chia lấy dư luôn dương cho số thực
    static double ModD(double a, double m) => ((a % m) + m) % m;
}
