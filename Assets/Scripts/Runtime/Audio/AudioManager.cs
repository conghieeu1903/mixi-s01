using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Quản lý phát SFX, nhạc nền và âm thanh môi trường theo mã định danh dùng chung toàn dự án.
// Tối ưu hiệu năng: dùng lại pool AudioSource có sẵn (không tạo GameObject khi phát), giới hạn số lần phát chồng
// cùng một âm, bỏ qua âm 3D ở quá xa và để mixer chỉnh âm lượng theo nhóm.
public class AudioManager : MonoBehaviour
{
    [Serializable]
    struct Entry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    // Một nhóm kênh phát dùng lại, hết kênh rảnh thì chiếm kênh phát lâu nhất
    sealed class VoicePool
    {
        readonly AudioSource[] sources;
        readonly string[] ids;
        readonly float[] startTimes;

        public VoicePool(AudioSource[] sources)
        {
            this.sources = sources;
            ids = new string[sources.Length];
            startTimes = new float[sources.Length];
        }

        public AudioSource Rent(string id)
        {
            int pick = 0;
            float oldest = float.MaxValue;
            for (int i = 0; i < sources.Length; i++)
            {
                if (!sources[i].isPlaying)
                {
                    pick = i;
                    break;
                }
                if (startTimes[i] < oldest)
                {
                    oldest = startTimes[i];
                    pick = i;
                }
            }
            ids[pick] = id;
            startTimes[pick] = Time.unscaledTime;
            return sources[pick];
        }

        // Số kênh đang phát cùng một âm
        public int CountPlaying(string id)
        {
            int count = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].isPlaying && string.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        public void StopAll()
        {
            for (int i = 0; i < sources.Length; i++)
                sources[i].Stop();
        }
    }

    const float SameClipCooldown = 0.05f; // Cùng một âm không phát lại trong khoảng này, tránh dồn tiếng khi bấm liên tục
    const int MaxSameClipVoices = 3; // Số lần phát chồng tối đa của cùng một âm

    static AudioManager instance;
    public static bool HasInstance => instance != null;

    // Trả về thể hiện duy nhất hoặc tự khởi tạo nếu trong scene chưa có
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    [SerializeField] AudioCatalog catalog;
    [SerializeField] AudioMixerGroup sfxGroup;
    [SerializeField] AudioMixerGroup musicGroup;
    [SerializeField] AudioMixerGroup uiGroup;
    [SerializeField] Entry[] sfx = Array.Empty<Entry>();
    [SerializeField] Entry[] music = Array.Empty<Entry>();

    [Tooltip("Số kênh SFX 2D phát chồng tối đa")]
    [SerializeField] int voices = 8;

    [Tooltip("Số kênh SFX 3D theo vị trí phát chồng tối đa")]
    [SerializeField] int spatialVoices = 10;

    [Tooltip("Số kênh âm UI phát chồng tối đa")]
    [SerializeField] int uiVoices = 4;

    [Tooltip("Âm 3D xa hơn khoảng này (m) sẽ không phát")]
    [SerializeField] float spatialMaxDistance = 18f;

    [SerializeField] float musicFadeSeconds = 0.8f;

    readonly Dictionary<string, Entry> sfxById = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Entry> musicById = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Entry> ambienceById = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, float> lastPlayTime = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> warnedMissing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    VoicePool sfxPool;
    VoicePool spatialPool;
    VoicePool uiPool;
    AudioSource musicSource;
    AudioSource ambienceSource;
    AudioListener listener;
    Coroutine musicRoutine;
    Coroutine ambienceRoutine;

    string currentMusic;
    string contextMusic; // Nhạc game muốn phát theo scene và trạng thái
    string playerMusic; // Nhạc người chơi chọn trên app Spoti, ưu tiên hơn nhạc theo ngữ cảnh
    string currentAmbience;
    float currentMusicBaseVolume = 0.5f;
    float currentAmbienceBaseVolume = 0.4f;

    // Hệ số âm lượng chỉ dùng khi không có mixer, có mixer thì mixer chỉnh theo nhóm
    float musicVolumeScale = 1f;
    float sfxVolumeScale = 1f;
    float uiVolumeScale = 1f;
    float ambienceVolumeScale = 1f;

