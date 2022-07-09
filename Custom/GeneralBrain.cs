using AiCup22.Model;

namespace AiCup22.Custom
{
    public class GeneralBrain : Brain
    {
        private LootingBrain _lootingBrain;

        public GeneralBrain()
        {
            _lootingBrain = new LootingBrain();
        }

        public override UnitOrder Process(Perception perception)
        {
            return _lootingBrain.Process(perception);
        }
    }
}