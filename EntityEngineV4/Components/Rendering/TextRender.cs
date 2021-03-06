using EntityEngineV4.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EntityEngineV4.Components.Rendering
{
    public class TextRender : Render
    {
        public string Text;
        public SpriteFont Font;

        public override Rectangle DrawRect
        {
            get
            {
                Vector2 position = GetDependency<Body>(DEPENDENCY_BODY).Position +
                    GetDependency<Body>(DEPENDENCY_BODY).Origin + Offset;
                return new Rectangle((int)position.X, (int)position.Y,
                    (int)(Bounds.X), (int)(Bounds.Y));
            }
        }

        public override Vector2 Bounds
        {
            get { return new Vector2(Font.MeasureString(Text).X * Scale.X, Font.MeasureString(Text).Y * Scale.Y); }
        }

        public TextRender(Node node, string name)
            : base(node, name)
        {

        }

        public TextRender(Node node, string name, SpriteFont font, string text)
            : base(node, name)
        {
            Text = text;
            Font = font;
        }

        public override void Draw(SpriteBatch sb)
        {
            if (EntityGame.ActiveCamera.ScreenSpace.Intersects(DrawRect))
                sb.DrawString(Font, Text, GetDependency<Body>(DEPENDENCY_BODY).Position + GetDependency<Body>(DEPENDENCY_BODY).Origin, Color * Alpha, GetDependency<Body>(DEPENDENCY_BODY).Angle, GetDependency<Body>(DEPENDENCY_BODY).Origin, Scale, Flip, Layer);
        }

        public void LoadFont(string location)
        {
            Font = EntityGame.Self.Content.Load<SpriteFont>(location);
        }

        //dependencies
        public const int DEPENDENCY_BODY = 0;
    }
}