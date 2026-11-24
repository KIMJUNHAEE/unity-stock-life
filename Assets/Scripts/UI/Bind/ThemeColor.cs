using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>
    /// 이 오브젝트의 색을 GameConfig 테마 색(키)으로 맞춘다. 테마를 바꾸면 함께 바뀐다.
    /// 씬에서 색을 직접 정하고 싶으면 이 컴포넌트를 지우면 된다.
    /// </summary>
    [ExecuteAlways]
    public class ThemeColor : MonoBehaviour
    {
        public string colorKey = "accent";
        [Range(0, 1)] public float alphaMultiplier = 1;
        Graphic g;

        void OnEnable() { g = GetComponent<Graphic>(); Apply(); }
        void Update() { Apply(); }

        void Apply()
        {
            if (g == null || string.IsNullOrEmpty(colorKey)) return;
            Color c;
            if (!Palette.TryGet(colorKey, out c)) return;
            c.a *= alphaMultiplier;
            if (g.color != c) g.color = c;
        }
    }
}
