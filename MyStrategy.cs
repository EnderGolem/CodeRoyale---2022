using System.Collections.Generic;
using AiCup22.Model;
using AiCup22.Custom;
using System;
using System.IO;

namespace AiCup22
{

    public class MyStrategy
    {
        private Perception perception;
        private Brain brain;
        private Game lastGame;
        public MyStrategy(AiCup22.Model.Constants constants)
        {
            perception = new Perception(constants);
            brain = new GeneralBrain();
        }

        public AiCup22.Model.Order GetOrder(AiCup22.Model.Game game, DebugInterface debugInterface)
        {
            lastGame = game;
            Dictionary<int, AiCup22.Model.UnitOrder> orders = new Dictionary<int, UnitOrder>();
            try
            {
                perception.Analyze(game, debugInterface);
                orders.Add(perception.MyUnints[0].Id, brain.Process(perception, debugInterface));

            }
            catch (Exception e)
            {
                debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center, $"Message: {e.Message}\nTrace: {e.StackTrace}\nSource: {e.Source}", new Vec2(0, 0), 10, new Debugging.Color(0, 0, 0, 1));
            }
            return new Order(orders);
        }
        public void addText()
        {

            FileInfo fileInf = new FileInfo("ressss.csv");
            var sw = fileInf.AppendText();
            int myPlayersId = 0;
            for (int i = 0; i < lastGame.Players.Length; i++)
            {
                if (lastGame.Players[i].Id == lastGame.MyId)
                {
                    myPlayersId = i;
                    break;
                }
            }
            System.Console.WriteLine($"{lastGame.Players[myPlayersId].Score};{lastGame.Players[myPlayersId].Kills};{lastGame.Players[myPlayersId].Damage}; \n");
            sw.WriteLine($"{lastGame.Players[myPlayersId].Score};{lastGame.Players[myPlayersId].Kills};{lastGame.Players[myPlayersId].Damage};{lastGame.Players[myPlayersId].Place}");
            sw.Close();
        }
        public void DebugUpdate(int displayedTick, DebugInterface debugInterface) { }
        public void Finish() { }
    }
}