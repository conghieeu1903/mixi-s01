using System;
using System.Collections.Generic;

// Đa ngôn ngữ: đổi ngôn ngữ và lấy chuỗi đã dịch theo key
public static class LocalizationService
{
    static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>
    {
        {
            "vi", new Dictionary<string, string>
            {
                { "menu.play", "Bắt đầu mới" },
                { "menu.continue", "Tiếp tục" },
                { "menu.settings", "Cài đặt" },
                { "menu.quit", "Thoát" },
                { "stream.open", "Lên sóng" },
                { "stream.close", "Tắt stream" }
            }
        },
        {
            "en", new Dictionary<string, string>
            {
                { "menu.play", "New Game" },
                { "menu.continue", "Continue" },
                { "menu.settings", "Settings" },
                { "menu.quit", "Quit" },
                { "stream.open", "Go Live" },
                { "stream.close", "End Stream" }
            }
        }
    };

    public static string Language { get; private set; } = "vi";

    public static event Action LanguageChanged;

    public static void SetLanguage(string code)
    {
        if (string.IsNullOrEmpty(code) || Language == code) return;
        Language = code;
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (Tables.TryGetValue(Language, out var dict) && dict.TryGetValue(key, out string val))
            return val;
        return key;
    }
}
