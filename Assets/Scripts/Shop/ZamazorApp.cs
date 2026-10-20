using UnityEngine;

// App mua sắm Zamazor trên PC: danh mục, giỏ hàng, đặt hàng
public class ZamazorApp : PcApp
{
    [SerializeField] ShopCatalog catalog;

    public override void OnOpen()
    {
    }

    // Thêm vào giỏ
    public void AddToCart(ItemData item)
    {
    }

    // Đặt hàng giỏ hiện tại
    public void Checkout()
    {
    }
}
