namespace Auios.Rectangle
{
    public class Rectangle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float Top => Y;
        public float Bottom => Y + Height;
        public float Left => X;
        public float Right => X + Width;
        public float HalfWidth => Width * 0.5f;
        public float HalfHeight => Height * 0.5f;
        public float CenterX => X + HalfWidth;
        public float CenterY => Y + HalfHeight;

        public Rectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(float x, float y)
        {
            if (x < Left || x > Right) return false;
            if (y < Top || y > Bottom) return false;
            return true;
        }
    }
}
