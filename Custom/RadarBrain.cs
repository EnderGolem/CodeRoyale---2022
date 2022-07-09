using AiCup22.Model;

namespace AiCup22.Custom
{
    public class RadarBrain:Brain
    {
        private LookAroundAction _lookAroundAction;
        public RadarBrain()
        {
            _lookAroundAction = new LookAroundAction();
        }

        public override UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            return _lookAroundAction.Process(perception,0);
        }
    }
}