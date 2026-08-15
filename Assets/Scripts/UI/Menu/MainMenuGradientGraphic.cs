using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class MainMenuGradientGraphic : MaskableGraphic
{
    [SerializeField] private Color startColor = new(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color endColor = new(0f, 0f, 0f, 0f);
    [SerializeField] private bool vertical;

    public void Configure(Color start, Color end, bool useVerticalGradient)
    {
        startColor = start;
        endColor = end;
        vertical = useVerticalGradient;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        Color32 bottomLeft = vertical ? startColor : startColor;
        Color32 topLeft = vertical ? endColor : startColor;
        Color32 topRight = endColor;
        Color32 bottomRight = vertical ? startColor : endColor;

        vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMin), bottomLeft, Vector2.zero);
        vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMax), topLeft, Vector2.up);
        vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMax), topRight, Vector2.one);
        vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMin), bottomRight, Vector2.right);

        vertexHelper.AddTriangle(0, 1, 2);
        vertexHelper.AddTriangle(2, 3, 0);
    }
}
