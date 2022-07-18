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
        protected const double eps = Koefficient.eps;
        protected const int ShieldLoot = Koefficient.Looting.ShieldLoot;
        protected const int AmmoLoot = Koefficient.Looting.AmmoLoot;
        protected const int BowLoot = Koefficient.Looting.BowLoot;

        private RunToDestination _runToDestination;
        private PickupLoot _pickupLoot;
        private UseShieldToDestinationWithEvading _useShieldToDestinationWithEvading;
        private LookAroundWithEvading _lookAroundWithEvading;
        private Loot desireLoot;
        private Vec2 desiredDestination;
        private double desiredPoints;


        /* Скрыл, так как мне надоели предупреждения
           private Loot desireLoot;
           private Vec2 desiredDestination;
           private double desiredPoints;
        */
        public LootingBrain()
        {
            _runToDestination = new SteeringRunToDestinationWithEvading();
            _pickupLoot = new PickupLoot();
            _useShieldToDestinationWithEvading = new UseShieldToDestinationWithEvading();
            _lookAroundWithEvading = new LookAroundWithEvading();
            allStates.Add(_runToDestination);
            allStates.Add(_pickupLoot);
            allStates.Add(_useShieldToDestinationWithEvading);
            allStates.Add(_lookAroundWithEvading);
        }


        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            if (perception.Game.Loot.Length == 0)   //Проверка, вдруг вообще ничего нет
                return _lookAroundWithEvading;

            int bestLootIndex = -1;
            Loot bestLoot = new Loot();
            double bestPoints = double.MinValue;
            Unit unit = perception.MyUnints[id];
            List<int> lootToRemove = new List<int>();
            foreach (var loot in perception.MemorizedLoot)
            {
                if (loot.Value.Position.SqrDistance(unit.Position) > 2000 ||
                    (Tools.BelongConeOfVision(loot.Value.Position, unit.Position,
                        unit.Direction, perception.Constants.ViewDistance,
                        perception.Constants.FieldOfView) &&
                     perception.Game.Loot.Count(
                         (Loot l) => l.Id == loot.Key &&
                                   l.Position.X == loot.Value.Position.X
                                   && l.Position.Y == loot.Value.Position.Y) == 0))
                {
                    lootToRemove.Add(loot.Key);
                    continue;
                }

                double curPoints = CalculateLootValue(perception, loot.Value, debugInterface);

                if (debugInterface != null)
                {
                    debugInterface.AddPlacedText(loot.Value.Position, Math.Round(curPoints).ToString(), new Vec2(0, 0), 1, new Color(1, 0, 0.5, 1));
                }

                if (bestPoints < curPoints)
                {
                    bestPoints = curPoints;
                    bestLootIndex = loot.Key;
                    bestLoot = loot.Value;
                }
            }
            double shieldPoints = perception.MyUnints[id].Shield < 140 ? 1500 : 0; //Чтобы пил, нужна формула, ну или перенести мозгу, но хз..

            if (shieldPoints > bestPoints && perception.MyUnints[id].ShieldPotions > 0 && perception.MyUnints[id].Action == null)
            {
                _useShieldToDestinationWithEvading.SetDestination(perception.MyUnints[0].Position.Add(perception.Directions[perception.FindIndexMaxSafeDirection()]));
                // _useShieldToDestinationWithEvading.SetDirection(perception.MyUnints[0].Direction);
                return _useShieldToDestinationWithEvading;
            }


            for (int i = 0; i < lootToRemove.Count; i++)
            {
                perception.MemorizedLoot.Remove(lootToRemove[i]);
            }

            if (bestLoot.Position.Distance(perception.MyUnints[id].Position) <

                perception.Constants.UnitRadius / 2)
            {
                _pickupLoot.SetPickableLootId(bestLoot.Id);
                perception.MemorizedLoot.Remove(bestLoot.Id);
                return _pickupLoot;
            }
            else
            {
                if (debugInterface != null)
                {
                    debugInterface.AddRing(bestLoot.Position, 1, 0.5, new Color(0.5, 0.5, 0, 1));
                }
                _runToDestination.SetDestination(bestLoot.Position);

                return _runToDestination;
            }
        }

        private double CalculateAmmoValue(Perception perception, Item.Ammo ammo)
        {
            double procentage = perception.MyUnints[id].Ammo[ammo.WeaponTypeIndex] / perception.Constants.Weapons[ammo.WeaponTypeIndex].MaxInventoryAmmo * 100;
            if (procentage == 100)
                return 1;
            double points = procentage != 0 ? AmmoLoot / procentage : 10000;
            return points;
        }
        private double CalculateShieldValue(Perception perception, Item.ShieldPotions potions)
        {
            double procentage = perception.MyUnints[id].ShieldPotions / perception.Constants.MaxShieldPotionsInInventory * 100;
            if (procentage == 100)
                return 1;
            double points = procentage != 0 ? (ShieldLoot * potions.Amount) / procentage : 10000;
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
        private double CalculateLootValue(Perception perception, Loot loot, DebugInterface debugInterface = null)
        {

            double points = 1;
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
                                points *= BowLoot;
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
            if (debugInterface != null)
            {
                debugInterface.AddPlacedText(loot.Position.Add(new Vec2(1, -1)), Math.Round(points).ToString(), new Vec2(0, 0), 0.7, new Color(0.2, 0.72, 0.8, 0.7));
            }
            points *= 1 / loot.Position.SqrDistance(perception.MyUnints[id].Position);
            points *= CalculateZoneValue(perception, loot.Position);
            return points;
        }
    }
}