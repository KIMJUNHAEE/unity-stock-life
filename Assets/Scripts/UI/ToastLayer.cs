namespace StockGame
{
    /// <summary>화면 위 알림. 목록 toasts: toast</summary>
    public class ToastLayer : UISource
    {
        public override int Count(string list) { return list == "toasts" ? GameRoot.I.Toasts.Count : 0; }

        public override string Text(string key, Ctx c)
        {
            var t = GameRoot.I.Toasts;
            if (key == "toast" && c.Index >= 0 && c.Index < t.Count) return t[c.Index].Key;
            return null;
        }
    }
}
