using System;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class EndAction : Processable
    {
        private int _lastActivationTick;
        private int _lastDeactivationTick;
        private bool _isActive;

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
    }

    public class RunToCenter : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
                new Vec2(-unit.Position.X, -unit.Position.Y), action);
        }
    }

    public class RunShootToCenter : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
                new Vec2(-unit.Position.X, -unit.Position.Y), action);
        }
    }

    public class RunToCenterRadar : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }

    public class RunShootToCenterRadar : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            ActionOrder action = new ActionOrder.Aim(true);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }


    public class RunToDestination : EndAction
    {
        protected Vec2 destination;

        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = perception.MyUnints[0];
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
            Obstacle? obst = Tools.RaycastObstacle2Point(perception.MyUnints[0].Position, destination, perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);

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

                debugInterface.AddRing(intersectPoint.Value, 1, 0.5, new Color(1, 0, 0, 1));
                debugInterface.AddRing(targetPos, 1, 0.5, new Color(0, 0.5, 0.5, 1));
                debugInterface.AddSegment(obst.Value.Position, obst.Value.Position.Substract(perpDir.Normalize()), 0.5, new Color(0, 0, 1, 1));
                debugInterface.AddSegment(unit.Position, destination, 0.5, new Color(0, 1, 0, 1));
                debugInterface.AddSegment(obst.Value.Position, targetPos, 0.5, new Color(1, 0, 0, 1));
                return new UnitOrder(targetDir, dir, null);
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
            Unit unit = perception.MyUnints[0];
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
            Unit unit = perception.MyUnints[0];
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
            Unit unit = perception.MyUnints[0];
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
            Obstacle? obst = Tools.RaycastObstacle2Point(perception.MyUnints[0].Position, destination, perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);
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

                debugInterface.AddRing(intersectPoint.Value, 1, 0.5, new Color(1, 0, 0, 1));
                debugInterface.AddRing(targetPos, 1, 0.5, new Color(0, 0.5, 0.5, 1));
                debugInterface.AddSegment(obst.Value.Position, obst.Value.Position.Substract(perpDir.Normalize()), 0.5, new Color(0, 0, 1, 1));
                debugInterface.AddSegment(unit.Position, destination, 0.5, new Color(0, 1, 0, 1));
                debugInterface.AddSegment(obst.Value.Position, targetPos, 0.5, new Color(1, 0, 0, 1));
                var enemy = direction.Substract(unit.Position);
                ActionOrder action = new ActionOrder.Aim(false);
                return new UnitOrder(targetDir, enemy, action);
            }
        }

    }

    public class SteeringShootToDestinationDirection : ShootToDestinationDirection
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            Obstacle? obst = Tools.RaycastObstacle2Point(perception.MyUnints[0].Position, destination, perception.Constants.UnitRadius * 2, perception.Constants.Obstacles, false);
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

                debugInterface.AddRing(intersectPoint.Value, 1, 0.5, new Color(1, 0, 0, 1));
                debugInterface.AddRing(targetPos, 1, 0.5, new Color(0, 0.5, 0.5, 1));
                debugInterface.AddSegment(obst.Value.Position, obst.Value.Position.Substract(perpDir.Normalize()), 0.5, new Color(0, 0, 1, 1));
                debugInterface.AddSegment(unit.Position, destination, 0.5, new Color(0, 1, 0, 1));
                debugInterface.AddSegment(obst.Value.Position, targetPos, 0.5, new Color(1, 0, 0, 1));
                var enemy = direction.Substract(unit.Position);
                ActionOrder action = new ActionOrder.Aim(true);
                return new UnitOrder(targetDir, enemy, action);
            }
        }

    }

    public class LookAroundAction : EndAction
    {
        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            var unit = perception.MyUnints[0];
            Vec2 dir = new Vec2(-unit.Direction.Y, unit.Direction.X);
            if (perception.MyUnints[0].Shield < 150)
            {
                ActionOrder action = new ActionOrder.UseShieldPotion();
                return new UnitOrder(dir, dir, action);
            }
            return new UnitOrder(dir, dir, null);
        }
    }

}