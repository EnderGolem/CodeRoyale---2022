using System.Collections.Generic;
using AiCup22.Model;
using AiCup22.Custom;
using System;
using System.IO;
using System.Linq;
using AiCup22.Debugging;

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
            //try
            //{
            perception.Analyze(game, debugInterface);
            var order = brain.Process(perception, debugInterface);
            orders.Add(perception.MyUnints[0].Id, order);
            
            
               /* var unit = perception.SimulateUnitMovement(perception.MyUnints[0], order, perception.CloseObstacles,
                    perception.MemorizedProjectiles.Values.ToList(), 3, game.CurrentTick,debugInterface);   
            
            debugInterface.AddRing(unit.Position,1.4,0.2,new Color(0,0,1,1));*/
            //}
            //catch (Exception e)
            //{
            //    if (debugInterface != null)
            //        debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center, $"Message: {e.Message}\nTrace: {e.StackTrace}\nSource: {e.Source}", new Vec2(0, 0), 10, new Debugging.Color(0, 0, 0, 1));
            //}
            return new Order(orders);
        }
        public void addText()
        {

            FileInfo fileInf = new FileInfo("Ver5_5.csv");
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