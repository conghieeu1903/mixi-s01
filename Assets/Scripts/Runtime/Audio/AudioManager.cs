using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Phát SFX và nhạc nền theo tên clip, dùng chung cho mọi scene
public class AudioManager : MonoBehaviour
{
    [Serializable]
    struct Entry
    {
        public string id; // Tên gọi dùng khi phát, ví dụ "ui_click"
        public AudioClip clip; // Clip âm thanh
        [Range(0f, 1f)] public float volume; // Âm lượng riêng của clip
    }

    public static AudioManager Instance { get; private set; } // Bản duy nhất của AudioManager

    [SerializeField] AudioMixerGroup sfxGroup; // Nhóm mixer cho SFX
    [SerializeField] AudioMixerGroup musicGroup; // Nhóm mixer cho nhạc nền
    [SerializeField] Entry[] sfx; // Danh sách SFX phát một lần
    [SerializeField] Entry[] music; // Danh sách nhạc nền phát lặp
    [SerializeField] int voices = 8; // Số nguồn phát SFX chạy đồng thời
    [SerializeField] float musicFadeSeconds = 0.8f; // Thời gian chuyển nhạc (giây)

    readonly Dictionary<string, Entry> sfxById = new(); // Tra cứu SFX theo tên
    readonly Dictionary<string, Entry> musicById = new(); // Tra cứu nhạc theo tên
    AudioSource[] pool; // Các nguồn phát SFX
    AudioSource musicSource; // Nguồn phát nhạc nền
    int nextVoice; // Nguồn SFX sẽ dùng tiếp theo
    string currentMusic; // Tên bản nhạc đang phát

    // Giữ một bản duy nhất, dựng bảng tra cứu và các nguồn phát
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        foreach (Entry e in sfx) if (e.clip != null) sfxById[e.id] = e;
        foreach (Entry e in music) if (e.clip != null) musicById[e.id] = e;
        pool = new AudioSource[voices];
        for (int i = 0; i < voices; i++) pool[i] = CreateSource(sfxGroup, false);
        musicSource = CreateSource(musicGroup, true);
    }

    // Phát một SFX theo tên, pitch tùy chọn
    public void PlaySfx(string id, float pitch = 1f)
    {
        if (!sfxById.TryGetValue(id, out Entry e)) return;
        AudioSource source = pool[nextVoice];
        nextVoice = (nextVoice + 1) % pool.Length;
        source.pitch = pitch;
        source.PlayOneShot(e.clip, e.volume > 0f ? e.volume : 1f);
    }

    // Chuyển sang một bản nhạc nền, bỏ qua nếu đang phát đúng bản đó
    public void PlayMusic(string id)
    {
        if (id == currentMusic || !musicById.TryGetValue(id, out Entry e)) return;
        currentMusic = id;
        StopAllCoroutines();
        StartCoroutine(SwapMusic(e));
    }

    // Nhỏ dần bản cũ, đổi clip rồi to dần bản mới
    System.Collections.IEnumerator SwapMusic(Entry next)
    {
        float target = next.volume > 0f ? next.volume : 0.6f;
        float from = musicSource.volume;
        for (float t = 0f; t < musicFadeSeconds && musicSource.isPlaying; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(from, 0f, t / musicFadeSeconds);
            yield return null;
        }
        musicSource.clip = next.clip;
        musicSource.volume = 0f;
        musicSource.Play();
        for (float t = 0f; t < musicFadeSeconds; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, target, t / musicFadeSeconds);
            yield return null;
        }
        musicSource.volume = target;
    }

    // Thêm một AudioSource 2D xuất ra nhóm mixer đã chọn
    AudioSource CreateSource(AudioMixerGroup group, bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = group;
        return source;
    }
}
