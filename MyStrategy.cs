using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22
{
    public class MyStrategy
    {
        int maxAttackDistance = 25;
        public MyStrategy(AiCup22.Model.Constants constants) { System.Console.WriteLine(constants.ViewDistance); }

        public AiCup22.Model.Order GetOrder(AiCup22.Model.Game game, DebugInterface debugInterface)
        {
            Dictionary<int, AiCup22.Model.UnitOrder> orders = new Dictionary<int, UnitOrder>();

            Unit target = new Unit();
            bool changed = false;
            foreach (var unit in game.Units)
            {
                if (unit.PlayerId != game.MyId)
                {
                    changed = true;
                    target = unit;

                    continue;
                }
                ActionOrder action = new ActionOrder.Aim(false);
                System.Console.WriteLine(target.Position.SqrDistance(unit.Position));
                UnitOrder order = new UnitOrder(
                    new Vec2(-unit.Position.X, -unit.Position.Y), new Vec2(-unit.Position.X, -unit.Position.Y), action);
                if (changed && target.Position.SqrDistance(unit.Position) < maxAttackDistance * maxAttackDistance)
                {
                    Vec2 enemy = target.Position.Subtract(unit.Position);
                    action = new ActionOrder.Aim(true);
                    order = new UnitOrder(enemy, enemy, action);
                }
                if (changed && target.Position.SqrDistance(unit.Position) <= 13 * 13)
                {
                    Vec2 enemy = target.Position.Subtract(unit.Position);
                    action = new ActionOrder.Aim(false);
                    order = new UnitOrder(unit.Position, unit.Direction, action);
                }
                orders.Add(unit.Id, order);
            }
            return new Order(orders);
        }
        public void DebugUpdate(DebugInterface debugInterface) { }
        public void Finish() { }
    }
    
}