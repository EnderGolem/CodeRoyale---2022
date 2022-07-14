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
        private int countHit;
        public MyStrategy(AiCup22.Model.Constants constants)
        {
            countHit = 0;
            perception = new Perception(constants);
            brain = new GeneralBrain();
            lastGame = new Game();
        }

        private int FindMyUnitId()
        {
            int myPlayersId = -1;
            if (lastGame.Players != null)
                for (int i = 0; i < lastGame.Players.Length; i++)
                {
                    if (lastGame.Players[i].Id == lastGame.MyId)
                    {
                        myPlayersId = i;
                        break;
                    }
                }
            return myPlayersId;
        }

        public AiCup22.Model.Order GetOrder(AiCup22.Model.Game game, DebugInterface debugInterface)
        {
            int myId = FindMyUnitId();
            if (myId != -1 && (lastGame.Units[myId].Health + lastGame.Units[myId].Shield) - (game.Units[myId].Health + game.Units[myId].Shield) > 0)
                countHit++;
            lastGame = game;
            Dictionary<int, AiCup22.Model.UnitOrder> orders = new Dictionary<int, UnitOrder>();
            //try
            //{

            perception.Analyze(game, debugInterface);
            orders.Add(perception.MyUnints[0].Id, brain.Process(perception, debugInterface));

            //}
            //catch (Exception e)
            //{
            //    if (debugInterface != null)
            //        debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center, $"Message: {e.Message}\nTrace: {e.StackTrace}\nSource: {e.Source}", new Vec2(0, 0), 10, new Debugging.Color(0, 0, 0, 1));
            //}
            return new Order(orders);
        }

        public void addText(string fileName)
        {

            FileInfo fileInf = new FileInfo(fileName);
            var sw = fileInf.AppendText();
            int myPlayersId = FindMyUnitId();
            string s = $"{lastGame.Players[myPlayersId].Score};{lastGame.Players[myPlayersId].Kills};{lastGame.Players[myPlayersId].Damage};{lastGame.Players[myPlayersId].Place};{brain.TimeStates[0]};{brain.TimeStates[1]};{brain.TimeStates[2]};{brain.TimeStates[3]};{countHit};";
            System.Console.WriteLine(s);
            sw.WriteLine(s);
            sw.Close();
        }

        public void DebugUpdate(int displayedTick, DebugInterface debugInterface) { }
        public void Finish() { }
    }
}