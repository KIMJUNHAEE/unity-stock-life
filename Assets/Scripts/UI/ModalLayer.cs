namespace StockGame
{
    /// <summary>확인 창. 키 — title, body / 목록 buttons: b_label, 참거짓 b_primary / 행동 b</summary>
    public class ModalLayer : UISource
    {
        public override string Text(string key, Ctx c)
        {
            var m = GameRoot.I.TopModal;
            if (m == null) return "";
            switch (key)
            {
                case "title": return m.Title;
                case "body": return m.Body;
                case "b_label": return c.Index >= 0 && c.Index < m.Buttons.Count ? m.Buttons[c.Index].Label : "";
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            var m = GameRoot.I.TopModal;
            if (key == "b_primary") return m != null && c.Index >= 0 && c.Index < m.Buttons.Count && m.Buttons[c.Index].Primary;
            return false;
        }

        public override string ColorKey(string key, Ctx c)
        {
            if (key == "b_color") return Flag("b_primary", c) ? "accent" : "button";
            if (key == "b_text_color") return Flag("b_primary", c) ? "text_on_accent" : "text";
            return null;
        }

        public override int Count(string list)
        {
            var m = GameRoot.I.TopModal;
            return list == "buttons" && m != null ? m.Buttons.Count : 0;
        }

        public override void Act(string action, Ctx c)
        {
            if (action == "b") GameRoot.I.ClickModal(c.Index);
        }
    }
}
