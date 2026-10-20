using System.Collections.Generic;
using UnityEngine;

// Sơn tường: chọn màu, trả tiền, đổi material bức tường
public class WallPainter : MonoBehaviour
{
    [SerializeField] List<Color> palette = new List<Color>();
    [SerializeField] float pricePerWall = 40f;

    public void Paint(Renderer wall, int colorIndex)
    {
    }
}
