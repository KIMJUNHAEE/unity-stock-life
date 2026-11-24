using UnityEngine;

namespace StockGame
{
    /// <summary>목록 안의 위치. 목록 밖이면 Index = -1.</summary>
    public struct Ctx
    {
        public string List;
        public int Index;
        public static Ctx None { get { return new Ctx { List = null, Index = -1 }; } }
    }

    /// <summary>
    /// 화면 컨트롤러의 기본형. 자식의 Bind* 컴포넌트가 키(문자열)로 값을 물어보고, 버튼은 행동 이름으로 알려 준다.
    /// 씬에서 오브젝트를 옮기거나 복제해도 키만 맞으면 그대로 동작한다.
    /// </summary>
    public abstract class UISource : MonoBehaviour
    {
        public virtual string Text(string key, Ctx c) { return null; }
        public virtual bool Flag(string key, Ctx c) { return false; }
        public virtual float Value(string key, Ctx c) { return 0; }
        public virtual Sprite SpriteOf(string key, Ctx c) { return null; }
        public virtual string ColorKey(string key, Ctx c) { return null; }
        public virtual void Act(string action, Ctx c) { }
        public virtual int Count(string list) { return 0; }
    }
}
