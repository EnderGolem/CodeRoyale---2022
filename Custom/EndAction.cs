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
            ActionOrder action = new ActionOrder.Aim(false);
            Unit unit = perception.MyUnints[0];
            return new UnitOrder(new Vec2(-unit.Position.X, -unit.Position.Y),
               new Vec2(-unit.Direction.Y, unit.Direction.X), action);
        }
    }


}