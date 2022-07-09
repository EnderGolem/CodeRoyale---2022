﻿using System;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22
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
            
            for (int i = 0; i < perception.Game.Loot.Length; i++)
            {

                double curPoints = CalculateLootValue(perception, perception.Game.Loot[i]);

                if (bestPoints < curPoints)
                {
                    bestPoints = curPoints;
                    bestLootIndex = i;
                }
            }

            //Console.WriteLine(perception.Game.Loot[bestLootIndex].Position.Distance(perception.MyUnints[0].Position));
            if (perception.Game.Loot[bestLootIndex].Position.Distance(perception.MyUnints[0].Position) <
                perception.Constants.UnitRadius/2)
            {
                Console.WriteLine("Start pickup!!!");
                _pickupLoot.SetPickableLootId(perception.Game.Loot[bestLootIndex].Id);
                return _pickupLoot.Process(perception, 0);
            }
            else
            {
                debugInterface.AddRing(perception.Game.Loot[bestLootIndex].Position,1,0.5,new Color(0.5,0.5,0,1));
                _runToDestination.SetDestination(perception.Game.Loot[bestLootIndex].Position);
                return _runToDestination.Process(perception, 0);
            }
        }

        private double CalculateLootValue(Perception perception, Loot loot)
        {
            double points = -loot.Position.SqrDistance(perception.MyUnints[0].Position);
            switch (loot.Item)
            {
                case Item.Weapon weapon:
                    ///Можно также рассмотреть ситуацию когда нет патронов для нашего оружия
                    /// Но рядом есть патроны для другого
                    if ((perception.MyUnints[0].Weapon.HasValue) &&
                        (weapon.TypeIndex == perception.MyUnints[0].Weapon.Value) ||
                        (perception.MyUnints[0].Weapon.Value == 2))
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
                    if (perception.MyUnints[0].Weapon.HasValue &&
                        ammo.WeaponTypeIndex == perception.MyUnints[0].Weapon.Value)
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