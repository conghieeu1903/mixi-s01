using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Công cụ tạo kho âm thanh thủ tục (.wav) và đóng gói vào AudioCatalog cho toàn bộ dự án
public static class PrototypeAudioBuilder
{
    const int SampleRate = 44100;
    const string AudioRoot = "Assets/Audio";
    const string CatalogPath = "Assets/Resources/AudioCatalog.asset";

    // Mô tả thông số tổng hợp một hiệu ứng âm thanh ngắn
    struct SfxRecipe
    {
        public string id;
        public string category;
        public float duration;
        public float freqStart;
        public float freqEnd;
        public float noiseMix; // Tỷ lệ pha tạp âm trắng (0..1) để tạo tiếng gõ, bước chân, nước hoặc súng
        public float modRate; // Tần số điều chế biên độ/cao độ (Hz) để tạo tiếng rung hoặc chuông
        public int waveType; // 0 = Sine, 1 = Square/Retro, 2 = Saw, 3 = Triangle, 4 = Chord Arpeggio
        public float volume;

        public SfxRecipe(string id, string category, float duration, float f0, float f1, float noise, float mod, int wave, float vol = 0.8f)
        {
            this.id = id;
            this.category = category;
            this.duration = duration;
            this.freqStart = f0;
            this.freqEnd = f1;
            this.noiseMix = noise;
            this.modRate = mod;
            this.waveType = wave;
            this.volume = vol;
        }
    }

    // Mô tả thông số tổng hợp một bản nhạc nền lặp
    struct BgmRecipe
    {
        public string id;
        public float duration;
        public float bpm;
        public float[] scaleNotes; // Tần số các nốt nhạc trong âm giai của bài
        public float bassFreq;
        public float noiseLevel;
        public int waveStyle; // 0 = Chill Sine/Pad, 1 = Synth Saw, 2 = ASMR Pad
        public float volume;

        public BgmRecipe(string id, float duration, float bpm, float[] notes, float bass, float noise, int style, float vol = 0.5f)
        {
            this.id = id;
            this.duration = duration;
            this.bpm = bpm;
            this.scaleNotes = notes;
            this.bassFreq = bass;
            this.noiseLevel = noise;
            this.waveStyle = style;
            this.volume = vol;
        }
    }

    // Menu khởi tạo toàn bộ file .wav và cập nhật AudioCatalog.asset
    [MenuItem("Tools/Prototype/Build Audio Library")]
    public static void BuildAudioLibrary()
    {
        CleanupLegacySlotAudio();
        EnsureFolders();

        List<BgmRecipe> bgmList = GetBgmRecipes();
        List<SfxRecipe> ambienceList = GetAmbienceRecipes();
        List<SfxRecipe> sfxList = GetSfxRecipes();

        foreach (BgmRecipe bgm in bgmList)
        {
            string assetPath = $"{AudioRoot}/BGM/{bgm.id}.wav";
            float[] samples = SynthesizeBgm(bgm);
            WriteWavFile(assetPath, samples);
        }

        foreach (SfxRecipe amb in ambienceList)
        {
            string assetPath = $"{AudioRoot}/Ambience/{amb.id}.wav";
            float[] samples = SynthesizeAmbience(amb);
            WriteWavFile(assetPath, samples);
        }

        foreach (SfxRecipe sfx in sfxList)
        {
            string folder = $"{AudioRoot}/SFX/{sfx.category}";
            EnsureFolder(folder);
            string assetPath = $"{folder}/{sfx.id}.wav";
            float[] samples = SynthesizeSfx(sfx);
            WriteWavFile(assetPath, samples);
        }

        AssetDatabase.Refresh();

        AudioCatalog catalog = LoadOrCreateCatalog();
        catalog.music.Clear();
        catalog.ambience.Clear();
        catalog.sfx.Clear();

        foreach (BgmRecipe bgm in bgmList)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/BGM/{bgm.id}.wav");
            if (clip != null)
            {
                catalog.music.Add(new AudioCatalogEntry
                {
                    id = bgm.id,
                    category = "BGM",
                    clip = clip,
                    volume = bgm.volume
                });
            }
        }

