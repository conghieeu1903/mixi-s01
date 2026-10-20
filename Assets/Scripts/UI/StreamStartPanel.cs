using System;

// Panel chọn chủ đề và bắt đầu stream khi ngồi vào máy
public class StreamStartPanel : UIPanel
{
    // Người chơi bấm bắt đầu với chủ đề đã chọn
    public event Action<StreamCategory> StartRequested;

    // Mở panel cho bàn máy đang ngồi
    public void Open(ComputerDesk desk)
    {
    }
}
