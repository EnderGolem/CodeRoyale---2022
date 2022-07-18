using System.Collections.Generic;
using System.Linq;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class StaySafeBrain : EndBrain
    {
        private RunToDestination _runToDestination;
        private UseShieldToDestinationWithEvading _useShieldToDestination;

        public StaySafeBrain(Perception perception):base(perception)
        {
            _runToDestination = new SteeringRunToDestinationWithEvading();
            _useShieldToDestination = new UseShieldToDestinationWithEvading();
            AddState("Run",_runToDestination,perception);
            AddState("UseShield",_useShieldToDestination,perception);
        }

        protected override Dictionary<int,EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();
            int safestDir = -1;
            double minDanger = double.MaxValue;
            for (int i = 0; i < perception.Directions.Length; i++) //Мне кажется тут ошибка т.к. сейчас DirectionDangers указывает опасность в дргую сторону
            {
                int prev = (i == 0) ? 7 : i - 1;
                int next = (i + 1) % perception.Directions.Length;
                if (perception.DirectionDangers[i] == 0 && perception.DirectionDangers[prev] == 0 && perception.DirectionDangers[next] == 0)
                {
                    safestDir = i;
                    minDanger = perception.DirectionDangers[i];
                    break;
                }

                if (perception.DirectionDangers[i] < minDanger)
                {
                    safestDir = i;
                    minDanger = perception.DirectionDangers[i];
                }
            }

            if (perception.MyUnints[0].Shield < 160 && perception.MyUnints[0].ShieldPotions != 0)
            {
                _useShieldToDestination.SetDestination(perception.MyUnints[0].Position.Add(perception.Directions[safestDir].Multi(100)));
                orderedEndActions[perception.MyUnints[0].Id] = _useShieldToDestination;
                return orderedEndActions;
            }
            _runToDestination.SetDestination(perception.MyUnints[0].Position.Add(perception.Directions[safestDir].Multi(100)));
            orderedEndActions[perception.MyUnints[0].Id] = _runToDestination;
            return orderedEndActions;
        }
    }
}