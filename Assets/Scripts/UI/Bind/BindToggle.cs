using UnityEngine;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>토글 상태를 키로 표시하고, 바꾸면 행동을 알린다.</summary>
    [RequireComponent(typeof(Toggle))]
    public class BindToggle : BindBase
    {
        public string key;
        public string action;
        Toggle t;
        bool syncing;

        void Awake()
        {
            t = GetComponent<Toggle>();
            t.onValueChanged.AddListener(v => { if (!syncing && Src != null) Src.Act(action, Context); });
        }

        void LateUpdate()
        {
            if (!Live || string.IsNullOrEmpty(key)) return;
            bool v = Src.Flag(key, Context);
            if (t.isOn != v) { syncing = true; t.isOn = v; syncing = false; }
        }
    }
}
