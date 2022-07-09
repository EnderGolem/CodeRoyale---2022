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

        public override UnitOrder Process(Perception perception)
        {
            if (perception.MyUnints[id].Weapon.HasValue && perception.MyUnints[0].Weapon.Value == 2 && perception.EnemyUnints.Count > 0 && perception.MyUnints[id].Ammo[2] > 0)
            {
                return _battleBrain.Process(perception);
            }
            else
                return _lootingBrain.Process(perception);
        }
    }
}