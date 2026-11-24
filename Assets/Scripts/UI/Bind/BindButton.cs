using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>버튼을 누르면 행동 이름을 화면 컨트롤러에 알린다.</summary>
    [RequireComponent(typeof(Button))]
    public class BindButton : BindBase
    {
        [Tooltip("누르면 실행할 행동")] public string action;
        [Tooltip("이 값이 거짓이면 비활성 (비우면 항상 활성)")] public string enableKey;
        Button b;

        void Awake()
        {
            b = GetComponent<Button>();
            b.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            if (Src != null && !string.IsNullOrEmpty(action)) Src.Act(action, Context);
        }

        void LateUpdate()
        {
            if (!Live || string.IsNullOrEmpty(enableKey)) return;
            bool on = Src.Flag(enableKey, Context);
            if (b.interactable != on) b.interactable = on;
        }
    }
}
