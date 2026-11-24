using UnityEngine;

namespace StockGame
{
    /// <summary>UIList 가 복제한 항목에 붙는다. 자식 바인딩은 이 번호로 값을 받는다.</summary>
    public class UIListItem : MonoBehaviour
    {
        public string List;
        public int Index;
        public Ctx Context { get { return new Ctx { List = List, Index = Index }; } }
    }
}
