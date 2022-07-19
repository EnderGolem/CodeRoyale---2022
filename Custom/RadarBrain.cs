using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class RadarBrain:EndBrain
    {
        public RadarBrain(Perception perception):base(perception)
        {
            AddState("LookAround", new LookAroundWithEvading(), perception);
        }

        protected override Dictionary<int,EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();
            foreach (var unit in perception.MyUnints)
            {
                orderedEndActions[unit.Id] = (LookAroundWithEvading)GetAction(unit.Id, "LookAround");
            }
            return orderedEndActions;
        }
        
    }
}