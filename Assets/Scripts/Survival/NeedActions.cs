// Các hành động sinh tồn dùng chung: tốn thời gian trong game, chỉ cộng mood khi nhu cầu đó đang bật
public static class NeedActions
{
    public const int EatMinutes = 10;
    public const int ShowerMinutes = 20;
    public const int ToiletMinutes = 5;

    // Ăn một món, trả về câu thông báo cho HUD
    public static string Eat(FoodData food)
    {
        GameWorld world = GameWorld.Instance;
        bool wasHungry = world.Needs.IsHungry;
        world.Clock.AdvanceMinutes(EatMinutes);
        world.Needs.Eat(food.hungerRelief, food.stressDelta);
        if (!wasHungry) return $"Đã ăn {food.displayName} (-{food.hungerRelief} Đói)";
        world.Mood.Change(4f);
        return $"Đã ăn {food.displayName} (-{food.hungerRelief} Đói, +4 Mood)";
    }

    // Uống thuốc, trả về false nếu không bị bệnh để không phí thuốc
    public static bool TakeMedicine()
    {
        GameWorld world = GameWorld.Instance;
        if (!world.Needs.IsSick) return false;
        world.Needs.Cure();
        world.Mood.Change(5f);
        return true;
    }

    // Tắm, trả về câu thông báo cho HUD
    public static string Shower()
    {
        GameWorld world = GameWorld.Instance;
        bool wasSmelly = world.Needs.IsSmelly;
        world.Clock.AdvanceMinutes(ShowerMinutes);
        world.Needs.Shower();
        world.Needs.ChangeStress(-10f);
        if (!wasSmelly) return "Đã tắm (-10 Stress)";
        world.Mood.Change(5f);
        return "Đã tắm rửa sạch sẽ (+5 Mood, -10 Stress)";
    }

    // Đi vệ sinh, trả về câu thông báo cho HUD
    public static string UseToilet()
    {
        GameWorld world = GameWorld.Instance;
        bool needed = world.Needs.NeedsToilet;
        world.Clock.AdvanceMinutes(ToiletMinutes);
        world.Needs.UseToilet();
        if (!needed) return "Đã đi vệ sinh";
        world.Mood.Change(2f);
        return "Đã đi vệ sinh (+2 Mood)";
    }
}
