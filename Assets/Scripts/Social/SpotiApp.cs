using System.Collections.Generic;
using UnityEngine;

// App nghe nhạc Spoti
public class SpotiApp : PcApp
{
    [SerializeField] List<AudioClip> songs = new List<AudioClip>();

    public override void OnOpen()
    {
    }

    public void Play(int index)
    {
    }

    public void Stop()
    {
    }
}
