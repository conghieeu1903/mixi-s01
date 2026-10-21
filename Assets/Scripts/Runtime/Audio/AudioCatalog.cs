using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Một mục âm thanh trong kho dữ liệu kèm tên định danh và nhóm phân loại
[Serializable]
public struct AudioCatalogEntry
{
    public string id;
    public string category;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

// Kho dữ liệu chứa toàn bộ nhạc nền, âm môi trường và hiệu ứng âm thanh của dự án
[CreateAssetMenu(menuName = "StreamerSim/Audio Catalog", fileName = "AudioCatalog")]
public class AudioCatalog : ScriptableObject
{
    // Mixer dùng chung, để AudioManager tự tạo lúc chạy vẫn tìm được nhóm BGM, SFX, UI
    public AudioMixer mixer;

    public List<AudioCatalogEntry> music = new List<AudioCatalogEntry>();
    public List<AudioCatalogEntry> ambience = new List<AudioCatalogEntry>();
    public List<AudioCatalogEntry> sfx = new List<AudioCatalogEntry>();
}
