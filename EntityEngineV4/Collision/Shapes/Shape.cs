﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace EntityEngineV4.Collision.Shapes
{
    public abstract class Shape
    {
        public Collision Collision;
        public abstract Vector2 Position { get; set; }
        public abstract Rectangle BoundingBox { get; set; }

        protected Shape()
        {
        }
    }
}
