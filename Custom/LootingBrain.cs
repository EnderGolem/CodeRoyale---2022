using System;
using AiCup22.Custom;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class LootingBrain : Brain
    {
        private RunToDestination _runToDestination;
        private PickupLoot _pickupLoot;
        private Loot desireLoot;
        private Vec2 desiredDestination;
        private double desiredPoints;

        public LootingBrain()
        {
            _runToDestination = new RunToDestination();
            _pickupLoot = new PickupLoot();
        }

        public override UnitOrder Process(Perception perception)
        {
            int bestLootIndex = -1;
            double bestPoints = double.MinValue;

            for (int i = 0; i < perception.Game.Loot.Length; i++)
            {

                double curPoints = CalculateLootValue(perception, perception.Game.Loot[i]);

                if (bestPoints < curPoints)
                {
                    bestPoints = curPoints;
                    bestLootIndex = i;
                }
            }

            Console.WriteLine(perception.Game.Loot[bestLootIndex].Position.Distance(perception.MyUnints[id].Position));
            if (perception.Game.Loot[bestLootIndex].Position.Distance(perception.MyUnints[id].Position) <
                perception.Constants.UnitRadius / 2)
            {
                Console.WriteLine("Start pickup!!!");
                _pickupLoot.SetPickableLootId(perception.Game.Loot[bestLootIndex].Id);
                return _pickupLoot.Process(perception, id);
            }
            else
            {
                _runToDestination.SetDestination(perception.Game.Loot[bestLootIndex].Position);
                return _runToDestination.Process(perception, id);
            }
        }

        private double CalculateLootValue(Perception perception, Loot loot)
        {
            
            double points = -loot.Position.SqrDistance(perception.MyUnints[id].Position); //Не корректно, лучше вообще работать без минуса
            switch (loot.Item)
            {
                case Item.Weapon weapon:
                    ///Можно также рассмотреть ситуацию когда нет патронов для нашего оружия
                    /// Но рядом есть патроны для другого
                    if ((perception.MyUnints[id].Weapon.HasValue) &&
                        (weapon.TypeIndex == perception.MyUnints[0].Weapon.Value) ||
                        (perception.MyUnints[id].Weapon.Value == 2))
                    {
                        points -= 10000000;
                    }
                    else
                    {
                        switch (weapon.TypeIndex)
                        {
                            case 0:
                                points += 0;
                                break;
                            case 1:
                                points += 0;
                                break;
                            case 2:
                                points += 200;
                                break;
                        }
                    }

                    break;
                case Item.Ammo ammo:
                    if (perception.MyUnints[id].Weapon.HasValue &&
                        ammo.WeaponTypeIndex == perception.MyUnints[id].Weapon.Value)
                    {
                        ///Надо написать формулу очков в зависимости от кол-ва патронов и других
                        /// факторов
                        points += 50;
                    }
                    else
                    {
                        points -= 10000000;
                    }

                    break;
                case Item.ShieldPotions potion:
                    points += 100;
                    break;
            }

            return points;
        }
    }
}