    public AudioCatalog Catalog => catalog;
    public string CurrentMusic => currentMusic;
    bool HasMixer => musicGroup != null && sfxGroup != null;

    // Khởi tạo kho âm thanh và tạo sẵn toàn bộ kênh phát một lần
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        ReloadCatalog();
        ResolveMixerGroups();

        sfxPool = new VoicePool(CreateSources(voices, sfxGroup, false, false));
        spatialPool = new VoicePool(CreateSources(spatialVoices, sfxGroup, true, false));
        uiPool = new VoicePool(CreateSources(uiVoices, uiGroup != null ? uiGroup : sfxGroup, false, true));

        musicSource = CreateSource(gameObject, musicGroup, true);
        musicSource.ignoreListenerPause = true;
        ambienceSource = CreateSource(gameObject, sfxGroup, true);

        if (GetComponent<GameAudioDirector>() == null)
            gameObject.AddComponent<GameAudioDirector>();
    }

    // Áp dụng âm lượng đã lưu, đặt ở Start vì mixer chưa nhận SetFloat trong Awake
    void Start()
    {
        GameSettings settings = GameSettings.Load();
        float effectiveMaster = settings.muteAll ? 0f : settings.masterVolume;
        SetVolumes(effectiveMaster, settings.musicVolume, settings.sfxVolume, settings.uiVolume, settings.ambienceVolume);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // Nạp toàn bộ danh mục âm thanh từ AudioCatalog và danh sách cấu hình trên Inspector
    public void ReloadCatalog()
    {
        sfxById.Clear();
        musicById.Clear();
        ambienceById.Clear();

        if (catalog == null)
            catalog = Resources.Load<AudioCatalog>("AudioCatalog");

        if (catalog != null)
        {
            AddEntries(catalog.sfx, sfxById, 0.8f);
            AddEntries(catalog.music, musicById, 0.5f);
            AddEntries(catalog.ambience, ambienceById, 0.4f);
        }

        foreach (Entry e in sfx)
            if (!string.IsNullOrEmpty(e.id) && e.clip != null)
                sfxById[e.id] = e;

        foreach (Entry e in music)
            if (!string.IsNullOrEmpty(e.id) && e.clip != null)
                musicById[e.id] = e;
    }

    static void AddEntries(List<AudioCatalogEntry> entries, Dictionary<string, Entry> target, float defaultVolume)
    {
        if (entries == null) return;
        foreach (AudioCatalogEntry e in entries)
        {
            if (!string.IsNullOrEmpty(e.id) && e.clip != null)
                target[e.id] = new Entry { id = e.id, clip = e.clip, volume = e.volume > 0f ? e.volume : defaultVolume };
        }
    }

    // Lấy nhóm mixer từ AudioCatalog khi AudioManager được tạo lúc chạy, không gán qua Inspector
    void ResolveMixerGroups()
    {
        if (catalog == null || catalog.mixer == null) return;
        if (musicGroup == null) musicGroup = FindGroup(catalog.mixer, "BGM");
        if (sfxGroup == null) sfxGroup = FindGroup(catalog.mixer, "SFX");
        if (uiGroup == null) uiGroup = FindGroup(catalog.mixer, "UI");
    }

    static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
    {
        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    // Hàm gọi nhanh để phát một SFX 2D từ bất kỳ script nào
    public static void Play(string id, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(id)) return;
        Instance.PlaySfx(id, pitch);
    }

    // Hàm gọi nhanh để phát một SFX 3D tại vị trí trong thế giới (cửa, tủ lạnh, toilet...)
    public static void PlayAt(string id, Vector3 position, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(id)) return;
        Instance.PlaySpatial(id, position, pitch);
    }

    // Hàm gọi nhanh để phát âm UI, vẫn nghe được khi game đang tạm dừng
    public static void PlayUi(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        Instance.PlayOn(Instance.uiPool, id, 1f, null);
    }

    // Phát một hiệu ứng âm thanh 2D theo mã định danh
    public void PlaySfx(string id, float pitch = 1f)
    {
        PlayOn(sfxPool, id, pitch, null);
    }

    // Phát một hiệu ứng âm thanh 3D tại vị trí, bỏ qua nếu ở quá xa người nghe
    public void PlaySpatial(string id, Vector3 position, float pitch = 1f)
    {
        if (listener == null)
            listener = FindFirstObjectByType<AudioListener>();
        if (listener != null && (listener.transform.position - position).sqrMagnitude > spatialMaxDistance * spatialMaxDistance)
            return;
        PlayOn(spatialPool, id, pitch, position);
    }

    void PlayOn(VoicePool pool, string id, float pitch, Vector3? position)
    {
        if (pool == null || string.IsNullOrEmpty(id)) return;
        if (!sfxById.TryGetValue(id, out Entry e) || e.clip == null)
        {
            WarnMissing(id);
            return;
        }

        float now = Time.unscaledTime;
        if (lastPlayTime.TryGetValue(id, out float last) && now - last < SameClipCooldown) return;
        if (pool.CountPlaying(id) >= MaxSameClipVoices) return;
        lastPlayTime[id] = now;

        AudioSource source = pool.Rent(id);
        if (position.HasValue)
            source.transform.position = position.Value;
        source.clip = e.clip;
        source.pitch = pitch;
        float channelScale = pool == uiPool ? uiVolumeScale : sfxVolumeScale;
        source.volume = e.volume * (HasMixer ? 1f : channelScale);
        source.Play();
    }

    // Đặt nhạc nền theo ngữ cảnh game (scene, đang stream...), nhạc người chơi chọn trên Spoti vẫn được ưu tiên
    public void PlayMusic(string id)
    {
        contextMusic = id;
        if (playerMusic == null)
            SwitchMusic(id);
    }

    // Phát nhạc người chơi chọn (app Spoti), thay cho nhạc theo ngữ cảnh
    public void PlayPlayerMusic(string id)
    {
        playerMusic = id;
        SwitchMusic(id);
    }

    // Tắt nhạc người chơi chọn, quay lại nhạc theo ngữ cảnh
    public void StopPlayerMusic()
    {
        playerMusic = null;
        SwitchMusic(contextMusic);
    }

    // Dừng nhạc nền theo ngữ cảnh
    public void StopMusic()
    {
        contextMusic = null;
        if (playerMusic == null)
            SwitchMusic(null);
    }

    void SwitchMusic(string id)
    {
        if (string.Equals(id, currentMusic, StringComparison.OrdinalIgnoreCase) || musicSource == null) return;

        Entry next = default;
        if (!string.IsNullOrEmpty(id) && !musicById.TryGetValue(id, out next))
        {
            WarnMissing(id);
            return;
        }

        currentMusic = id;
        currentMusicBaseVolume = next.clip != null ? next.volume : 0f;
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(CrossFade(musicSource, next.clip, MusicTargetVolume()));
    }

    // Phát âm thanh môi trường lặp liên tục theo mã định danh, chuyển mượt khi đổi khu vực
    public void PlayAmbience(string id)
    {
        if (string.IsNullOrEmpty(id) || string.Equals(id, currentAmbience, StringComparison.OrdinalIgnoreCase) || ambienceSource == null) return;
        if (!ambienceById.TryGetValue(id, out Entry e) || e.clip == null)
        {
            WarnMissing(id);
            return;
        }

        currentAmbience = id;
        currentAmbienceBaseVolume = e.volume;
        if (ambienceRoutine != null)
            StopCoroutine(ambienceRoutine);
        ambienceRoutine = StartCoroutine(CrossFade(ambienceSource, e.clip, AmbienceTargetVolume()));
    }

    // Dừng âm thanh môi trường
    public void StopAmbience()
    {
        if (currentAmbience == null || ambienceSource == null) return;
        currentAmbience = null;
        if (ambienceRoutine != null)
            StopCoroutine(ambienceRoutine);
        ambienceRoutine = StartCoroutine(CrossFade(ambienceSource, null, 0f));
    }

    // Tạm dừng âm gameplay khi Pause, nhạc nền và âm UI vẫn chạy
    public void SetGameplayPaused(bool paused)
    {
        AudioListener.pause = paused;
    }

    // Tra cứu AudioClip theo mã định danh trong cả 3 nhóm SFX, BGM và Ambience
    public AudioClip GetClip(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (musicById.TryGetValue(id, out Entry m) && m.clip != null) return m.clip;
        if (ambienceById.TryGetValue(id, out Entry a) && a.clip != null) return a.clip;
        if (sfxById.TryGetValue(id, out Entry s) && s.clip != null) return s.clip;
        return null;
    }

    // Cập nhật âm lượng tổng, nhạc nền, hiệu ứng, UI và môi trường (0..1)
    public void SetVolumes(float master, float musicVol, float sfxVol, float uiVol = -1f, float ambienceVol = -1f)
    {
        master = Mathf.Clamp01(master);
        musicVolumeScale = Mathf.Clamp01(musicVol);
        sfxVolumeScale = Mathf.Clamp01(sfxVol);
        uiVolumeScale = uiVol >= 0f ? Mathf.Clamp01(uiVol) : sfxVolumeScale;
        ambienceVolumeScale = ambienceVol >= 0f ? Mathf.Clamp01(ambienceVol) : sfxVolumeScale;

        AudioMixer mixer = musicGroup != null ? musicGroup.audioMixer : (sfxGroup != null ? sfxGroup.audioMixer : null);
        if (mixer != null && HasMixer)
        {
            AudioListener.volume = 1f;
            mixer.SetFloat("MasterVolume", LinearToDecibel(master));
            mixer.SetFloat("BGMVolume", LinearToDecibel(musicVolumeScale));
            mixer.SetFloat("SFXVolume", LinearToDecibel(sfxVolumeScale));
            mixer.SetFloat("UIVolume", LinearToDecibel(uiVolumeScale));
        }
        else
        {
            AudioListener.volume = master;
        }

        if (musicSource != null && musicRoutine == null && currentMusic != null)
            musicSource.volume = MusicTargetVolume();
        if (ambienceSource != null && ambienceRoutine == null && currentAmbience != null)
            ambienceSource.volume = AmbienceTargetVolume();
    }

    float MusicTargetVolume() => currentMusicBaseVolume * (HasMixer ? 1f : musicVolumeScale);
    float AmbienceTargetVolume() => currentAmbienceBaseVolume * (HasMixer ? 1f : ambienceVolumeScale);

    // Đổi giá trị âm lượng tuyến tính 0..1 sang thang đo Decibel của AudioMixer
    static float LinearToDecibel(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
    }

    // Giảm dần âm cũ rồi tăng dần âm mới trên cùng một nguồn phát, clip null thì chỉ tắt dần
    IEnumerator CrossFade(AudioSource source, AudioClip next, float targetVolume)
    {
        float from = source.volume;
        for (float t = 0f; t < musicFadeSeconds && source.isPlaying; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(from, 0f, t / musicFadeSeconds);
            yield return null;
        }

        source.Stop();
        source.clip = next;
        if (next != null)
        {
            source.volume = 0f;
            source.Play();
            for (float t = 0f; t < musicFadeSeconds; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, targetVolume, t / musicFadeSeconds);
                yield return null;
            }
            source.volume = targetVolume;
        }

        if (source == musicSource) musicRoutine = null;
        else ambienceRoutine = null;
    }

    // Báo một lần cho mỗi mã âm chưa có trong kho, giúp bắt lỗi gõ sai tên
    void WarnMissing(string id)
    {
        if (warnedMissing.Add(id))
            Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh '{id}' trong AudioCatalog");
    }

    AudioSource[] CreateSources(int count, AudioMixerGroup group, bool spatial, bool ignorePause)
    {
        AudioSource[] sources = new AudioSource[Mathf.Max(1, count)];
        for (int i = 0; i < sources.Length; i++)
        {
            GameObject host = gameObject;
            if (spatial)
            {
                host = new GameObject($"SpatialVoice{i}");
                host.transform.SetParent(transform, false);
            }
            AudioSource source = CreateSource(host, group, false);
            source.ignoreListenerPause = ignorePause;
            if (spatial)
            {
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = spatialMaxDistance;
                source.dopplerLevel = 0f;
            }
            sources[i] = source;
        }
        return sources;
    }

    // Tạo một AudioSource gắn vào đối tượng chỉ định
    static AudioSource CreateSource(GameObject host, AudioMixerGroup group, bool loop)
    {
        AudioSource source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = group;
        return source;
    }
}
