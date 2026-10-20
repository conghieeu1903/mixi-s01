using UnityEngine;
using UnityEngine.UIElements;

// HUD trong game: giờ, ngày, tiền, badge hóa đơn, gợi ý tương tác, thông báo
[RequireComponent(typeof(UIDocument))]
public class HudView : MonoBehaviour
{
    [SerializeField] PlayerInteractor interactor;

    // Lấy element trong UXML và đăng ký sự kiện của GameWorld
    void OnEnable()
    {
    }

    // Hủy đăng ký sự kiện
    void OnDisable()
    {
    }

    // Cập nhật đồng hồ và ngày
    void RefreshClock()
    {
    }

    // Cập nhật số tiền
    void RefreshMoney(float money)
    {
    }

    // Đổi gợi ý phím theo vật đang nhắm tới
    void RefreshPrompt(IInteractable target)
    {
    }

    // Hiện một dòng thông báo ở cuối màn hình (Bottom Info)
    public void ShowInfo(string message, bool positive)
    {
    }
}
