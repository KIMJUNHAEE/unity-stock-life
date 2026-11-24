using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 목록. template 오브젝트(비활성)를 항목 수만큼 복제한다.
    /// 항목 모양은 씬에서 Template 을 켜고 편집하면 된다 (플레이하면 자동으로 숨겨진다).
    /// </summary>
    public class UIList : BindBase
    {
        public string listKey;
        [Tooltip("복제할 항목 원본")] public RectTransform template;
        [Tooltip("복제본이 들어갈 곳 (비우면 template 의 부모)")] public RectTransform content;
        readonly List<GameObject> items = new List<GameObject>();

        void Awake()
        {
            if (template != null)
            {
                if (content == null) content = template.parent as RectTransform;
                template.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!Live || template == null || content == null) return;
            int n = Mathf.Max(0, Src.Count(listKey));
            while (items.Count < n)
            {
                var go = Instantiate(template.gameObject, content, false);
                go.name = template.name.Replace("Template", "Item") + "_" + items.Count;
                var li = go.GetComponent<UIListItem>();
                if (li == null) li = go.AddComponent<UIListItem>();
                li.List = listKey;
                li.Index = items.Count;
                go.SetActive(true);
                items.Add(go);
            }
            while (items.Count > n)
            {
                Destroy(items[items.Count - 1]);
                items.RemoveAt(items.Count - 1);
            }
        }
    }
}
