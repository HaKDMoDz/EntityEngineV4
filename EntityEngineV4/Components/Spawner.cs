﻿using System.Collections.Generic;
using System.Linq;
using EntityEngineV4.Components;
using EntityEngineV4.Components.Rendering;
using EntityEngineV4.Engine;
using Microsoft.Xna.Framework;

namespace EntityEngineV4.PowerTools
{
    public class Spawn : Entity
    {

        public int TimeToLive
        {
            get { return TimeToLiveTimer.Milliseconds; }
            set { TimeToLiveTimer.Milliseconds = value; }
        }

        protected Timer TimeToLiveTimer;

        public Spawn(Node parent, int ttl)
            : base(parent, parent.Name + ".Spawn")
        {
            Name = Name + Id;

            TimeToLiveTimer = new Timer(this, "Timer");
            TimeToLive = ttl;
            TimeToLiveTimer.Start();
            TimeToLiveTimer.LastEvent += () => Destroy();
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
        }

        public override void Destroy(IComponent sender = null)
        {
            base.Destroy(sender);
        }
    }

    public class FadeSpawn : Spawn
    {
        public int FadeAge;
        public Render Render;

        public FadeSpawn(Node parent, int ttl)
            : base(parent, ttl)
        {
        }

        public FadeSpawn(Node parent, int fadeage, int ttl)
            : base(parent, ttl)
        {
            FadeAge = fadeage;
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
            if ((int)TimeToLiveTimer.TickTime > FadeAge)
            {
                int totalsteps = TimeToLive - FadeAge;
                int currentstep = (int)TimeToLiveTimer.TickTime - FadeAge;
                if (currentstep > totalsteps) currentstep = totalsteps;
                float step = currentstep / (totalsteps * 1f);

                Render.Alpha = 1f - 1f * step;
            }
        }
    }

    public class Spawner : Component
    {
        public bool AutoEmit;
        public int AutoEmitAmount = 1;

        public Spawner(Node parent, string name)
            : base(parent, name)
        {
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
            if (AutoEmit)
                Emit(AutoEmitAmount);
        }

        protected virtual Spawn GenerateNewParticle()
        {
            var p = new Spawn(this, 30);
            return p;
        }

        public virtual void Emit(int amount = 1)
        {
            for (var i = 0; i < amount; i++)
            {
                GenerateNewParticle();
            }
        }
    }
}