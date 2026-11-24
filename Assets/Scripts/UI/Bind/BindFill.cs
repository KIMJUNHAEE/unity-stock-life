using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>0~1 값으로 막대 길이를 바꾼다. Image 가 Filled 면 fillAmount, 아니면 RectTransform 가로 비율.</summary>
    public class BindFill : BindBase
    {
        public string key;
        Image img;
        RectTransform rt;

        void Awake() { img = GetComponent<Image>(); rt = (RectTransform)transform; }

        void LateUpdate()
        {
            if (!Live || string.IsNullOrEmpty(key)) return;
            float v = Mathf.Clamp01(Src.Value(key, Context));
            if (img != null && img.type == Image.Type.Filled) img.fillAmount = v;
            else
            {
                var max = rt.anchorMax;
                if (!Mathf.Approximately(max.x, v)) { max.x = v; rt.anchorMax = max; }
            }
        }
    }
}
