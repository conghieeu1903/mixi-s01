using System.Collections.Generic;
using UnityEngine;

// Sơn tường: chọn màu, trả tiền, đổi material bức tường
public class WallPainter : MonoBehaviour, IInteractable
{
    [SerializeField] List<Color> palette = new List<Color>
    {
        new Color(0.88f, 0.91f, 0.95f),
        new Color(0.95f, 0.89f, 0.82f),
        new Color(0.82f, 0.92f, 0.86f),
        new Color(0.86f, 0.84f, 0.94f)
    };
    [SerializeField] float pricePerWall = 40f;

    int nextColorIndex;

    public string Prompt => $"Sơn mới tường phòng ($ {pricePerWall:0})";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Renderer r = GetComponentInChildren<Renderer>();
        Paint(r, nextColorIndex);
        nextColorIndex = (nextColorIndex + 1) % Mathf.Max(1, palette.Count);
    }

    public void Paint(Renderer wall, int colorIndex)
    {
        if (palette == null || palette.Count == 0)
        {
            palette = new List<Color> { new Color(0.88f, 0.91f, 0.95f), new Color(0.82f, 0.92f, 0.86f) };
        }

        if (GameWorld.HasInstance && !GameWorld.Instance.Economy.TrySpend(pricePerWall))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ $ {pricePerWall:0} để sơn tường", false);
            return;
        }

        Color chosen = palette[Mathf.Clamp(colorIndex, 0, palette.Count - 1)];
        if (wall != null && wall.material != null)
            wall.material.color = chosen;

        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(4f);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã sơn màu tường mới cho phòng Streamer! (+4 Mood)", true);
    }
}
