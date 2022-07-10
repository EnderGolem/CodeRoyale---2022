using AiCup22.Model;

namespace AiCup22
{
    public static class Vec2Extensions
    {
        public static Vec2 Add(this Vec2 a, Vec2 b)
        {
            return new Vec2(a.X + b.X, a.Y + b.Y);
        }
        public static Vec2 Substract(this Vec2 a, Vec2 b)
        {
            return new Vec2(a.X - b.X, a.Y - b.Y);
        }
        public static Vec2 Multi(this Vec2 a, double b)
        {

            return new Vec2(a.X * b, a.Y * b);
        }
        public static double SqrDistance(this Vec2 a, Vec2 b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }
        public static double Distance(this Vec2 a, Vec2 b)
        {
            return System.Math.Sqrt(a.SqrDistance(b));
        }

        public static Vec2 Normalize(this Vec2 a)
        {
            double distance = a.Distance(new Vec2(0, 0));
            a.X = a.X / distance;
            a.Y = a.Y / distance;
            return a;
        }
        public static bool Compare(this Vec2 a, Vec2 b)
        {
            return (a.X == b.X) && (a.Y == b.Y);
        }
        public static bool CompareEps(this Vec2 a, Vec2 b, double eps = 0.1)
        {
            return System.Math.Abs(a.X - b.X) < eps && System.Math.Abs(a.Y - b.Y) < eps;
        }
        public static bool CheckPerpendicular(this Vec2 a, Vec2 b)
        {
            return (a.X * b.X + a.Y * b.Y) == 0;
        }
        public static Vec2 FindPerpendicularWithX(this Vec2 a, double X)
        {
            return new Vec2(X, -a.X * X / a.Y);
        }
        public static Vec2 FindPerpendicularWithY(this Vec2 a, double Y)
        {
            return new Vec2(-a.Y * Y / a.X, Y);
        }
        /// <summary>
        /// Находит "Зеркальное" отображение точки b относительно a. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vec2 FindMirror(this Vec2 a, Vec2 b)
        {
            return a.Substract(a.Substract(b).Multi(-1));

        }
    }
}
