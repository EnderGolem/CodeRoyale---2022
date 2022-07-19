using System;
using System.Linq;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class EndAction : Processable<UnitOrder>
    {
        private int _lastActivationTick;
        private int _lastDeactivationTick;
        private bool _isActive;
        protected Unit actingUnit;

        public int LastActivationTick
        {
            get => _lastActivationTick;
            set => _lastActivationTick = value;
        }

        public int LastDeactivationTick
        {
            get => _lastDeactivationTick;
            set => _lastDeactivationTick = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public void Activate(int currentGameTick)
        {
            if (!IsActive)
            {
                IsActive = true;
                LastActivationTick = currentGameTick;
            }
        }

        public void Deactivate(int currentGameTick)
        {
            if (IsActive)
            {
                IsActive = false;
                LastDeactivationTick = currentGameTick;
            }
        }

        public virtual UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            return new UnitOrder();
        }

        public void SetActingUnit(Unit unit)
        {
            actingUnit = unit;
        }

        public EndAction Copy()
        {
            return (EndAction)this.MemberwiseClone();
        }
    }

    public class RunToCenter : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = actingUnit;
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
                new Vec2(-unit.Position.X, -unit.Position.Y), action);
        }
    }

    public class RunShootToCenter : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = actingUnit;
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
                new Vec2(-unit.Position.X, -unit.Position.Y), action);
        }
    }

    public class RunToCenterRadar : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = actingUnit;
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }

    public class RunShootToCenterRadar : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(true);
            Unit unit = actingUnit;
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }


    public class RunToDestination : EndAction
    {
        protected Vec2 destination;

        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = actingUnit;
            var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
            return new UnitOrder(dir, dir, null);
        }

        public virtual void SetDestination(Vec2 dest)
        {
            destination = dest;
        }
    }

    public class SteeringRunToDestination : RunToDestination
    {

        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            /*Obstacle? obst = Tools.RaycastObstacle2Point(perception.MyUnints[0].Position, destination, perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);

            if (!obst.HasValue || obst.Value.Position.SqrDistance(perception.MyUnints[0].Position) > obst.Value.Radius * obst.Value.Radius * 9)
            {
                return base.Process(perception, debugInterface);
            }
            else
            {
                Unit unit = perception.MyUnints[0];
                var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
                Straight perpS = new Straight();
                perpS.SetByNormalAndPoint(dir, obst.Value.Position);
                var dirS = new Straight(dir, unit.Position);
                var intersectPoint = dirS.GetIntersection(perpS);
                var perpDir = obst.Value.Position.Substract(intersectPoint.Value);
                var targetPos = obst.Value.Position.Substract(perpDir.Normalize().Multi(obst.Value.Radius + 3 * perception.Constants.UnitRadius));
                var targetDir = targetPos.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);

                /* Console.WriteLine($"Прямая перпендикулярная цели: {perpS}");
                 Console.WriteLine($"Прямая до цели: {dirS}");
                 Console.WriteLine($"Точка пересечения: {intersectPoint}");
                 Console.WriteLine($"Центр препятствия: {obst.Value.Position}");
                 Console.WriteLine($"Перпендикулярный вектор: {perpDir}");
                 Console.WriteLine($"Целевая позиция: {targetPos}");
                 Console.WriteLine($"Целевой вектор: {targetDir}");*/
            var targetDir = CalculateDirToDestinationWithObstAvoidance(perception, debugInterface);
            return new UnitOrder(targetDir, targetDir, null);
        }

        protected Vec2 CalculateDirToDestinationWithObstAvoidance(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = actingUnit;
            Obstacle? obst = Tools.RaycastObstacle2Point(unit.Position, destination,
                perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);

            if (!obst.HasValue || obst.Value.Position.SqrDistance(unit.Position) >
                obst.Value.Radius * obst.Value.Radius * 9)
            {
                return destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
            }
            else
            {
                var dir = destination.Substract(unit.Position).Normalize()
                    .Multi(perception.Constants.MaxUnitForwardSpeed);
                Straight perpS = new Straight();
                perpS.SetByNormalAndPoint(dir, obst.Value.Position);
                var dirS = new Straight(dir, unit.Position);
                var intersectPoint = dirS.GetIntersection(perpS);
                var perpDir = obst.Value.Position.Substract(intersectPoint.Value);
                var targetPos = obst.Value.Position.Substract(perpDir.Normalize()
                    .Multi(obst.Value.Radius + 3 * perception.Constants.UnitRadius));
                var targetDir = targetPos.Substract(unit.Position).Normalize()
                    .Multi(perception.Constants.MaxUnitForwardSpeed);

                /* Console.WriteLine($"Прямая перпендикулярная цели: {perpS}");
                 Console.WriteLine($"Прямая до цели: {dirS}");
                 Console.WriteLine($"Точка пересечения: {intersectPoint}");
                 Console.WriteLine($"Центр препятствия: {obst.Value.Position}");
                 Console.WriteLine($"Перпендикулярный вектор: {perpDir}");
                 Console.WriteLine($"Целевая позиция: {targetPos}");
                 Console.WriteLine($"Целевой вектор: {targetDir}");*/

                if (debugInterface != null)
                {
                    debugInterface.AddRing(intersectPoint.Value, 1, 0.5, new Color(1, 0, 0, 1));
                    debugInterface.AddRing(targetPos, 1, 0.5, new Color(0, 0.5, 0.5, 1));
                    debugInterface.AddSegment(obst.Value.Position, obst.Value.Position.Substract(perpDir.Normalize()), 0.5, new Color(0, 0, 1, 1));
                    debugInterface.AddSegment(unit.Position, destination, 0.5, new Color(0, 1, 0, 1));
                    debugInterface.AddSegment(obst.Value.Position, targetPos, 0.5, new Color(1, 0, 0, 1));
                }
                
                return targetDir;
            }
        }
    }

    public class UseShield : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            return new UnitOrder(new Vec2(), new Vec2(), new ActionOrder.UseShieldPotion());
        }
    }
    public class UseShieldToDestination : RunToDestinationDirection
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            var order = base.Process(perception, debugInterface);
            order.Action = new ActionOrder.UseShieldPotion();
            return order;
        }
    }
    public class PickupLoot : EndAction
    {
        private int pickableLootId;
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Pickup(pickableLootId);
            return new UnitOrder(new Vec2(), new Vec2(), action);
        }

        public void SetPickableLootId(int id)
        {
            pickableLootId = id;
        }
    }

    public class RunToDestinationDirection : RunToDestination
    {
        protected Vec2 direction;
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = actingUnit;
            var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
            var enemy = direction.Substract(unit.Position);
            return new UnitOrder(dir, enemy, null);
        }
        public virtual void SetDirection(Vec2 dir) //Измени везде название тут
        {
            direction = dir;
        }
    }
    public class AimToDestinationDirection : RunToDestinationDirection
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = actingUnit;
            var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
            var enemy = direction.Substract(unit.Position);
            ActionOrder action = new ActionOrder.Aim(false);
            return new UnitOrder(dir, enemy, action);
        }

    }
    public class ShootToDestinationDirection : RunToDestinationDirection
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = actingUnit;
            var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
            var enemy = direction.Substract(unit.Position);
            ActionOrder action = new ActionOrder.Aim(true);
            return new UnitOrder(dir, enemy, action);
        }

    }

    public class SteeringAimToDestinationDirection : AimToDestinationDirection
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Obstacle? obst = Tools.RaycastObstacle2Point(actingUnit.Position, destination, perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);
            if (!obst.HasValue || obst.Value.Position.SqrDistance(actingUnit.Position) > obst.Value.Radius * obst.Value.Radius * 9)
            {
                return base.Process(perception, debugInterface);
            }
            else
            {

                Unit unit = actingUnit;
                var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
                Straight perpS = new Straight();
                perpS.SetByNormalAndPoint(dir, obst.Value.Position);
                var dirS = new Straight(dir, unit.Position);
                var intersectPoint = dirS.GetIntersection(perpS);
                var perpDir = obst.Value.Position.Substract(intersectPoint.Value);
                var targetPos = obst.Value.Position.Substract(perpDir.Normalize().Multi(obst.Value.Radius + 3 * perception.Constants.UnitRadius));
                var targetDir = targetPos.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
                var enemy = direction.Substract(unit.Position);
                ActionOrder action = new ActionOrder.Aim(false);
                if (debugInterface != null)
                {
                    debugInterface.AddRing(intersectPoint.Value, 1, 0.5, new Color(1, 0, 0, 1));
                    debugInterface.AddRing(targetPos, 1, 0.5, new Color(0, 0.5, 0.5, 1));
                    debugInterface.AddSegment(obst.Value.Position, obst.Value.Position.Substract(perpDir.Normalize()), 0.5, new Color(0, 0, 1, 1));
                    debugInterface.AddSegment(unit.Position, destination, 0.5, new Color(0, 1, 0, 1));
                    debugInterface.AddSegment(obst.Value.Position, targetPos, 0.5, new Color(1, 0, 0, 1));
                }
                return new UnitOrder(targetDir, enemy, action);
            }
        }

    }

    public class SteeringShootToDestinationDirection : ShootToDestinationDirection
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Obstacle? obst = Tools.RaycastObstacle2Point(actingUnit.Position, destination, perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);
            if (!obst.HasValue || obst.Value.Position.SqrDistance(actingUnit.Position) > obst.Value.Radius * obst.Value.Radius * 9)
            {
                return base.Process(perception, debugInterface);
            }
            else
            {

                Unit unit = actingUnit;
                var dir = destination.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
                Straight perpS = new Straight();
                perpS.SetByNormalAndPoint(dir, obst.Value.Position);
                var dirS = new Straight(dir, unit.Position);
                var intersectPoint = dirS.GetIntersection(perpS);
                var perpDir = obst.Value.Position.Substract(intersectPoint.Value);
                var targetPos = obst.Value.Position.Substract(perpDir.Normalize().Multi(obst.Value.Radius + 3 * perception.Constants.UnitRadius));
                var targetDir = targetPos.Substract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);            
                var enemy = direction.Substract(unit.Position);
                ActionOrder action = new ActionOrder.Aim(true);
                if (debugInterface != null)
                {
                    debugInterface.AddRing(intersectPoint.Value, 1, 0.5, new Color(1, 0, 0, 1));
                    debugInterface.AddRing(targetPos, 1, 0.5, new Color(0, 0.5, 0.5, 1));
                    debugInterface.AddSegment(obst.Value.Position, obst.Value.Position.Substract(perpDir.Normalize()), 0.5, new Color(0, 0, 1, 1));
                    debugInterface.AddSegment(unit.Position, destination, 0.5, new Color(0, 1, 0, 1));
                    debugInterface.AddSegment(obst.Value.Position, targetPos, 0.5, new Color(1, 0, 0, 1));
                }
                return new UnitOrder(targetDir, enemy, action);
            }
        }

    }

    public class LookAroundAction : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            var unit = actingUnit;
            Vec2 dir = new Vec2(-unit.Direction.Y, unit.Direction.X);
            if (actingUnit.Shield < 150)
            {
                ActionOrder action = new ActionOrder.UseShieldPotion();
                return new UnitOrder(dir, dir, action);
            }
            return new UnitOrder(dir, dir, null);
        }
    }

    public class Evading : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = actingUnit;
            if (perception.MemorizedProjectiles.Count == 0)
            {
                return new UnitOrder(new Vec2(),new Vec2(-unit.Direction.Y, unit.Direction.X),null );
            }

            Vec2 dir = perception.SimulateEvading(actingUnit,0,perception.CloseObstacles,
                perception.MemorizedProjectiles.Values.ToList(),3,perception.Game.Zone.CurrentCenter.Substract(unit.Position).Normalize(),1, 10, debugInterface);
            debugInterface?.AddSegment(unit.Position,unit.Position.Add(dir.Multi(5)),0.5,new Color(1,1,0,0.3));
            //Console.WriteLine(dir);
            return new UnitOrder(dir.Multi(perception.Constants.MaxUnitForwardSpeed),new Vec2(), null);
        }
    }

    public class SteeringRunToDestinationWithEvading : SteeringRunToDestination
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
                var targetDir = CalculateDirToDestinationWithObstAvoidance(perception, debugInterface);
                var projs = perception.ClipSafeProjectiles(ref actingUnit);
                if (projs.Count == 0 || actingUnit.RemainingSpawnTime.HasValue)
                {
                    return new UnitOrder(targetDir, targetDir, null);
                }
                else
                {
                    Vec2 d = perception.SimulateEvading(actingUnit,0,perception.CloseObstacles,
                            projs,3,targetDir.Normalize(),1, 10, debugInterface);
                    debugInterface?.AddSegment(actingUnit.Position,actingUnit.Position.Add(d.Multi(5)),0.5,new Color(1,1,0,0.3));
                    //Console.WriteLine(dir);
                    return new UnitOrder(d.Multi(perception.Constants.MaxUnitForwardSpeed),actingUnit.Direction, null);
                }
        }
    }

    public class UseShieldToDestinationWithEvading:SteeringRunToDestination
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            var targetDir = CalculateDirToDestinationWithObstAvoidance(perception, debugInterface);
            var projs = perception.ClipSafeProjectiles(ref actingUnit);
            if (projs.Count == 0)
            {
                return new UnitOrder(targetDir, targetDir, null);
            }
            else
            {
                Vec2 d = perception.SimulateEvading(actingUnit,0,perception.CloseObstacles,
                    projs,3,targetDir.Normalize(),1,10,debugInterface);
                debugInterface?.AddSegment(actingUnit.Position,actingUnit.Position.Add(d.Multi(5)),0.5,new Color(1,1,0,0.3));
                //Console.WriteLine(dir);
                return new UnitOrder(d.Multi(perception.Constants.MaxUnitForwardSpeed),actingUnit.Direction, new ActionOrder.UseShieldPotion());
            }
        }
    }
    
    public class LookAroundWithEvading : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            var unit = actingUnit;
            Vec2 dir = new Vec2(-unit.Direction.Y, unit.Direction.X);
            Vec2 dirToCenter = perception.Game.Zone.CurrentCenter.Substract(unit.Position);
            var projs = perception.ClipSafeProjectiles(ref actingUnit);
            if (projs.Count == 0)
            {
                if (actingUnit.Shield < 150)
                {
                    ActionOrder action = new ActionOrder.UseShieldPotion();
                    return new UnitOrder(dirToCenter, dir, action);
                }
                return new UnitOrder(dirToCenter, dir, null);
            }
            Vec2 d = perception.SimulateEvading(actingUnit,-1,perception.CloseObstacles,
                projs,3,dirToCenter,1, 10, debugInterface);
                debugInterface?.AddSegment(actingUnit.Position,actingUnit.Position.Add(d.Multi(5)),0.5,new Color(1,1,0,0.3));
            ActionOrder a = null;
            if (actingUnit.Shield < 160)
            {
                a = new ActionOrder.UseShieldPotion();
            }
            return new UnitOrder(d.Multi(perception.Constants.MaxUnitForwardSpeed),dir, a);

        }
    }
}

    