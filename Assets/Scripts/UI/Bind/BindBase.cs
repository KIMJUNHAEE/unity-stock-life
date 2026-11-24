using UnityEngine;

namespace StockGame
{
    /// <summary>가장 가까운 부모 UISource 와 목록 항목을 찾아 둔다.</summary>
    public abstract class BindBase : MonoBehaviour
    {
        UISource src;
        UIListItem item;
        bool searched;

        void Find()
        {
            if (searched) return;
            searched = true;
            item = GetComponentInParent<UIListItem>(true);
            src = GetComponentInParent<UISource>(true);
        }

        protected UISource Src { get { Find(); return src; } }
        protected Ctx Context { get { Find(); return item != null ? item.Context : Ctx.None; } }
        protected virtual void OnTransformParentChanged() { searched = false; }
        protected bool Live { get { return Application.isPlaying && Src != null; } }
    }
}
