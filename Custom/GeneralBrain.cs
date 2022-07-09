using AiCup22.Model;

namespace AiCup22.Custom
{
    public class GeneralBrain : Brain
    {
        private LootingBrain _lootingBrain;
        private BattleBrain _battleBrain;

        public GeneralBrain()
        {
            _lootingBrain = new LootingBrain();
            _battleBrain = new BattleBrain();
        }

        public override UnitOrder Process(Perception perception,DebugInterface debugInterface)
        {
            if (false/*perception.MyUnints[id].Weapon.HasValue && perception.MyUnints[0].Weapon.Value == 2*/)
            {
                return _battleBrain.Process(perception,debugInterface);
            }
            else
                return _lootingBrain.Process(perception,debugInterface);
        }
    }
}