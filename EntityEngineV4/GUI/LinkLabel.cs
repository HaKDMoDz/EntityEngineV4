﻿using EntityEngineV4.Engine;
using Microsoft.Xna.Framework;

namespace EntityEngineV4.GUI
{
    public class LinkLabel : Label
    {
        public Color SelectedColor = Color.Red;

        public LinkLabel(IComponent parent, string name)
            : base(parent, name)
        {
            Selectable = true;
        }

        public override void OnFocusLost(Control c)
        {
            base.OnFocusLost(c);
            TextRender.Color = Color;
        }

        public override void OnFocusGain(Control c)
        {
            base.OnFocusGain(c);
            TextRender.Color = SelectedColor;
        }
    }
}