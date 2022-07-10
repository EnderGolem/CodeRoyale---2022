using AiCup22.Model;

namespace AiCup22.Custom
{
    public class RadarBrain:Brain
    {
        private LookAroundAction _lookAroundAction;
        public RadarBrain()
        {
            _lookAroundAction = new LookAroundAction();
            allStates.Add(_lookAroundAction);
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            return _lookAroundAction;
        }
        
    }
}