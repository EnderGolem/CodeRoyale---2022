using AiCup22.Model;

namespace AiCup22.Custom
{
    public class GeneralBrain : Brain
    {
        private LootingBrain _lootingBrain;
        private BattleBrain _battleBrain;
        private RadarBrain _radarBrain;

        public GeneralBrain()
        {
            _lootingBrain = new LootingBrain();
            _battleBrain = new BattleBrain();
            _radarBrain = new RadarBrain();
            allStates.Add(_lootingBrain);
            allStates.Add(_battleBrain);
            allStates.Add(_radarBrain);
        }

        public override UnitOrder Process(Perception perception,DebugInterface debugInterface)
        {
            Activate(perception.Game.CurrentTick);
            //currentState = _radarBrain;
            if (perception.MyUnints[id].Weapon.HasValue && perception.MyUnints[0].Weapon.Value == 2 &&
                perception.EnemyUnints.Count > 0 && perception.MyUnints[id].Ammo[2] > 0)
            {
                currentState = _battleBrain;
            }
            else
                currentState =  _lootingBrain;
            for (int i = 0; i < allStates.Count; i++)
            {
                
            }
            return currentState.Process(perception, debugInterface);
        }
    }
}