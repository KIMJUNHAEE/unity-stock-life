using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>위→아래 두 색 그라데이션. 테마 색 키를 넣으면 테마를 따른다.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class GradientGraphic : MaskableGraphic
    {
        public Color top = new Color(0.75f, 0.85f, 0.95f);
        public Color bottom = new Color(0.96f, 0.85f, 0.91f);
        [Tooltip("테마 색 키 (비우면 위 색 사용)")] public string themeTopKey;
        public string themeBottomKey;
        [Tooltip("가로 그라데이션")] public bool horizontal;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            Color a = top * color, b = bottom * color;
            if (horizontal)
            {
                vh.AddVert(new Vector3(r.xMin, r.yMin), a, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin, r.yMax), a, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMax), b, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMin), b, Vector2.zero);
            }
            else
            {
                vh.AddVert(new Vector3(r.xMin, r.yMin), b, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin, r.yMax), a, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMax), a, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMin), b, Vector2.zero);
            }
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        void Update()
        {
            Color c;
            bool changed = false;
            if (!string.IsNullOrEmpty(themeTopKey) && Palette.TryGet(themeTopKey, out c) && c != top) { top = c; changed = true; }
            if (!string.IsNullOrEmpty(themeBottomKey) && Palette.TryGet(themeBottomKey, out c) && c != bottom) { bottom = c; changed = true; }
            if (changed) SetVerticesDirty();
        }
    }
}
