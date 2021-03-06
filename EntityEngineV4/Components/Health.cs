using EntityEngineV4.Engine;

namespace EntityEngineV4.Components
{
    public class Health : Component
    {
        public int HitPoints { get; set; }

        public bool Alive
        {
            get { return !(HitPoints <= 0); }
        }

        public event Node.EventHandler HurtEvent;

        public event Node.EventHandler DiedEvent;

        public Health(Node parent, string name)
            : base(parent, name)
        {
        }

        public Health(Node parent, string name, int hp)
            : base(parent, name)
        {
            HitPoints = hp;
        }

        public void Hurt(int points)
        {
            if (!Alive) return;

            HitPoints -= points;
            if (HurtEvent != null)
                HurtEvent(Parent);

            if (!Alive)
            {
                if (DiedEvent != null)
                    DiedEvent(Parent);
            }
        }
    }
}