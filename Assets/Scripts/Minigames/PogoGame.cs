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
    }

    public void ReachCheckpoint(Transform checkpoint)
    {
        CheckpointsReached++;
        if (checkpoint != null)
            LastCheckpointPosition = checkpoint.position;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
    }
}
