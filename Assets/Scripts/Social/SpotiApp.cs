using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App nghe nhạc Spoti trên Windows Desktop: trình phát nhạc thư giãn (+Mood)
public class SpotiApp : PcApp
{
    static readonly string[] DefaultTrackNames =
    {
        "Synth Chill Stream Beat",
        "Lo-Fi Late Night Coding",
        "Cyberpunk Gaming Wave",
        "Relaxing ASMR Ambient"
    };

    [SerializeField] List<AudioClip> songs = new List<AudioClip>();

    public override string AppName => "Spoti";

    AudioSource audioSource;
    public int CurrentTrackIndex { get; private set; } = -1;
    public bool IsPlaying { get; private set; }

    void Awake()
    {
        if (string.IsNullOrEmpty(AppName))
        {
            var field = typeof(PcApp).GetField("appName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, "Spoti");
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 0.35f;
        }
    }

    public void Play(int index)
    {
        CurrentTrackIndex = Mathf.Clamp(index, 0, DefaultTrackNames.Length - 1);
        IsPlaying = true;
        if (songs != null && CurrentTrackIndex < songs.Count && songs[CurrentTrackIndex] != null && audioSource != null)
        {
            audioSource.clip = songs[CurrentTrackIndex];
            audioSource.Play();
        }
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
    }

    public void Stop()
    {
        IsPlaying = false;
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.08f, 0.11f, 0.10f, 1f);

        Label title = new Label("♪ SPOTI MUSIC — Playlist Streamer (+Mood)");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.20f, 0.88f, 0.48f, 1f);
        title.style.marginBottom = 8;
        content.Add(title);

        Label status = new Label(IsPlaying && CurrentTrackIndex >= 0
            ? $"▶ Đang phát: {DefaultTrackNames[CurrentTrackIndex]}"
            : "⏸ Trạng thái: Đang dừng phát");
        status.style.fontSize = 12;
        status.style.color = new Color(0.84f, 0.96f, 0.88f, 1f);
        status.style.marginBottom = 10;
        content.Add(status);

        for (int i = 0; i < DefaultTrackNames.Length; i++)
        {
            int idx = i;
            bool playingThis = IsPlaying && CurrentTrackIndex == idx;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 7;
            row.style.paddingBottom = 7;
            row.style.marginBottom = 5;
            row.style.backgroundColor = playingThis
                ? new Color(0.12f, 0.28f, 0.18f, 1f)
                : new Color(0.13f, 0.16f, 0.16f, 1f);
            row.style.borderTopLeftRadius = 5;
            row.style.borderTopRightRadius = 5;
            row.style.borderBottomLeftRadius = 5;
            row.style.borderBottomRightRadius = 5;

            Label trackLbl = new Label($"{i + 1}. {DefaultTrackNames[i]}");
            trackLbl.style.fontSize = 12;
            trackLbl.style.color = playingThis ? new Color(0.28f, 0.96f, 0.56f, 1f) : Color.white;

            Button playBtn = new Button(() =>
            {
                Play(idx);
                BuildWindowContent(content);
            })
            {
                text = playingThis ? "Đang phát ♪" : "▶ Phát"
            };
            playBtn.style.height = 24;
            playBtn.style.paddingLeft = 10;
            playBtn.style.paddingRight = 10;
            playBtn.style.fontSize = 11;
            playBtn.style.color = Color.white;
            playBtn.style.backgroundColor = new Color(0.14f, 0.72f, 0.38f, 1f);

            row.Add(trackLbl);
            row.Add(playBtn);
            content.Add(row);
        }

        Button stopBtn = new Button(() =>
        {
            Stop();
            BuildWindowContent(content);
        })
        {
            text = "⏹ Dừng phát nhạc"
        };
        stopBtn.style.height = 28;
        stopBtn.style.marginTop = 8;
        stopBtn.style.color = Color.white;
        stopBtn.style.backgroundColor = new Color(0.24f, 0.28f, 0.32f, 1f);
        content.Add(stopBtn);
    }
}
