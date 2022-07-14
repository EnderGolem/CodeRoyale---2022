using System.Linq;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class StaySafeBrain :Brain
    {
        private RunToDestination _runToDestination;
        private UseShieldToDestination _useShieldToDestination;

        public StaySafeBrain()
        {
            _runToDestination = new SteeringRunToDestinationWithEvading();
            _useShieldToDestination = new UseShieldToDestination();
            allStates.Add(_runToDestination);
            allStates.Add(_useShieldToDestination);
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            int safestDir = -1;
            double minDanger = double.MaxValue;
            for (int i = 0; i < perception.Directions.Length; i++) //Мне кажется тут ошибка т.к. сейчас DirectionDangers указывает опасность в дргую сторону
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

            if (perception.MyUnints[0].Shield < 160)
            {
                _useShieldToDestination.SetDestination(perception.MyUnints[0].Position.Add(perception.Directions[safestDir].Multi(100)));
                return _useShieldToDestination;
            }
            _runToDestination.SetDestination(perception.MyUnints[0].Position.Add(perception.Directions[safestDir].Multi(100)));

            return _runToDestination;
        }
    }
}