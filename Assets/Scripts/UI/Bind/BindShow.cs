using UnityEngine;

namespace StockGame
{
    /// <summary>키 값이 참일 때만 보인다 (CanvasGroup 사용). 편집 중에는 항상 보인다.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BindShow : BindBase
    {
        public string key;
        [Tooltip("반대로 (거짓일 때 보이기)")] public bool invert;
        CanvasGroup g;

        void Awake() { g = GetComponent<CanvasGroup>(); }

        void LateUpdate()
        {
            if (!Live || string.IsNullOrEmpty(key)) return;
            bool v = Src.Flag(key, Context) ^ invert;
            float a = v ? 1 : 0;
            if (g.alpha != a) g.alpha = a;
            g.blocksRaycasts = v;
            g.interactable = v;
        }
    }
}
