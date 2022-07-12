using System.Collections.Generic;
using AiCup22.Model;
using AiCup22.Custom;
using System;

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
            Dictionary<int, AiCup22.Model.UnitOrder> orders = new Dictionary<int, UnitOrder>();
            try
            {
                perception.Analyze(game, debugInterface);
                orders.Add(perception.MyUnints[0].Id, brain.Process(perception, debugInterface));
            }
            catch (Exception e)
            {
                debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center, $"Message: {e.Message}\nTrace: {e.StackTrace}\nSource: {e.Source}", new Vec2(0, 0), 10, new Debugging.Color(1, 1, 1, 1));
            }
            return new Order(orders);
        }
        public void DebugUpdate(int displayedTick, DebugInterface debugInterface) { }
        public void Finish() { }
    }
}