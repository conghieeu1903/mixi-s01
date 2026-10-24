using System;
using System.Collections.Generic;

// Đa ngôn ngữ: đổi ngôn ngữ, tra cứu chuỗi đã dịch và lấy văn bản mẫu theo mã ngôn ngữ
public static class LocalizationService
{
    static readonly string[] SupportedCodes = { "vi", "en", "ja", "ko", "zh" };

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
                { "stream.close", "Tắt stream" },
                { "settings.title", "Cài đặt (Settings)" },
                { "settings.tab.graphics", "Đồ họa" },
                { "settings.tab.audio", "Âm thanh" },
                { "settings.tab.controls", "Điều khiển & Phím" },
                { "settings.tab.language", "Ngôn ngữ" },
                { "settings.default", "Mặc định" },
                { "settings.back", "Quay lại" },
                { "settings.apply", "Áp dụng" },
                { "settings.preview.title", "Chào mừng đến với Streamer Simulator" },
                { "settings.preview.body", "Kiểm tra thiết bị PC, bật OBS và bắt đầu buổi phát sóng trực tiếp đầu tiên của bạn." }
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
                { "stream.close", "End Stream" },
                { "settings.title", "Settings" },
                { "settings.tab.graphics", "Graphics" },
                { "settings.tab.audio", "Audio" },
                { "settings.tab.controls", "Controls & Keys" },
                { "settings.tab.language", "Language" },
                { "settings.default", "Default" },
                { "settings.back", "Back" },
                { "settings.apply", "Apply" },
                { "settings.preview.title", "Welcome to Streamer Simulator" },
                { "settings.preview.body", "Check your PC rig, open OBS, and start your first live stream session." }
            }
        },
        {
            "ja", new Dictionary<string, string>
            {
                { "menu.play", "ニューゲーム" },
                { "menu.continue", "つづきから" },
                { "menu.settings", "設定" },
                { "menu.quit", "終了" },
                { "stream.open", "配信開始" },
                { "stream.close", "配信終了" },
                { "settings.title", "設定 (Settings)" },
                { "settings.tab.graphics", "グラフィック" },
                { "settings.tab.audio", "オーディオ" },
                { "settings.tab.controls", "操作・キー設定" },
                { "settings.tab.language", "言語" },
                { "settings.default", "初期設定" },
                { "settings.back", "戻る" },
                { "settings.apply", "適用" },
                { "settings.preview.title", "Streamer Simulator へようこそ" },
                { "settings.preview.body", "PC機材を確認し、OBSを起動して最初のライブ配信を始めましょう。" }
            }
        },
        {
            "ko", new Dictionary<string, string>
            {
                { "menu.play", "새 게임" },
                { "menu.continue", "이어하기" },
                { "menu.settings", "설정" },
                { "menu.quit", "종료" },
                { "stream.open", "방송 시작" },
                { "stream.close", "방송 종료" },
                { "settings.title", "설정 (Settings)" },
                { "settings.tab.graphics", "그래픽" },
                { "settings.tab.audio", "오디오" },
                { "settings.tab.controls", "조작 및 키 설정" },
                { "settings.tab.language", "언어" },
                { "settings.default", "기본값" },
                { "settings.back", "뒤로" },
                { "settings.apply", "적용" },
                { "settings.preview.title", "Streamer Simulator에 오신 것을 환영합니다" },
                { "settings.preview.body", "PC 장비를 점검하고 OBS를 켜서 첫 라이브 스트리밍을 시작하세요." }
            }
        },
        {
            "zh", new Dictionary<string, string>
            {
                { "menu.play", "新游戏" },
                { "menu.continue", "继续游戏" },
                { "menu.settings", "设置" },
                { "menu.quit", "退出" },
                { "stream.open", "开始直播" },
                { "stream.close", "结束直播" },
                { "settings.title", "设置 (Settings)" },
                { "settings.tab.graphics", "画面" },
                { "settings.tab.audio", "音频" },
                { "settings.tab.controls", "控制与按键" },
                { "settings.tab.language", "语言" },
                { "settings.default", "恢复默认" },
                { "settings.back", "返回" },
                { "settings.apply", "应用" },
                { "settings.preview.title", "欢迎来到 Streamer Simulator" },
                { "settings.preview.body", "检查您的电脑配置，打开 OBS 并开始您的第一场直播。" }
            }
        }
    };

    public static string Language { get; private set; } = "vi";

    public static event Action LanguageChanged;

    // Đổi ngôn ngữ theo mã (vi, en, ja, ko, zh)
    public static void SetLanguage(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        string normalized = code.Trim().ToLowerInvariant();
        if (!Tables.ContainsKey(normalized))
            normalized = "vi";
        if (Language == normalized) return;
        Language = normalized;
        LanguageChanged?.Invoke();
    }

    // Đổi ngôn ngữ theo chỉ số trong dropdown
    public static void SetLanguageByIndex(int index)
    {
        int clamped = index >= 0 && index < SupportedCodes.Length ? index : 0;
        SetLanguage(SupportedCodes[clamped]);
    }

    // Lấy mã ngôn ngữ từ chỉ số
    public static string IndexToCode(int index)
    {
        int clamped = index >= 0 && index < SupportedCodes.Length ? index : 0;
        return SupportedCodes[clamped];
    }

    // Lấy chỉ số từ mã ngôn ngữ
    public static int CodeToIndex(string code)
    {
        if (string.IsNullOrEmpty(code)) return 0;
        for (int i = 0; i < SupportedCodes.Length; i++)
        {
            if (string.Equals(SupportedCodes[i], code, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 0;
    }

    // Tra cứu chuỗi dịch theo key của ngôn ngữ hiện tại
    public static string Get(string key)
    {
        return GetForLanguage(Language, key);
    }

    // Tra cứu chuỗi dịch theo mã ngôn ngữ chỉ định
    public static string GetForLanguage(string code, string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (!string.IsNullOrEmpty(code) && Tables.TryGetValue(code, out var dict) && dict.TryGetValue(key, out string val))
            return val;
        if (Tables.TryGetValue("vi", out var viDict) && viDict.TryGetValue(key, out string fallback))
            return fallback;
        return key;
    }
}
