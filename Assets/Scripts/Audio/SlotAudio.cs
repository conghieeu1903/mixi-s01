using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Phát âm thanh máy slot theo các sự kiện quay, dừng và kết quả
[RequireComponent(typeof(SlotMachine))]
public class SlotAudio : MonoBehaviour
{
    [SerializeField] AudioMixerGroup output; // Nhóm mixer xuất âm
    [SerializeField] AudioClip spinStart; // Tiếng bấm quay
    [SerializeField] AudioClip reelLoop; // Tiếng reel quay, phát lặp
    [SerializeField] AudioClip reelStop; // Tiếng reel dừng
    [SerializeField] AudioClip anticipation; // Tiếng hồi hộp khi còn reel cuối, phát lặp
    [SerializeField] AudioClip lose; // Âm khi thua
    [SerializeField] AudioClip win; // Âm khi trúng thường
    [SerializeField] AudioClip jackpot; // Âm khi trúng 777
    [SerializeField] AudioClip coinTick; // Tiếng đếm xu
    [SerializeField] AudioClip notEnoughCoins; // Âm báo thiếu xu
    [SerializeField] float reelStopPitchStep = 0.06f; // Độ tăng cao độ tiếng dừng theo từng reel
    [SerializeField] float loopFadeSeconds = 0.25f; // Thời gian nhỏ dần khi tắt âm lặp (giây)
    [SerializeField] float coinTickInterval = 0.07f; // Khoảng cách tối thiểu giữa hai tiếng xu (giây)

    SlotMachine machine; // Máy slot cần nghe sự kiện
    float lastCoinTick = -1f; // Thời điểm phát tiếng xu gần nhất
    AudioSource oneShot; // Nguồn phát các âm ngắn
    AudioSource stopSource; // Nguồn phát tiếng dừng reel
    AudioSource loopSource; // Nguồn phát tiếng reel quay
    AudioSource anticipationSource; // Nguồn phát tiếng hồi hộp

    // Tạo các nguồn phát riêng cho từng loại âm
    void Awake()
    {
        machine = GetComponent<SlotMachine>();
        oneShot = CreateSource(false);
        stopSource = CreateSource(false);
        loopSource = CreateSource(true);
        anticipationSource = CreateSource(true);
    }

    // Đăng ký sự kiện của máy slot
    void OnEnable()
    {
        machine.SpinStarted += OnSpinStarted;
        machine.ReelStopped += OnReelStopped;
        machine.AnticipationStarted += OnAnticipation;
        machine.SpinFinished += OnSpinFinished;
        machine.NotEnoughCoins += OnNotEnoughCoins;
        if (machine.Vfx != null) machine.Vfx.CoinArrived += OnCoinArrived;
    }

    // Hủy đăng ký sự kiện của máy slot
    void OnDisable()
    {
        machine.SpinStarted -= OnSpinStarted;
        machine.ReelStopped -= OnReelStopped;
        machine.AnticipationStarted -= OnAnticipation;
        machine.SpinFinished -= OnSpinFinished;
        machine.NotEnoughCoins -= OnNotEnoughCoins;
        if (machine.Vfx != null) machine.Vfx.CoinArrived -= OnCoinArrived;
    }

    // Phát tiếng bấm quay và bật vòng lặp reel
    void OnSpinStarted()
    {
        StopAllCoroutines();
        anticipationSource.Stop();
        Play(oneShot, spinStart);
        StartLoop(loopSource, reelLoop);
    }

    // Phát tiếng reel dừng, cao dần theo bước dừng
    void OnReelStopped(Reel reel)
    {
        stopSource.pitch = 1f + (reel.Index % 12) * reelStopPitchStep;
        Play(stopSource, reelStop);
    }

    // Bật vòng lặp hồi hộp khi chỉ còn reel cuối
    void OnAnticipation() => StartLoop(anticipationSource, anticipation);

    // Tắt các vòng lặp và phát âm kết quả
    void OnSpinFinished(SpinOutcome outcome)
    {
        StartCoroutine(FadeOut(loopSource));
        StartCoroutine(FadeOut(anticipationSource));
        switch (outcome)
        {
            case SpinOutcome.Bingo777:
                Play(oneShot, jackpot);
                break;
            case SpinOutcome.BingoXXX:
                Play(oneShot, win);
                break;
            default:
                Play(oneShot, lose);
                break;
        }
    }

    // Phát tiếng báo thiếu xu
    void OnNotEnoughCoins() => Play(oneShot, notEnoughCoins);

    // Phát tiếng xu khi một đồng xu bay tới icon, giãn cách tối thiểu coinTickInterval
    void OnCoinArrived()
    {
        if (Time.time - lastCoinTick < coinTickInterval) return;
        lastCoinTick = Time.time;
        Play(oneShot, coinTick);
    }

    // Giảm dần âm lượng rồi dừng nguồn phát
    IEnumerator FadeOut(AudioSource source)
    {
        if (!source.isPlaying) yield break;
        float start = source.volume;
        for (float t = 0f; t < loopFadeSeconds; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(start, 0f, t / loopFadeSeconds);
            yield return null;
        }
        source.Stop();
        source.volume = 1f;
    }

    // Bật một vòng lặp từ đầu với âm lượng đầy đủ
    static void StartLoop(AudioSource source, AudioClip clip)
    {
        if (clip == null) return;
        source.clip = clip;
        source.volume = 1f;
        source.Play();
    }

    // Phát một âm ngắn nếu có clip
    static void Play(AudioSource source, AudioClip clip)
    {
        if (clip != null) source.PlayOneShot(clip);
    }

    // Thêm một AudioSource 2D xuất ra nhóm mixer đã chọn
    AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = output;
        return source;
    }
}
