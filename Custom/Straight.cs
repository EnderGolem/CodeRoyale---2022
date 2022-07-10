using AiCup22.Model;

namespace AiCup22.Custom
{
    public struct Straight
    {
        public double a;
        public double b;
        public double c;
        
        public Straight(double a, double b, double c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        /// <summary>
        /// устанавливаем коэффициенты прямой по направляющему вектору и точке принадлежащей прямой
        /// </summary>
        /// <param name="directive"></param>
        /// <param name="point"></param>
        public Straight(Vec2 directive, Vec2 point)
        {
            a = directive.Y;
            b = -directive.X;
            c = directive.X * point.Y - directive.Y * point.X;
        }
        /// <summary>
        /// устанавливаем коэффициенты прямой по направляющему вектору и точке принадлежащей прямой
        /// </summary>
        /// <param name="directive"></param>
        /// <param name="point"></param>
        public void SetByDirectiveAndPoint(Vec2 directive, Vec2 point)
        {
            a = directive.Y;
            b = -directive.X;
            c = directive.X * point.Y - directive.Y * point.X;
        }

        public void SetByNormalAndPoint(Vec2 normal, Vec2 point)
        {
            a = normal.X;
            b = normal.Y;
            c = -normal.X * point.X - normal.Y * point.Y;
        }

        /// <summary>
        /// получаем направляющую прямой
        /// </summary>
        /// <returns></returns>
        public Vec2 GetDirective()
        {
            return new Vec2(-b,a);
        }
        /// <summary>
        ///  возвращает вектор перпендикулярный прямой
        /// </summary>
        public Vec2 GetNormal()
        {
            return new Vec2(a,b);
        }

        public bool IsParallel(Straight s)
        {
            if (a*s.b-s.a*b == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Vec2? GetIntersection(Straight s)
        {
            if (IsParallel(s)) return null;

            double x = (s.c * b - c * s.b) / (a * s.b - s.a * b);
            double y = 0;
            if (b != 0)
            {
                y = -(a * x + c) / b;
            }
            else
            {
                y = -(s.a * x + s.c) / s.b;
            }
            
            
            return new Vec2(x,y);
        }

        public override string ToString()
        {
            return $"{a}x+{b}y+{c}=0";
        }
    }
    
    
}