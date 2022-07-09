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
        private const double eps = 0.000001;
        private const int kShieldLoot = 120;
        private const int kAmmoLoot = 400;
        private const int kBowLoot = 2000;

        private RunToDestination _runToDestination;
        private PickupLoot _pickupLoot;
        private UseShield _useShield;
        private Loot desireLoot;
        private Vec2 desiredDestination;
        private double desiredPoints;

        public LootingBrain()
        {
            _runToDestination = new SteeringRunToDestination();
            _pickupLoot = new PickupLoot();
            _useShield = new UseShield();
        }

        public override UnitOrder Process(Perception perception,DebugInterface debugInterface)
        {
            if (perception.Game.Loot.Length == 0)   //Проверка, вдруг вообще ничего нет
                return new UnitOrder(new Vec2(), new Vec2(), null);

            int bestLootIndex = -1;
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
            /* double shieldPoints = perception.MyUnints[id].Shield < 100 ? 300 : -1000; //Чтобы пил, нужна формула, ну или перенести мозгу, но хз..
             System.Console.WriteLine("ShieldPoints " + shieldPoints);
             System.Console.WriteLine("bestPoints " + bestPoints);
             if (shieldPoints > bestPoints && perception.MyUnints[id].Action == null)
             {
                 return _useShield.Process(perception, id);
             }
            */

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
        private double CalculateAmmoValue(Perception perception, Item.Ammo ammo)
        {
            double procentage = perception.MyUnints[id].Ammo[ammo.WeaponTypeIndex] / perception.Constants.Weapons[ammo.WeaponTypeIndex].MaxInventoryAmmo * 100;
            if (procentage == 100)
                return 1;
            double points = procentage != 0 ? kAmmoLoot / procentage : 10000;
            return points;
        }
        private double CalculateShieldValue(Perception perception, Item.ShieldPotions potions)
        {
            double procentage = perception.MyUnints[id].ShieldPotions / perception.Constants.MaxShieldPotionsInInventory * 100;
            if (procentage == 100)
                return 1;
            double points = procentage != 0 ? (kShieldLoot * potions.Amount) / procentage : 10000;
            if ((double)perception.MyUnints[id].Shield > 1)
                points *= (-0.005 * (perception.Constants.MaxShield / (double)perception.MyUnints[id].Shield)) + 2;
            else
                points *= 2;

            return points;
        }
        private double CalculateZoneValue(Perception perception, Vec2 vec2)
        {
            var distance = Tools.CurrentZoneDistance(perception.Game.Zone, vec2);

            if (distance < 0)
                return -1;
            return 20 / (-distance - 4) + 5;
        }
        private double CalculateLootValue(Perception perception, Loot loot)
        {


            //   double points = -loot.Position.SqrDistance(perception.MyUnints[id].Position); //Не корректно, лучше вообще работать без минуса
            double points = 1 / loot.Position.SqrDistance(perception.MyUnints[id].Position);
            System.Console.WriteLine("Looting Brain Points Disance " + points);

            switch (loot.Item)
            {
                case Item.Weapon weapon:
                    ///Можно также рассмотреть ситуацию когда нет патронов для нашего оружия
                    /// Но рядом есть патроны для другого
                    if ((perception.MyUnints[id].Weapon.HasValue) &&
                        (weapon.TypeIndex == perception.MyUnints[id].Weapon.Value) ||
                        (perception.MyUnints[id].Weapon.Value == 2))
                    {
                        points = eps;
                    }
                    else
                    {
                        switch (weapon.TypeIndex)
                        {
                            case 0:
                                points *= 1;
                                break;
                            case 1:
                                points *= 1;
                                break;
                            case 2:
                                points *= kBowLoot;
                                break;
                        }

                    }

                    break;
                case Item.Ammo ammo:
                    if (perception.MyUnints[id].Weapon.HasValue &&
                        ammo.WeaponTypeIndex == 2) //ТОЛЬКО ПАТРОНЫ ДЛЯ ЛУКА
                    {
                        ///Надо написать формулу очков в зависимости от кол-ва патронов и других
                        /// факторов
                        points *= CalculateAmmoValue(perception, ammo);
                    }
                    else
                    {
                        points = eps;
                    }

                    break;
                case Item.ShieldPotions potion:
                    points *= CalculateShieldValue(perception, potion);
                    break;
            }
            points *= CalculateZoneValue(perception, loot.Position);
            return points;
        }
    }
}