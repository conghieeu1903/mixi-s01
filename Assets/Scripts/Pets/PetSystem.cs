using System.Collections.Generic;

// Quản lý thú cưng đang nuôi và hệ số viewer thú cưng mang lại
public class PetSystem
{
    public List<Pet> Pets { get; } = new List<Pet>();

    // Điểm phần trăm viewer cộng thêm khi stream có thú cưng
    public int ViewerBonus()
    {
        return 0;
    }
}
