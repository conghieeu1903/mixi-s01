using System;
using System.Collections.Generic;
using UnityEngine;

// Bộ thẻ khi stream: rút thẻ, chọn thẻ, tính kết quả
public class StreamCardDeck
{
    readonly List<StreamCardData> pool = new List<StreamCardData>();
    readonly System.Random rng;

    public event Action<StreamCardData, bool> CardResolved;

    public StreamCardDeck(System.Random rng = null)
    {
        this.rng = rng ?? new System.Random();
        EnsureDefaultPool();
    }

    void EnsureDefaultPool()
    {
        if (pool.Count > 0) return;

        StreamCardData[] loaded = Resources.LoadAll<StreamCardData>("StreamCards");
        if (loaded != null && loaded.Length > 0)
        {
            pool.AddRange(loaded);
            return;
        }

        pool.Add(CreateRuntimeCard("Kể chuyện hài", "Tương tác vui vẻ với chat, dễ kéo thêm người xem.", 0.75f, 8, -2, 3f));
        pool.Add(CreateRuntimeCard("Thử thách độ khó cao", "Pha xử lý mạo hiểm, thắng lớn hoặc mất người xem.", 0.5f, 15, -5, -2f));
        pool.Add(CreateRuntimeCard("Đọc bình luận fan", "Trả lời câu hỏi của người xem nhiệt tình.", 0.85f, 6, -1, 4f));
        pool.Add(CreateRuntimeCard("Reaction clip hot", "Xem và bình luận video xu hướng.", 0.7f, 10, -3, 2f));
        pool.Add(CreateRuntimeCard("Giveaway nhỏ", "Khuấy động không khí phòng chat.", 0.8f, 12, -2, 5f));
    }

    static StreamCardData CreateRuntimeCard(string title, string desc, float chance, int winPct, int failPct, float mood)
    {
        StreamCardData card = ScriptableObject.CreateInstance<StreamCardData>();
        card.title = title;
        card.description = desc;
        card.successChance = chance;
        card.viewerPercentOnSuccess = winPct;
        card.viewerPercentOnFail = failPct;
        card.moodDelta = mood;
        return card;
    }

    // Rút một số thẻ để người chơi chọn
    public List<StreamCardData> Draw(int count)
    {
        EnsureDefaultPool();
        List<StreamCardData> candidates = new List<StreamCardData>(pool);
        List<StreamCardData> hand = new List<StreamCardData>();
        int drawCount = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < drawCount; i++)
        {
            int idx = rng.Next(candidates.Count);
            hand.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }
        return hand;
    }

    // Chơi một thẻ, trả về true nếu thành công
    public bool Play(StreamCardData card)
    {
        if (card == null) return false;
        bool success = rng.NextDouble() < card.successChance;

        if (GameWorld.HasInstance)
        {
            GameWorld world = GameWorld.Instance;
            int deltaPct = success ? card.viewerPercentOnSuccess : card.viewerPercentOnFail;
            if (deltaPct > 0)
                world.Stream.UpdateViewerPercent(deltaPct);
            world.Mood.Change(card.moodDelta);
        }

        CardResolved?.Invoke(card, success);
        return success;
    }
}
