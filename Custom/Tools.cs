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
                if (ignoreLowObstacles && obstacles[i].CanShootThrough)
                    continue;
                Vec2 sp = startPoint;
                Vec2 ep = endPoint;
                sp = sp.Substract(obstacles[i].Position);
                ep = ep.Substract(obstacles[i].Position);

                Vec2 d = ep.Substract(sp);

                double a = d.X * d.X + d.Y * d.Y;
                double b = 2 * (sp.X * d.X + sp.Y * d.Y);
                double c = sp.X * sp.X + sp.Y * sp.Y - obstacles[i].Radius * obstacles[i].Radius;

                bool flag;
                if (-b < 0)
                    flag = (c < 0);
                else if (-b < (2 * a))
                    flag = ((4 * a * c - b * b) < 0);
                else
                    flag = (a + b + c < 0);

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

        public static Obstacle? RaycastObstacleWithAllies(Vec2 startPoint, Vec2 endPoint, Obstacle[] obstacles,List<Unit> myUnits,
            int unitId,double unitRadius,
            bool ignoreLowObstacles)
        {
            var obst = RaycastObstacle(startPoint, endPoint, obstacles, ignoreLowObstacles);
            var allieObst = RaycastAllies(startPoint, endPoint, myUnits,unitId,unitRadius, ignoreLowObstacles);
            if (!obst.HasValue)
            {
                return allieObst;
            }
            else if(!allieObst.HasValue)
            {
                return obst;
            }
            else
            {
                if (allieObst.Value.Position.SqrDistance(startPoint) < obst.Value.Position.SqrDistance(startPoint))
                {
                    return allieObst;
                }
                else
                {
                    return obst;
                }
            }
        }

        public static Obstacle? RaycastAllies(Vec2 startPoint, Vec2 endPoint,
            List<Unit> myUnits,
            int unitId, double unitRadius,
            bool ignoreLowObstacles)
        {
            Obstacle[] allies = new Obstacle[myUnits.Count-1];
            bool flag = false;
            for (int i = 0; i < myUnits.Count; i++)
            {
                if (myUnits[i].Id != unitId)
                {
                    var ind = (flag) ? i - 1 : i;
                    var r = unitRadius;
                    if (myUnits[i].RemainingSpawnTime.HasValue)
                    {
                        r = 0;
                    }

                    allies[ind] = new Obstacle(0,myUnits[i].Position,r,false,false);
                }
                else
                {
                    flag = true;
                }
            }
           return RaycastObstacle(startPoint, endPoint, allies, ignoreLowObstacles);
        }

        /// <summary>
        /// Обнаружение препятствий по 2-м точкам
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="width">Ширина юнита</param>
        /// <param name="obstacles"></param>
        /// <param name="ignoreLowObstacles"></param>
        /// <returns></returns>
        public static Obstacle? RaycastObstacle2Point(Vec2 startPoint, Vec2 endPoint, double width, Obstacle[] obstacles,
            bool ignoreLowObstacles, DebugInterface debugInterface = null)
        {
            Straight s = new Straight();
            var normal = endPoint.Substract(startPoint);
            s.SetByNormalAndPoint(normal, startPoint);
            var directive = s.GetDirective().Normalize();

            var point1 = startPoint.Substract(directive.Multi(width / 2));
            var point2 = startPoint.Substract(directive.Multi(-width / 2));
            var o1 = RaycastObstacle(point1, endPoint, obstacles, ignoreLowObstacles);
            var o2 = RaycastObstacle(point2, endPoint, obstacles, ignoreLowObstacles);
            if (debugInterface != null)
            {
                debugInterface.AddSegment(point1, endPoint, 0.2, new Debugging.Color(1, 0, 1, 0.5)); //Фиолетовый
                debugInterface.AddSegment(point2, endPoint, 0.2, new Debugging.Color(0, 1, 1, 0.5)); //Синий
            }
            if (!o1.HasValue)
                return o2;
            if (!o2.HasValue)
                return o1;
            if( o1.Value.Position.SqrDistance(startPoint) < o2.Value.Position.SqrDistance(startPoint))            
                return o1;            
            return o2;
        }
        /// <summary>
        /// Определяет принадлежит ли точка полю обзора
        /// </summary>
        /// <param name="point"></param>
        /// <param name="startPos"></param>
        /// <param name="viewDistance"></param>
        /// <param name="viewAngle"></param>
        /// <returns></returns>
        public static bool BelongConeOfVision(Vec2 point, Vec2 startPos, Vec2 viewDirection, double viewDistance, double viewAngle)
        {
            if (!BelongDirection(point,startPos,viewDirection,viewAngle))
            {
                return false;
            }

            return startPos.Distance(point) < viewDistance;
        }


        public static bool BelongDirection(Vec2 point,Vec2 startPos,Vec2 viewDirection, double viewAngle)
        {
            var pointAngle = AngleToPoint(startPos, point);
            var viewDirAngle = AngleToPoint(new Vec2(0, 0), viewDirection);

            return IsInside(viewDirAngle, viewAngle, pointAngle);
        }

        public static double AngleToPoint(Vec2 yourPosition, Vec2 point)
        {
            return (Math.Atan2(point.Y - yourPosition.Y,
                point.X - yourPosition.X) * (180 / Math.PI));
        }

        public static double AngleDiff(double angle1, double angle2)
        {
            return ((((angle1 - angle2) % 360) + 540) % 360) - 180;
        }

        public static bool IsInside(double rayAngle, double alpha, double pointAngle)
        {
            return (AngleDiff(pointAngle, rayAngle - alpha) > 0 && AngleDiff(pointAngle, rayAngle + alpha) < 0);
        }

        public static double TicksToTime(int ticks, double ticksPerSecond)
        {
            return (double)ticks / ticksPerSecond;
        }

        public static int TimeToTicks(double time, double ticksPerSecond)
        {
            return (int)Math.Ceiling(time * ticksPerSecond);
        }
    }
}
