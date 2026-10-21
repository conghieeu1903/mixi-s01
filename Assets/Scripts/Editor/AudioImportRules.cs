using UnityEditor;
using UnityEngine;

// Tự áp dụng cấu hình import tối ưu cho âm thanh trong Assets/Audio theo thư mục:
// - BGM: Streaming + Vorbis, chỉ đọc dần từ đĩa nên gần như không tốn RAM
// - Ambience: Compressed In Memory + Vorbis + mono, giữ nén trong RAM vì lặp liên tục
// - SFX: Decompress On Load + ADPCM + mono + preload, phát tức thì không khựng và tốn ít CPU
public class AudioImportRules : AssetPostprocessor
{
    const string AudioRoot = "Assets/Audio/";

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(AudioRoot)) return;
        AudioImporter importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;

        if (assetPath.Contains("/BGM/"))
        {
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            settings.preloadAudioData = false;
            importer.forceToMono = false;
            importer.loadInBackground = true;
        }
        else if (assetPath.Contains("/Ambience/"))
        {
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.5f;
            settings.preloadAudioData = true;
            importer.forceToMono = true;
            importer.loadInBackground = true;
        }
        else
        {
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.preloadAudioData = true;
            importer.forceToMono = true;
            importer.loadInBackground = false;
        }

        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        importer.defaultSampleSettings = settings;
    }

    // Import lại toàn bộ âm thanh để áp dụng cấu hình cho các file đã có sẵn
    [MenuItem("Tools/Audio/Reapply Import Settings")]
    public static void ReapplyAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot.TrimEnd('/') });
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        Debug.Log($"[AudioImportRules] Đã import lại {guids.Length} file âm thanh");
    }
}
