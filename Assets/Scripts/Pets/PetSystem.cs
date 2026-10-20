using System.Collections.Generic;

// Quản lý thú cưng đang nuôi và hệ số viewer thú cưng mang lại
public class PetSystem
{
    public List<Pet> Pets { get; } = new List<Pet>();

    // Điểm phần trăm viewer cộng thêm khi stream có thú cưng
    public int ViewerBonus()
    {
        int bonus = 0;
        for (int i = 0; i < Pets.Count; i++)
        {
            Pet p = Pets[i];
            if (p != null && p.gameObject.activeInHierarchy && p.Happiness >= 40f)
                bonus += 5;
        }
        return bonus;
    }
}
