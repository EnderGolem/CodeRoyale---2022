using System.Collections.Generic;
using AiCup22.Model;
using AiCup22.Custom;


namespace AiCup22
{

    public class MyStrategy
    {
        private Perception perception;
        private Brain brain;

        public MyStrategy(AiCup22.Model.Constants constants)
        {
            perception = new Perception(constants);
            brain = new GeneralBrain();
        }

        public AiCup22.Model.Order GetOrder(AiCup22.Model.Game game, DebugInterface debugInterface)
        {
            perception.Analyze(game, debugInterface);
            Dictionary<int, AiCup22.Model.UnitOrder> orders = new Dictionary<int, UnitOrder>();
            orders.Add(perception.MyUnints[0].Id, brain.Process(perception, debugInterface));
            return new Order(orders);
        }
        public void DebugUpdate(int displayedTick, DebugInterface debugInterface) { }
        public void Finish() { }
    }
}