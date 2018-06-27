using Microsoft.Xna.Framework;

namespace JoysOfEfficiency
{
    public class RectangleE
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }

        public RectangleE(Rectangle parent)
        {
            X = parent.Left;
            Y = parent.Top;
            Width = parent.Width;
            Height = parent.Height;
        }

        public RectangleE(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool CollideWith(Rectangle rect)
        {
            return IsInternalPoint(rect.Center);
        }

        public bool IsInternalPoint(Point point)
        {
            return IsInternalPoint(point.X, point.Y);
        }

        public bool IsInternalPoint(Vector2 locationVec)
        {
            return IsInternalPoint(locationVec.X, locationVec.Y);
        }

        public bool IsInternalPoint(float x, float y)
        {
            return x >= X && x <= X + Width && y >= Y && y < Y + Height;
        }
    }
}
