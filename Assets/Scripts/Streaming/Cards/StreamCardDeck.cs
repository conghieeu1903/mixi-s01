using System;
using System.Collections.Generic;

// Bộ thẻ khi stream: rút thẻ, chọn thẻ, tính kết quả
public class StreamCardDeck
{
    public event Action<StreamCardData, bool> CardResolved;

    // Rút một số thẻ để người chơi chọn
    public List<StreamCardData> Draw(int count)
    {
        return new List<StreamCardData>();
    }

    // Chơi một thẻ, trả về true nếu thành công
    public bool Play(StreamCardData card)
    {
        return false;
    }
}
