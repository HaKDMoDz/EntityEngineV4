﻿using EntityEngineV4.Components;
using EntityEngineV4.Engine;
using Microsoft.Xna.Framework;

namespace EntityEngineV4.CollisionEngine.Shapes
{
    public abstract class Shape : Component
    {
        public abstract Vector2 Position { get; }
        public Vector2 Offset = new Vector2();

        protected Shape(Node parent, string name)
            : base(parent, name)
        {
        }
        //Dependencies
        public const int DEPENDENCY_COLLISION = 0;
        public const int DEPENDENCY_BODY = 1;
        public override void CreateDependencyList()
        {
            base.CreateDependencyList();
            AddLinkType(DEPENDENCY_COLLISION, typeof(Collision));
            AddLinkType(DEPENDENCY_BODY, typeof(Body));
        }
    }
}