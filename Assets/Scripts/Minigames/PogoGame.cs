using UnityEngine;

// Pogo: platformer nhảy gậy có checkpoint
public class PogoGame : MonoBehaviour
{
    public Vector3 LastCheckpointPosition { get; private set; }
    public int CheckpointsReached { get; private set; }

    public void Restart()
    {
        CheckpointsReached = 0;
        LastCheckpointPosition = Vector3.zero;
        AudioManager.Play("pogo_jump");
    }

    public void ReachCheckpoint(Transform checkpoint)
    {
        CheckpointsReached++;
        AudioManager.Play("pogo_jump");
        AudioManager.PlayUi("ui_success");
        if (checkpoint != null)
            LastCheckpointPosition = checkpoint.position;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
    }
}
