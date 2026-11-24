using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>
    /// 이미지(스프라이트)·색을 키로 바꾼다. 스프라이트가 없으면 placeholder 를 보여준다.
    /// 고정 이미지는 이 컴포넌트 없이 Image 의 Source Image 에 바로 넣으면 된다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class BindImage : BindBase
    {
        [Tooltip("스프라이트 키 (비우면 바꾸지 않음)")] public string spriteKey;
        [Tooltip("색 키 (테마 색 이름을 돌려준다)")] public string colorKey;
        [Tooltip("스프라이트가 없을 때 켤 자리표시 오브젝트")] public GameObject placeholder;
        [Tooltip("스프라이트가 없을 때 이미지 자체를 숨김")] public bool hideWhenEmpty;
        Image img;
        Color baseColor;

        void Awake() { img = GetComponent<Image>(); baseColor = img.color; }

        void LateUpdate()
        {
            if (!Live) return;
            if (!string.IsNullOrEmpty(spriteKey))
            {
                var s = Src.SpriteOf(spriteKey, Context);
                if (img.sprite != s) img.sprite = s;
                bool has = s != null;
                if (placeholder != null && placeholder.activeSelf == has) placeholder.SetActive(!has);
                if (hideWhenEmpty) img.enabled = has;
                if (has && string.IsNullOrEmpty(colorKey) && img.color != Color.white) img.color = Color.white;
                else if (!has && string.IsNullOrEmpty(colorKey) && img.color != baseColor) img.color = baseColor;
                if (has) img.preserveAspect = true;
            }
            if (!string.IsNullOrEmpty(colorKey))
            {
                string ck = Src.ColorKey(colorKey, Context);
                Color c;
                if (ck != null && ck.StartsWith("#") && ColorUtility.TryParseHtmlString(ck, out c)) img.color = c;
                else if (ck != null && Palette.TryGet(ck, out c)) img.color = c;
            }
        }
    }
}
