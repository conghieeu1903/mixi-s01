using System;

// Đa ngôn ngữ: đổi ngôn ngữ và lấy chuỗi đã dịch theo key
public static class LocalizationService
{
    public static string Language { get; private set; } = "vi";

    public static event Action LanguageChanged;

    public static void SetLanguage(string code)
    {
    }

    public static string Get(string key)
    {
        return key;
    }
}
