using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22
{
    public class Perception
    {
        private Game game;
        private DebugInterface debug;
        private Constants constants;

        public Constants Constants => constants;

        public Game Game => game;

        public DebugInterface Debug => debug;
        public Perception(Constants consts)
        {
            constants = consts;
        }

        public void Analyze(Game game, DebugInterface debugInterface)
        {
            
        }
    }

    public interface Processable
    {
        public UnitOrder Process(Perception perception);
    }

    public class Brain: Processable
    {
        /// <summary>
        /// Здесь дожен быть какой - то общий список всех конечных действий на выбор данного мозга
        /// </summary>
        /// <param name="perception"></param>
        public UnitOrder Process(Perception perception)
        {
            return new UnitOrder();
        }
    }

    public class MyStrategy
    {
        int maxAttackDistance = 25;

        private Perception perception;

        public MyStrategy(AiCup22.Model.Constants constants)
        {
            perception = new Perception(constants);
            System.Console.WriteLine(constants.ViewDistance);
        }

        public AiCup22.Model.Order GetOrder(AiCup22.Model.Game game, DebugInterface debugInterface)
        {
            
            perception.Analyze(game,debugInterface);
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
                    action = new ActionOrder.Aim(true);
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