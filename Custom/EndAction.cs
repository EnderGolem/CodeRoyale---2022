using AiCup22.Model;

namespace AiCup22.Custom
{
    public class EndAction
    {
        public UnitOrder Process(Perception perception, ref int id)
        {
            throw new System.NotImplementedException();
        }
    }

    public class RunToCenter : EndAction
    {
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
                new Vec2(-unit.Position.X, -unit.Position.Y), action);
        }
    }

    public class RunShootToCenter : EndAction
    {
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
                new Vec2(-unit.Position.X, -unit.Position.Y), action);
        }
    }

    public class RunToCenterRadar : EndAction
    {
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }

    public class RunShootToCenterRadar : EndAction
    {
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Aim(true);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }



    public class StayShootToEnemy : EndAction
    {
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Aim(true);
            Unit unit = perception.MyUnints[0];
            Unit target = perception.EnemyUnints[0];
            Vec2 enemy = target.Position.Subtract(unit.Position);
            return new UnitOrder(new Vec2(0, 0),
             enemy, action);
        }
    }

    public class RunToDestination : EndAction
    {
        protected Vec2 destination;
        public virtual UnitOrder Process(Perception perception, int id)
        {
            Unit unit = perception.MyUnints[0];
            var dir = destination.Subtract(unit.Position).Normalize().Multi(perception.Constants.MaxUnitForwardSpeed);
            return new UnitOrder(dir, dir, null);
        }

        public void SetDestination(Vec2 dest)
        {
            destination = dest;
        }
    }

    public class SteeringRunToDestination : RunToDestination
    {
        public override UnitOrder Process(Perception perception, int id)
        {
            Obstacle? obst = Tools.RaycastObstacle(perception.MyUnints[0].Position, destination, perception.Constants.Obstacles, false);
            if (!obst.HasValue)
            {
                return base.Process(perception, id);
            }
            else
            {
                return new UnitOrder(new Vec2(), new Vec2(), new ActionOrder.UseShieldPotion());
            }
        }
    }
    public class UseShield
    {
        public UnitOrder Process(Perception perception, int id)
        {
            return new UnitOrder(new Vec2(), new Vec2(), new ActionOrder.UseShieldPotion());
        }

    }
    public class PickupLoot
    {
        private int pickableLootId;
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Pickup(pickableLootId);

            //Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(), new Vec2(), action);
        }

        public void SetPickableLootId(int id)
        {
            pickableLootId = id;
        }
    }

    public class ShootToPoint
    {
        private Vec2 target;
        public ShootToPoint()
        {
            target = new Vec2();
        }
        public UnitOrder Process(Perception perception, int id)
        {
            ActionOrder action = new ActionOrder.Aim(true);
            Unit unit = perception.MyUnints[id];
            Vec2 enemy = target.Subtract(unit.Position);
            return new UnitOrder(new Vec2(), enemy, action);
        }

        public void SetTarget(Vec2 _target)
        {
            target = _target;
        }
    }
    public class AimingToPoint
    {
        private Vec2 target;
        public AimingToPoint()
        {
            target = new Vec2();
        }
        public UnitOrder Process(Perception perception, int id)
        {

            ActionOrder action = new ActionOrder.Aim(false); //Исправить, не знаю будет ли прицеливаться...
            Unit unit = perception.MyUnints[id];
            Vec2 enemy = target.Subtract(unit.Position);
            return new UnitOrder(new Vec2(), enemy, action);
        }

        public void SetTarget(Vec2 _target)
        {
            target = _target;
        }
    }

}