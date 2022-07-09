using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Custom;
using AiCup22.Debugging;
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
            _runToDestination = new SteeringRunToDestination();
            _pickupLoot = new PickupLoot();
        }

        public override UnitOrder Process(Perception perception,DebugInterface debugInterface)
        {
            int bestLootIndex = -1;
            /*Console.WriteLine("GHFIHJGJHKBVKJNSKLDJVBKSDBVKLBSDKLV11111!!!!!!!!!!");
            Console.WriteLine(perception.Game);
            Console.WriteLine(perception.Game.Loot);
            Console.WriteLine(perception.Game.Loot[0]);*/
            double bestPoints = double.MinValue;
            Unit unit = perception.MyUnints[id];
            List<int> lootToRemove = new List<int>();
            //Console.WriteLine(Tools.IsInside(-10,110,-130));
            foreach (var loot in perception.MemorizedLoot)
            {
                if (loot.Value.Position.SqrDistance(unit.Position) > 2000 ||
                    (Tools.BelongConeOfVision(loot.Value.Position,unit.Position,
                        unit.Direction,perception.Constants.ViewDistance,
                        perception.Constants.FieldOfView) &&
                     perception.Game.Loot.Count(
                         (Loot l)=>l.Id==loot.Key && 
                                   l.Position.X == loot.Value.Position.X 
                                   && l.Position.Y == loot.Value.Position.Y)==0))
                {
                    lootToRemove.Add(loot.Key);
                    continue;
                }

                double curPoints = CalculateLootValue(perception, loot.Value);

                if (bestPoints < curPoints)
                {
                    bestPoints = curPoints;
                    bestLootIndex = loot.Key;
                }
            }

            for (int i = 0; i < lootToRemove.Count; i++)
            {
                perception.MemorizedLoot.Remove(lootToRemove[i]);
            }

           // Console.WriteLine(perception.Game.Loot[bestLootIndex].Position.Distance(perception.MyUnints[id].Position));
            if (perception.MemorizedLoot[bestLootIndex].Position.Distance(perception.MyUnints[id].Position) <
                perception.Constants.UnitRadius / 2)
            {
                //Console.WriteLine("Start pickup!!!");
                _pickupLoot.SetPickableLootId(perception.MemorizedLoot[bestLootIndex].Id);
                perception.MemorizedLoot.Remove(perception.MemorizedLoot[bestLootIndex].Id);
                return _pickupLoot.Process(perception, id);
            }
            else
            {
                debugInterface.AddRing(perception.MemorizedLoot[bestLootIndex].Position,1,0.5,new Color(0.5,0.5,0,1));
                _runToDestination.SetDestination(perception.MemorizedLoot[bestLootIndex].Position);
                return _runToDestination.Process(perception, debugInterface,id);
            }
        }

        private double CalculateLootValue(Perception perception, Loot loot)
        {
            double points = -loot.Position.SqrDistance(perception.MyUnints[id].Position);
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