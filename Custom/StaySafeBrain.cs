using System.Linq;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class StaySafeBrain :Brain
    {
        private RunToDestination _runToDestination;
        
        public StaySafeBrain()
        {
            _runToDestination = new SteeringRunToDestination();
            allStates.Add(_runToDestination);
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            int safestDir = -1;
            double minDanger = double.MaxValue;
            for (int i = 0; i < perception.Directions.Length; i++)
            {
                int prev = (i==0)?7:i - 1;
                int next = (i + 1) % perception.Directions.Length;
                if (perception.DirectionDangers[i]==0 && perception.DirectionDangers[prev] == 0 && perception.DirectionDangers[next]==0)
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
            _runToDestination.SetDestination(perception.MyUnints[0].Position.Add(perception.Directions[safestDir].Multi(100)));

            return _runToDestination;
        }
    }
}