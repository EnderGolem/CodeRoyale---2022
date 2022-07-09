using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public static class Tools
    {
        /// <summary>
        /// Возвращает расстояние от точки до края текущей зоны
        /// Если позиция находится за зоной, то расстояние считается отрицательным
        /// </summary>
        public static double CurrentZoneDistance(Zone zone, Vec2 position)
        {
            return zone.CurrentRadius - zone.CurrentCenter.Distance(position);
        }
        /// <summary>
        /// Возвращает первое препятствие на прямой из стартовой точки к конечной, если оно существует
        /// И null в противном случае
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="obstacles">Некий набор укрытий который мы просматриваем желательно не все укрытия на карте</param>
        /// <param name="ignoreLowObstacles">Следует ли игнорировать при просмотре
        /// простреливаемые укрытия
        /// </param>
        /// <returns></returns>
        public static Obstacle? RaycastObstacle(Vec2 startPoint, Vec2 endPoint, Obstacle[] obstacles, bool ignoreLowObstacles)
        {
            List<Obstacle> hitObstacles = new List<Obstacle>();

            for (int i = 0; i < obstacles.Length; i++)
            {
                if(ignoreLowObstacles && obstacles[i].CanShootThrough)
                    continue;
                Vec2 sp = startPoint;
                Vec2 ep = endPoint;
                sp = sp.Subtract(obstacles[i].Position);
                ep = ep.Subtract(obstacles[i].Position);

                Vec2 d = ep.Subtract(sp);
                
                double a = d.X*d.X + d.Y*d.Y;
                double b = 2*(sp.X*d.X + sp.Y*d.Y);
                double c = sp.X*sp.X + sp.Y*sp.Y - obstacles[i].Radius*obstacles[i].Radius;

                bool flag;
                if (-b < 0)
                    flag = (c < 0);
                else if (-b < (2*a))
                 flag = ((4*a*c - b*b) < 0);
                else
                flag = (a+b+c < 0);

                if (flag)
                {
                    hitObstacles.Add(obstacles[i]);
                }
            }

            if (hitObstacles.Count == 0)
            {
                return null;
            }
            else
            {
                return hitObstacles.OrderBy((Obstacle o) => startPoint.SqrDistance(o.Position)).First();
            }
        }
    }
}