        foreach (SfxRecipe amb in ambienceList)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/Ambience/{amb.id}.wav");
            if (clip != null)
            {
                catalog.ambience.Add(new AudioCatalogEntry
                {
                    id = amb.id,
                    category = "Ambience",
                    clip = clip,
                    volume = amb.volume
                });
            }
        }

        foreach (SfxRecipe sfx in sfxList)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/SFX/{sfx.category}/{sfx.id}.wav");
            if (clip != null)
            {
                catalog.sfx.Add(new AudioCatalogEntry
                {
                    id = sfx.id,
                    category = sfx.category,
                    clip = clip,
                    volume = sfx.volume
                });
            }
        }

        AddExistingClips(catalog);
        EditorUtility.SetDirty(catalog);
        EnsureSceneAudioManager(catalog);
        AssetDatabase.SaveAssets();

        Debug.Log($"[PrototypeAudioBuilder] Đã tạo kho âm thanh gồm {catalog.music.Count} BGM, {catalog.ambience.Count} Ambience và {catalog.sfx.Count} SFX tại {CatalogPath}");
    }

    // Các file âm thanh có sẵn trong dự án (nhạc mp3 thật, SFX từ bộ cũ) được đưa vào kho bên cạnh âm tự tổng hợp
    static readonly (string id, string group, string category, string path, float volume)[] ExistingClips =
    {
        ("bgm_menu_theme", "music", "BGM", "Assets/Audio/BGM/viacheslavstarostin-funny-cartoon-music-412765.mp3", 0.35f),
        ("bgm_home_theme", "music", "BGM", "Assets/Audio/BGM/maksymmalko-funny-cartoon-421644.mp3", 0.3f),
        ("bgm_casino", "music", "BGM", "Assets/Audio/BGM/bgm_synth_shop.wav", 0.4f),
        ("level_up", "sfx", "UI", "Assets/Audio/SFX/sfx_level_up.wav", 0.8f),
        ("skill_unlock", "sfx", "UI", "Assets/Audio/SFX/sfx_item_unlock.wav", 0.8f),
        ("purchase", "sfx", "UI", "Assets/Audio/SFX/sfx_purchase.wav", 0.8f),
        ("purchase_fail", "sfx", "UI", "Assets/Audio/SFX/sfx_purchase_fail.wav", 0.8f),
        ("coin_gain", "sfx", "UI", "Assets/Audio/SFX/sfx_coin_gain.wav", 0.75f)
    };

    // Gán mixer và thêm các file âm thanh có sẵn vào kho mà không tổng hợp lại toàn bộ
    [MenuItem("Tools/Audio/Sync Existing Clips To Catalog")]
    public static void SyncExistingClips()
    {
        AudioCatalog catalog = LoadOrCreateCatalog();
        AddExistingClips(catalog);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PrototypeAudioBuilder] Kho âm thanh: {catalog.music.Count} BGM, {catalog.ambience.Count} Ambience, {catalog.sfx.Count} SFX");
    }

    static void AddExistingClips(AudioCatalog catalog)
    {
        catalog.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioMixer.mixer");
        foreach (var (id, group, category, path, volume) in ExistingClips)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[PrototypeAudioBuilder] Không tìm thấy {path}");
                continue;
            }
            List<AudioCatalogEntry> list = group == "music" ? catalog.music : group == "ambience" ? catalog.ambience : catalog.sfx;
            list.RemoveAll(e => e.id == id);
            list.Add(new AudioCatalogEntry { id = id, category = category, clip = clip, volume = volume });
        }
    }

    // Xóa các file âm thanh mẫu của SlotGame cũ không còn thuộc dự án
    static void CleanupLegacySlotAudio()
    {
        string[] legacyPaths =
        {
            "Assets/Audio/BGM/slot_bgm.wav",
            "Assets/Audio/SFX/spin_click.wav",
            "Assets/Audio/SFX/reel_stop.wav",
            "Assets/Audio/SFX/win_small.wav",
            "Assets/Audio/SFX/win_big.wav"
        };

        foreach (string path in legacyPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
                AssetDatabase.DeleteAsset(path);
        }
    }

    // Tạo cây thư mục lưu trữ âm thanh và thư mục Resources
    static void EnsureFolders()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(AudioRoot);
        EnsureFolder($"{AudioRoot}/BGM");
        EnsureFolder($"{AudioRoot}/Ambience");
        EnsureFolder($"{AudioRoot}/SFX");
    }

    // Tạo thư mục con trong Assets nếu chưa tồn tại
    static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // Tải hoặc tạo mới file AudioCatalog.asset trong Assets/Resources
    static AudioCatalog LoadOrCreateCatalog()
    {
        AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        return catalog;
    }

    // Đảm bảo trong scene hiện tại có đối tượng AudioManager được gán sẵn AudioCatalog và AudioMixer
    static void EnsureSceneAudioManager(AudioCatalog catalog)
    {
        AudioManager mgr = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (mgr == null)
        {
            GameObject go = new GameObject("AudioManager");
            mgr = go.AddComponent<AudioManager>();
        }

        SerializedObject so = new SerializedObject(mgr);
        SerializedProperty catalogProp = so.FindProperty("catalog");
        if (catalogProp != null)
            catalogProp.objectReferenceValue = catalog;

        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioMixer.mixer");
        if (mixer != null)
        {
            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            AudioMixerGroup[] bgmGroups = mixer.FindMatchingGroups("BGM");
            AudioMixerGroup[] uiGroups = mixer.FindMatchingGroups("UI");
            SerializedProperty sfxProp = so.FindProperty("sfxGroup");
            SerializedProperty musicProp = so.FindProperty("musicGroup");
            SerializedProperty uiProp = so.FindProperty("uiGroup");
            if (sfxProp != null && sfxGroups != null && sfxGroups.Length > 0)
                sfxProp.objectReferenceValue = sfxGroups[0];
            if (musicProp != null && bgmGroups != null && bgmGroups.Length > 0)
                musicProp.objectReferenceValue = bgmGroups[0];
            if (uiProp != null && uiGroups != null && uiGroups.Length > 0)
                uiProp.objectReferenceValue = uiGroups[0];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mgr);
    }

    // Danh sách công thức tổng hợp 9 bản nhạc nền (BGM)
    static List<BgmRecipe> GetBgmRecipes()
    {
        return new List<BgmRecipe>
        {
            new BgmRecipe("bgm_menu", 6.0f, 96f, new[] { 261.63f, 329.63f, 392.00f, 493.88f, 523.25f }, 130.81f, 0.01f, 0, 0.50f),
            new BgmRecipe("bgm_chill", 6.0f, 84f, new[] { 220.00f, 261.63f, 329.63f, 392.00f, 440.00f }, 110.00f, 0.02f, 0, 0.50f),
            new BgmRecipe("bgm_peaceful", 6.0f, 72f, new[] { 293.66f, 349.23f, 440.00f, 523.25f, 587.33f }, 146.83f, 0.01f, 0, 0.45f),
            new BgmRecipe("bgm_cyberpunk", 6.0f, 124f, new[] { 196.00f, 233.08f, 293.66f, 349.23f, 392.00f }, 98.00f, 0.03f, 1, 0.55f),
            new BgmRecipe("bgm_asmr", 6.0f, 60f, new[] { 261.63f, 311.13f, 392.00f, 466.16f }, 130.81f, 0.05f, 2, 0.40f),
            new BgmRecipe("bgm_downtown", 6.0f, 105f, new[] { 246.94f, 293.66f, 369.99f, 440.00f, 493.88f }, 123.47f, 0.02f, 0, 0.50f),
            new BgmRecipe("bgm_nightclub", 6.0f, 128f, new[] { 220.00f, 261.63f, 329.63f, 440.00f }, 110.00f, 0.04f, 1, 0.60f),
            new BgmRecipe("bgm_action", 5.0f, 136f, new[] { 164.81f, 196.00f, 220.00f, 246.94f, 329.63f }, 82.41f, 0.04f, 1, 0.55f),
            new BgmRecipe("bgm_race", 5.0f, 144f, new[] { 293.66f, 349.23f, 392.00f, 440.00f, 587.33f }, 146.83f, 0.04f, 1, 0.55f)
        };
    }

    // Danh sách công thức tổng hợp 6 âm thanh môi trường (Ambience)
    static List<SfxRecipe> GetAmbienceRecipes()
    {
        return new List<SfxRecipe>
        {
            new SfxRecipe("amb_room", "Ambience", 4.0f, 85f, 90f, 0.25f, 0.5f, 0, 0.35f),
            new SfxRecipe("amb_pc_fan", "Ambience", 4.0f, 140f, 142f, 0.45f, 2.0f, 2, 0.40f),
            new SfxRecipe("amb_street", "Ambience", 4.0f, 110f, 160f, 0.55f, 0.8f, 0, 0.40f),
            new SfxRecipe("amb_nature", "Ambience", 4.0f, 900f, 1400f, 0.35f, 6.0f, 0, 0.38f),
            new SfxRecipe("amb_rain", "Ambience", 4.0f, 400f, 420f, 0.85f, 1.5f, 0, 0.45f),
            new SfxRecipe("amb_casino", "Ambience", 4.0f, 520f, 880f, 0.30f, 8.0f, 4, 0.42f)
        };
    }

    // Danh sách công thức tổng hợp toàn bộ hiệu ứng âm thanh (SFX) chia theo 9 danh mục
    static List<SfxRecipe> GetSfxRecipes()
    {
        return new List<SfxRecipe>
        {
            // Danh mục UI & Điện thoại (20 clip)
            new SfxRecipe("ui_click", "UI", 0.08f, 950f, 620f, 0.05f, 0f, 0, 0.75f),
            new SfxRecipe("ui_hover", "UI", 0.05f, 720f, 840f, 0.02f, 0f, 0, 0.50f),
            new SfxRecipe("ui_select", "UI", 0.14f, 660f, 990f, 0.02f, 0f, 3, 0.80f),
            new SfxRecipe("ui_cancel", "UI", 0.14f, 520f, 310f, 0.04f, 0f, 3, 0.75f),
            new SfxRecipe("ui_tab", "UI", 0.07f, 800f, 1050f, 0.03f, 0f, 0, 0.65f),
            new SfxRecipe("ui_toggle_on", "UI", 0.10f, 580f, 880f, 0.02f, 0f, 0, 0.70f),
            new SfxRecipe("ui_toggle_off", "UI", 0.10f, 880f, 520f, 0.02f, 0f, 0, 0.70f),
            new SfxRecipe("ui_popup_open", "UI", 0.16f, 400f, 820f, 0.05f, 0f, 3, 0.75f),
            new SfxRecipe("ui_popup_close", "UI", 0.15f, 780f, 360f, 0.05f, 0f, 3, 0.70f),
            new SfxRecipe("ui_notification", "UI", 0.28f, 880f, 1320f, 0.01f, 12f, 4, 0.80f),
            new SfxRecipe("ui_alert", "UI", 0.32f, 920f, 640f, 0.05f, 18f, 1, 0.85f),
            new SfxRecipe("ui_error", "UI", 0.26f, 240f, 160f, 0.10f, 20f, 1, 0.85f),
            new SfxRecipe("ui_bsod", "UI", 0.65f, 180f, 175f, 0.25f, 30f, 1, 0.90f),
            new SfxRecipe("ui_success", "UI", 0.35f, 523.25f, 1046.5f, 0.01f, 10f, 4, 0.85f),
            new SfxRecipe("cash_beep", "UI", 0.24f, 1200f, 1800f, 0.04f, 16f, 4, 0.85f),
            new SfxRecipe("store_bell", "UI", 0.42f, 1046.5f, 1318.5f, 0.02f, 8f, 4, 0.80f),
            new SfxRecipe("phone_tap", "UI", 0.06f, 1100f, 760f, 0.04f, 0f, 0, 0.65f),
            new SfxRecipe("phone_open", "UI", 0.18f, 440f, 880f, 0.06f, 0f, 3, 0.75f),
            new SfxRecipe("phone_close", "UI", 0.16f, 840f, 420f, 0.06f, 0f, 3, 0.75f),
            new SfxRecipe("phone_shutter", "UI", 0.14f, 1400f, 400f, 0.55f, 0f, 1, 0.80f),

            // Danh mục Stream & Donate (17 clip)
            new SfxRecipe("stream_start", "Stream", 0.45f, 440f, 880f, 0.02f, 10f, 4, 0.85f),
            new SfxRecipe("stream_stop", "Stream", 0.40f, 784f, 330f, 0.02f, 8f, 3, 0.80f),
            new SfxRecipe("donate_notify", "Stream", 0.48f, 659.25f, 1318.5f, 0.02f, 14f, 4, 0.90f),
            new SfxRecipe("asmr_completed", "Stream", 0.38f, 528f, 960f, 0.12f, 6f, 0, 0.75f),
            new SfxRecipe("dance_completed", "Stream", 0.42f, 587.33f, 1174.6f, 0.03f, 15f, 4, 0.85f),
            new SfxRecipe("chat_completed", "Stream", 0.25f, 740f, 1100f, 0.02f, 8f, 3, 0.80f),
            new SfxRecipe("card_select", "Stream", 0.12f, 680f, 920f, 0.15f, 0f, 3, 0.75f),
            new SfxRecipe("donate_usb", "Stream", 0.32f, 520f, 780f, 0.02f, 9f, 1, 0.85f),
            new SfxRecipe("donate_knock", "Stream", 0.26f, 160f, 95f, 0.35f, 12f, 0, 0.90f),
            new SfxRecipe("donate_oof", "Stream", 0.28f, 310f, 180f, 0.08f, 5f, 2, 0.85f),
            new SfxRecipe("donate_woof", "Stream", 0.26f, 420f, 210f, 0.22f, 14f, 2, 0.85f),
            new SfxRecipe("donate_winerror", "Stream", 0.34f, 380f, 190f, 0.10f, 22f, 1, 0.85f),
            new SfxRecipe("donate_boink", "Stream", 0.30f, 260f, 780f, 0.02f, 18f, 0, 0.85f),
            new SfxRecipe("donate_sus", "Stream", 0.45f, 220f, 207.65f, 0.05f, 6f, 2, 0.85f),
            new SfxRecipe("donate_huh", "Stream", 0.30f, 280f, 490f, 0.06f, 4f, 3, 0.85f),
            new SfxRecipe("donate_laugh", "Stream", 0.45f, 620f, 420f, 0.10f, 16f, 3, 0.85f),
            new SfxRecipe("donate_meow", "Stream", 0.36f, 720f, 540f, 0.05f, 6f, 3, 0.85f),

            // Danh mục Di chuyển (13 clip)
            new SfxRecipe("footstep_wood_01", "Movement", 0.11f, 170f, 85f, 0.40f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_wood_02", "Movement", 0.11f, 185f, 92f, 0.42f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_wood_03", "Movement", 0.11f, 160f, 80f, 0.38f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_wood_04", "Movement", 0.11f, 195f, 96f, 0.40f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_concrete_01", "Movement", 0.10f, 260f, 120f, 0.60f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_concrete_02", "Movement", 0.10f, 280f, 130f, 0.58f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_concrete_03", "Movement", 0.10f, 245f, 115f, 0.62f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_concrete_04", "Movement", 0.10f, 290f, 135f, 0.60f, 0f, 0, 0.65f),
            new SfxRecipe("footstep_grass_01", "Movement", 0.13f, 520f, 280f, 0.82f, 0f, 0, 0.60f),
            new SfxRecipe("footstep_grass_02", "Movement", 0.13f, 560f, 300f, 0.84f, 0f, 0, 0.60f),
            new SfxRecipe("sit_down", "Movement", 0.22f, 240f, 120f, 0.45f, 0f, 0, 0.70f),
            new SfxRecipe("get_up", "Movement", 0.20f, 140f, 260f, 0.40f, 0f, 0, 0.70f),
            new SfxRecipe("jump", "Movement", 0.18f, 210f, 460f, 0.25f, 0f, 3, 0.75f),

            // Danh mục Tương tác trong nhà (20 clip)
            new SfxRecipe("item_pickup", "Home", 0.14f, 420f, 760f, 0.15f, 0f, 3, 0.75f),
            new SfxRecipe("item_drop", "Home", 0.16f, 320f, 120f, 0.45f, 0f, 0, 0.75f),
            new SfxRecipe("door_open", "Home", 0.32f, 220f, 360f, 0.25f, 8f, 2, 0.75f),
            new SfxRecipe("door_close", "Home", 0.24f, 260f, 95f, 0.45f, 0f, 0, 0.80f),
            new SfxRecipe("door_knock", "Home", 0.28f, 180f, 105f, 0.35f, 10f, 0, 0.85f),
            new SfxRecipe("door_bell", "Home", 0.50f, 784f, 659.25f, 0.02f, 4f, 4, 0.85f),
            new SfxRecipe("light_switch", "Home", 0.07f, 1300f, 850f, 0.20f, 0f, 1, 0.75f),
            new SfxRecipe("fridge_open", "Home", 0.24f, 190f, 310f, 0.30f, 0f, 0, 0.70f),
            new SfxRecipe("fridge_close", "Home", 0.22f, 280f, 130f, 0.35f, 0f, 0, 0.75f),
            new SfxRecipe("eat_chew", "Home", 0.32f, 260f, 180f, 0.65f, 11f, 0, 0.75f),
            new SfxRecipe("drink_sip", "Home", 0.34f, 340f, 580f, 0.35f, 9f, 0, 0.75f),
            new SfxRecipe("shower_water", "Home", 0.65f, 600f, 620f, 0.90f, 4f, 0, 0.70f),
            new SfxRecipe("toilet_flush", "Home", 0.75f, 480f, 160f, 0.85f, 6f, 0, 0.80f),
            new SfxRecipe("pill_bottle", "Home", 0.26f, 950f, 1200f, 0.70f, 20f, 0, 0.75f),
            new SfxRecipe("sleep_bed", "Home", 0.38f, 240f, 150f, 0.50f, 4f, 0, 0.70f),
            new SfxRecipe("mop_clean", "Home", 0.32f, 420f, 680f, 0.75f, 7f, 0, 0.75f),
            new SfxRecipe("clean_shine", "Home", 0.36f, 1046.5f, 2093f, 0.03f, 16f, 4, 0.80f),
            new SfxRecipe("wall_paint", "Home", 0.30f, 380f, 520f, 0.80f, 5f, 0, 0.75f),
            new SfxRecipe("box_open", "Home", 0.28f, 300f, 540f, 0.65f, 0f, 2, 0.80f),
            new SfxRecipe("delivery_complete", "Home", 0.38f, 587.33f, 880f, 0.02f, 10f, 4, 0.85f),

            // Danh mục Máy tính & Linh kiện (10 clip)
            new SfxRecipe("pc_power_on", "PC", 0.35f, 220f, 660f, 0.12f, 6f, 3, 0.80f),
            new SfxRecipe("pc_boot", "PC", 0.55f, 440f, 880f, 0.05f, 8f, 4, 0.80f),
            new SfxRecipe("pc_shutdown", "PC", 0.42f, 660f, 180f, 0.10f, 4f, 3, 0.75f),
            new SfxRecipe("pc_cover_open", "PC", 0.24f, 280f, 420f, 0.45f, 0f, 2, 0.75f),
            new SfxRecipe("pc_cover_close", "PC", 0.22f, 400f, 180f, 0.50f, 0f, 2, 0.80f),
            new SfxRecipe("pc_part_install", "PC", 0.16f, 520f, 940f, 0.25f, 0f, 1, 0.80f),
            new SfxRecipe("pc_part_remove", "PC", 0.16f, 840f, 420f, 0.25f, 0f, 1, 0.75f),
            new SfxRecipe("typing_key", "PC", 0.05f, 1250f, 780f, 0.35f, 0f, 1, 0.65f),
            new SfxRecipe("mouse_click", "PC", 0.05f, 1500f, 950f, 0.20f, 0f, 1, 0.68f),
            new SfxRecipe("mining_beep", "PC", 0.18f, 980f, 1420f, 0.02f, 15f, 1, 0.75f),

            // Danh mục Casino & Giải trí (11 clip)
            new SfxRecipe("slot_start", "Casino", 0.28f, 440f, 880f, 0.08f, 12f, 4, 0.80f),
            new SfxRecipe("slot_roll", "Casino", 0.35f, 520f, 780f, 0.15f, 24f, 1, 0.75f),
            new SfxRecipe("slot_stop", "Casino", 0.14f, 380f, 190f, 0.25f, 0f, 1, 0.85f),
            new SfxRecipe("slot_win", "Casino", 0.60f, 523.25f, 1567.98f, 0.03f, 18f, 4, 0.90f),
            new SfxRecipe("spin_wheel", "Casino", 0.45f, 600f, 320f, 0.20f, 20f, 1, 0.80f),
            new SfxRecipe("scratch_card", "Casino", 0.26f, 800f, 1200f, 0.80f, 16f, 2, 0.75f),
            new SfxRecipe("case_open", "Casino", 0.40f, 330f, 660f, 0.15f, 10f, 3, 0.85f),
            new SfxRecipe("case_reveal", "Casino", 0.55f, 659.25f, 1318.5f, 0.04f, 14f, 4, 0.90f),
            new SfxRecipe("card_deal", "Casino", 0.10f, 900f, 500f, 0.65f, 0f, 0, 0.75f),
            new SfxRecipe("chip_bet", "Casino", 0.12f, 1600f, 1100f, 0.18f, 0f, 3, 0.80f),
            new SfxRecipe("drop_gems", "Casino", 0.35f, 1100f, 1900f, 0.05f, 22f, 4, 0.85f),

            // Danh mục Phương tiện (9 clip)
            new SfxRecipe("car_start", "Vehicles", 0.55f, 95f, 220f, 0.40f, 18f, 2, 0.85f),
            new SfxRecipe("car_engine_idle", "Vehicles", 0.60f, 110f, 115f, 0.35f, 22f, 2, 0.70f),
            new SfxRecipe("car_door_open", "Vehicles", 0.22f, 240f, 360f, 0.35f, 0f, 2, 0.80f),
            new SfxRecipe("car_door_close", "Vehicles", 0.24f, 220f, 85f, 0.50f, 0f, 0, 0.85f),
            new SfxRecipe("car_brake", "Vehicles", 0.45f, 1400f, 950f, 0.55f, 14f, 2, 0.80f),
            new SfxRecipe("car_horn", "Vehicles", 0.36f, 415f, 415f, 0.05f, 0f, 2, 0.85f),
            new SfxRecipe("motorbike_start", "Vehicles", 0.50f, 130f, 310f, 0.38f, 24f, 2, 0.85f),
            new SfxRecipe("bicycle_bell", "Vehicles", 0.38f, 1567.98f, 1567.98f, 0.04f, 26f, 4, 0.80f),
            new SfxRecipe("bicycle_pedal", "Vehicles", 0.20f, 420f, 520f, 0.55f, 12f, 1, 0.65f),

            // Danh mục Minigames trong PC (10 clip)
            new SfxRecipe("gun_pistol", "Minigames", 0.20f, 480f, 90f, 0.75f, 0f, 1, 0.85f),
            new SfxRecipe("gun_deagle", "Minigames", 0.28f, 360f, 65f, 0.80f, 0f, 2, 0.90f),
            new SfxRecipe("gun_reload", "Minigames", 0.26f, 620f, 980f, 0.40f, 14f, 1, 0.80f),
            new SfxRecipe("gun_empty", "Minigames", 0.08f, 1200f, 700f, 0.30f, 0f, 1, 0.75f),
            new SfxRecipe("player_damage", "Minigames", 0.22f, 280f, 110f, 0.45f, 12f, 2, 0.85f),
            new SfxRecipe("victory_fps", "Minigames", 0.55f, 523.25f, 1046.5f, 0.02f, 12f, 4, 0.85f),
            new SfxRecipe("defeat_fps", "Minigames", 0.55f, 392f, 196f, 0.06f, 8f, 2, 0.85f),
            new SfxRecipe("explosion_toon", "Minigames", 0.50f, 220f, 45f, 0.88f, 10f, 2, 0.90f),
            new SfxRecipe("pogo_jump", "Minigames", 0.26f, 240f, 680f, 0.08f, 16f, 3, 0.80f),
            new SfxRecipe("clicker_tap", "Minigames", 0.07f, 1020f, 1380f, 0.05f, 0f, 0, 0.75f),

            // Danh mục NPC & Thú cưng (9 clip)
            new SfxRecipe("npc_greet_01", "NPC_Pets", 0.28f, 340f, 480f, 0.05f, 8f, 3, 0.80f),
            new SfxRecipe("npc_greet_02", "NPC_Pets", 0.28f, 420f, 560f, 0.05f, 9f, 3, 0.80f),
            new SfxRecipe("npc_agree", "NPC_Pets", 0.24f, 380f, 520f, 0.04f, 6f, 3, 0.80f),
            new SfxRecipe("npc_ask", "NPC_Pets", 0.26f, 330f, 540f, 0.04f, 4f, 3, 0.80f),
            new SfxRecipe("npc_joy", "NPC_Pets", 0.34f, 480f, 760f, 0.03f, 14f, 4, 0.85f),
            new SfxRecipe("npc_angry", "NPC_Pets", 0.32f, 220f, 140f, 0.20f, 18f, 2, 0.85f),
            new SfxRecipe("cat_meow", "NPC_Pets", 0.35f, 680f, 510f, 0.05f, 7f, 3, 0.80f),
            new SfxRecipe("dog_bark", "NPC_Pets", 0.24f, 380f, 190f, 0.25f, 12f, 2, 0.85f),
            new SfxRecipe("taming_success", "NPC_Pets", 0.42f, 587.33f, 1174.66f, 0.02f, 12f, 4, 0.85f)
        };
    }

    // Tổng hợp mẫu âm thanh cho một SFX dựa trên công thức tần số, dạng sóng và tạp âm
    static float[] SynthesizeSfx(SfxRecipe r)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(r.duration * SampleRate));
        float[] data = new float[count];
        System.Random rng = new System.Random(r.id.GetHashCode());
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = i / (float)count;

            float env = u < 0.08f ? (u / 0.08f) : Mathf.Pow(1f - ((u - 0.08f) / 0.92f), 1.4f);
            float freq = Mathf.Lerp(r.freqStart, r.freqEnd, u);
            if (r.modRate > 0f)
                freq *= 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * r.modRate * t);

            phase += 2f * Mathf.PI * freq / SampleRate;
            float tone = SampleWave(phase, r.waveType, u);
            float noise = ((float)rng.NextDouble() * 2f - 1f);

            float combined = Mathf.Lerp(tone, noise, r.noiseMix);
            if (r.modRate > 0f)
                combined *= 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * r.modRate * t);

            data[i] = Mathf.Clamp(combined * env * r.volume, -0.95f, 0.95f);
        }

        return data;
    }

    // Tổng hợp mẫu âm thanh môi trường có chu kỳ mượt để lặp không bị khựng
    static float[] SynthesizeAmbience(SfxRecipe r)
    {
        int count = Mathf.Max(SampleRate, Mathf.RoundToInt(r.duration * SampleRate));
        float[] data = new float[count];
        System.Random rng = new System.Random(r.id.GetHashCode());
        float filterState = 0f;
        float alpha = Mathf.Clamp(r.freqStart / 2000f, 0.02f, 0.45f);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float rawNoise = (float)rng.NextDouble() * 2f - 1f;
            filterState = Mathf.Lerp(filterState, rawNoise, alpha);

            float hum = Mathf.Sin(2f * Mathf.PI * r.freqStart * t) * 0.3f
                      + Mathf.Sin(2f * Mathf.PI * r.freqEnd * t) * 0.2f;
            float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * r.modRate * t);
            float sample = Mathf.Lerp(hum, filterState, r.noiseMix) * lfo * r.volume;

            float fadeEdge = 0.05f;
            float u = i / (float)count;
            if (u < fadeEdge) sample *= u / fadeEdge;
            else if (u > 1f - fadeEdge) sample *= (1f - u) / fadeEdge;

            data[i] = Mathf.Clamp(sample, -0.9f, 0.9f);
        }

        return data;
    }

    // Tổng hợp một bản nhạc nền lặp gồm hợp âm nền, nốt giai điệu và nhịp trống cơ bản
    static float[] SynthesizeBgm(BgmRecipe r)
    {
        int count = Mathf.Max(SampleRate, Mathf.RoundToInt(r.duration * SampleRate));
        float[] data = new float[count];
        System.Random rng = new System.Random(r.id.GetHashCode());
        float stepSeconds = 60f / Mathf.Max(40f, r.bpm) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            int step = Mathf.FloorToInt(t / stepSeconds);
            float stepProgress = (t % stepSeconds) / stepSeconds;

            float noteFreq = r.scaleNotes[step % r.scaleNotes.Length];
            float noteEnv = Mathf.Exp(-3.2f * stepProgress);

            float lead;
            if (r.waveStyle == 1)
                lead = SampleWave(2f * Mathf.PI * noteFreq * t, 2, stepProgress) * 0.32f * noteEnv;
            else if (r.waveStyle == 2)
                lead = Mathf.Sin(2f * Mathf.PI * noteFreq * t) * 0.22f * Mathf.Sin(Mathf.PI * stepProgress);
            else
                lead = (Mathf.Sin(2f * Mathf.PI * noteFreq * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * noteFreq * t)) * 0.26f * noteEnv;

            float bassFreq = (step / 4) % 2 == 0 ? r.bassFreq : r.bassFreq * 1.25f;
            float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.30f;

            float kick = 0f;
            if (step % 2 == 0 && stepProgress < 0.25f)
            {
                float kEnv = 1f - (stepProgress / 0.25f);
                kick = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 45f, stepProgress * 4f) * t) * 0.25f * kEnv;
            }

            float noise = ((float)rng.NextDouble() * 2f - 1f) * r.noiseLevel;
            float sample = (lead + bass + kick + noise) * r.volume;

            float u = i / (float)count;
            if (u < 0.03f) sample *= u / 0.03f;
            else if (u > 0.97f) sample *= (1f - u) / 0.03f;

            data[i] = Mathf.Clamp(sample, -0.9f, 0.9f);
        }

        return data;
    }

    // Tính giá trị biên độ tại pha cho trước theo loại sóng âm
    static float SampleWave(float phase, int waveType, float progress)
    {
        switch (waveType)
        {
            case 1:
                return Mathf.Sin(phase) >= 0f ? 0.8f : -0.8f;
            case 2:
                float norm = (phase / (2f * Mathf.PI)) % 1f;
                if (norm < 0f) norm += 1f;
                return (norm * 2f - 1f) * 0.85f;
            case 3:
                return Mathf.Asin(Mathf.Sin(phase)) * (2f / Mathf.PI);
            case 4:
                float mult = progress < 0.33f ? 1f : (progress < 0.66f ? 1.25f : 1.5f);
                return (Mathf.Sin(phase * mult) + 0.5f * Mathf.Sin(phase * mult * 2f)) * 0.7f;
            default:
                return Mathf.Sin(phase);
        }
    }

    // Ghi mảng mẫu âm thanh float (-1..1) ra file chuẩn PCM 16-bit mono .wav
    static void WriteWavFile(string assetPath, float[] samples)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        int sampleCount = samples.Length;
        int byteRate = SampleRate * 2;
        int dataSize = sampleCount * 2;

        using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(SampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            for (int i = 0; i < sampleCount; i++)
            {
                short pcm = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), short.MinValue, short.MaxValue);
                bw.Write(pcm);
            }
        }
    }
}
