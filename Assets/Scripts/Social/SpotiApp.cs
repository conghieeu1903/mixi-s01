using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App nghe nhạc Spoti
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

        Label title = new Label("Spoti — Trình phát nhạc thư giãn (+Mood)");
        title.AddToClassList("wf-subheading");
        title.style.marginBottom = 8;
        content.Add(title);

        Label status = new Label(IsPlaying && CurrentTrackIndex >= 0
            ? $"Đang phát: {DefaultTrackNames[CurrentTrackIndex]}"
            : "Trạng thái: Đang dừng");
        status.AddToClassList("wf-text");
        status.style.marginBottom = 8;
        content.Add(status);

        for (int i = 0; i < DefaultTrackNames.Length; i++)
        {
            int idx = i;
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;

            Label trackLbl = new Label($"{i + 1}. {DefaultTrackNames[i]}");
            trackLbl.AddToClassList("wf-text");

            Button playBtn = new Button(() =>
            {
                Play(idx);
                BuildWindowContent(content);
            })
            {
                text = IsPlaying && CurrentTrackIndex == idx ? "Đang phát" : "Phát"
            };
            playBtn.AddToClassList("wf-button");
            playBtn.AddToClassList("wf-button--small");

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
            text = "Dừng nhạc"
        };
        stopBtn.AddToClassList("wf-button");
        stopBtn.style.marginTop = 10;
        content.Add(stopBtn);
    }
}
