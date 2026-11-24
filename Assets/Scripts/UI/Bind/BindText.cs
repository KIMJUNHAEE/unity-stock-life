using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>Text 내용을 키로 채운다. 씬에 적힌 글자는 편집용 예시이며 플레이 중에 바뀐다.</summary>
    [RequireComponent(typeof(Text))]
    public class BindText : BindBase
    {
        [Tooltip("표시할 값의 키")] public string key;
        [Tooltip("글자색 키 (비우면 씬에서 정한 색 유지)")] public string colorKey;
        Text t;

        void Awake() { t = GetComponent<Text>(); }

        void LateUpdate()
        {
            if (!Live) return;
            if (!string.IsNullOrEmpty(key))
            {
                string s = Src.Text(key, Context);
                if (s != null && t.text != s) t.text = s;
            }
            if (!string.IsNullOrEmpty(colorKey))
            {
                string ck = Src.ColorKey(colorKey, Context);
                Color c;
                if (ck != null && Palette.TryGet(ck, out c)) t.color = c;
            }
        }
    }
}
