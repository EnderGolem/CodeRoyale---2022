using AiCup22.Model;

namespace AiCup22.Custom
{
    public class RadarBrain:Brain
    {
        private LookAroundWithEvading _lookAroundAction;
        public RadarBrain()
        {
            _lookAroundAction = new LookAroundWithEvading();
            allStates.Add(_lookAroundAction);
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            return _lookAroundAction;
        }
        
    }
}