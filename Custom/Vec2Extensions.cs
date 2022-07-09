using AiCup22.Model;

namespace AiCup22
{
    public static class Vec2Extensions
    {
        public static Vec2 Add(this Vec2 a, Vec2 b)
        {
            return new Vec2(a.X - b.X, a.Y - b.Y);
        }
        public static Vec2 Subtract(this Vec2 a, Vec2 b)
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

        public static Vec2 Nomalize(this Vec2 a)
        {
            double distance = a.Distance(new Vec2());
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
    }
}
