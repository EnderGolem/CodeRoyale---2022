﻿using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class LootingBrain : EndBrain
    {
        protected const double eps = Koefficient.eps;
        protected const int ShieldLoot = Koefficient.Looting.ShieldLoot;
        protected const int AmmoLoot = Koefficient.Looting.AmmoLoot;
        protected const int BowLoot = Koefficient.Looting.BowLoot;
        protected const int StaffLoot = Koefficient.Looting.StaffLoot;
        protected const int WandLoot = Koefficient.Looting.WandLoot;

        private List<Loot> occupiedLoot;

        /* Скрыл, так как мне надоели предупреждения
           private Loot desireLoot;
           private Vec2 desiredDestination;
           private double desiredPoints;
        */
        public LootingBrain(Perception perception) : base(perception)
        {
            AddState("Run", new SteeringRunToDestinationWithEvading(), perception);
            AddState("Pickup", new PickupLoot(), perception);
            AddState("UseShield", new UseShieldToDestinationWithEvading(), perception);
            AddState("LookAround", new LookAroundWithEvading(), perception);
        }


        protected override Dictionary<int, EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();

            occupiedLoot = new List<Loot>();
            for (int idInMyUnints = 0; idInMyUnints < perception.MyUnints.Count; idInMyUnints++)
            {
                var unit = perception.MyUnints[idInMyUnints];
                var run = (SteeringRunToDestinationWithEvading)GetAction(unit.Id, "Run");
                var pickUp = (PickupLoot)GetAction(unit.Id, "Pickup");
                var useShield = (UseShieldToDestinationWithEvading)GetAction(unit.Id, "UseShield");
                var lookAround = (LookAroundWithEvading)GetAction(unit.Id, "LookAround");

                if (perception.Game.Loot.Length == 0) //Проверка, вдруг вообще ничего нет
                {
                    orderedEndActions[unit.Id] = lookAround;
                    continue;
                }

                Loot bestLoot = new Loot();
                double bestPoints = double.MinValue;
                foreach (var loot in perception.MemorizedLoot)
                {
                    double curPoints = CalculateLootValue(perception, loot.Value, unit);

                    if (debugInterface != null)
                    {
                        debugInterface.AddPlacedText(loot.Value.Position.Add(new Vec2(0, idInMyUnints * 0.7)), Math.Round(curPoints).ToString(), new Vec2(0, 0), 0.7, new Color(1, 0, 0.5, 1));
                    }

                    if (bestPoints < curPoints)
                    {
                        bestPoints = curPoints;
                        bestLoot = loot.Value;
                    }
                }
                occupiedLoot.Add(bestLoot);
                double shieldPoints = unit.Shield < 140 ? 1500 : 0; //Чтобы пил, нужна формула, ну или перенести мозгу, но хз..

                if (shieldPoints > bestPoints && unit.ShieldPotions > 0 && unit.Action == null)
                {
                    useShield.SetDestination(bestLoot.Position);
                    orderedEndActions[unit.Id] = useShield;
                    continue;
                }




                if (bestLoot.Position.Distance(unit.Position) < perception.Constants.UnitRadius / 2)
                {
                    pickUp.SetPickableLootId(bestLoot.Id);
                    perception.MemorizedLoot.Remove(bestLoot.Id);
                    orderedEndActions[unit.Id] = pickUp;
                    continue;
                }
                else
                {
                    if (debugInterface != null)
                    {
                        debugInterface.AddRing(bestLoot.Position, 1, 0.5, new Color(0.5, 0.5, 0, 1));
                    }
                    run.SetDestination(bestLoot.Position);
                    orderedEndActions[unit.Id] = run;
                    continue;
                }
            }
            return orderedEndActions;
        }

        private double CalculateAmmoValue(Perception perception, Item.Ammo ammo, Unit unit)
        {
            double procentage = unit.Ammo[ammo.WeaponTypeIndex] / perception.Constants.Weapons[ammo.WeaponTypeIndex].MaxInventoryAmmo * 100;
            if (procentage == 100)
                return 0;

            double points = procentage != 0 ? AmmoLoot / procentage : 10000;
            if (ammo.WeaponTypeIndex == unit.Weapon.Value)
            {
                points *= 4;
            }
            return points;
        }
        private double CalculateShieldValue(Perception perception, Item.ShieldPotions potions, Unit unit)
        {
            double procentage = unit.ShieldPotions / perception.Constants.MaxShieldPotionsInInventory * 100;
            if (procentage == 100)
                return 0;
            double points = procentage != 0 ? (ShieldLoot * potions.Amount) / procentage : 10000;
            if ((double)unit.Shield > 1)
                points *= (-0.02 * (perception.Constants.MaxShield / (double)unit.Shield)) + 2;
            else
                points *= 2;

            return points;
        }
        private double CalculateZoneValue(Perception perception, Vec2 vec2)
        {
            var distance = Tools.CurrentZoneDistance(perception.Game.Zone, vec2);

            if (distance < 0)
                return 0.0;
            return 20 / (-distance - 4) + 5;
        }
        /// <summary>
        /// Коэффицент получаемый относительно их средней точки
        /// </summary>
        private double CalculateValueCentralPoint(Perception perception, Vec2 loot)
        {
            var distance = loot.Distance(perception.AverageUnitPosition);
            if (distance >= 30)
                return 0;
            return 30 / (distance - 40) + 3;
        }
        private double CalculateLootNearEnemy(Perception perception, Vec2 loot)
        {
            var points = 1.0;
            foreach (var enemy in perception.EnemyUnints)
            {
                var distance = loot.Distance(enemy.Position);
                if (distance < 5)
                    points = 0;
                else if (distance > 15)
                    points *= 1;
                else
                    points *= 0.1 * distance - 0.5;
            }
            return points;
        }
        private double CalculateLootValue(Perception perception, Loot loot, Unit unit, DebugInterface debugInterface = null)
        {
            if (occupiedLoot.Contains(loot))
            {
                return 0;
            }
            double points = 1;
            switch (loot.Item)
            {
                case Item.Weapon weapon:
                    ///Можно также рассмотреть ситуацию когда нет патронов для нашего оружия
                    /// Но рядом есть патроны для другого
                    if ((unit.Weapon.HasValue) &&
                        (weapon.TypeIndex == unit.Weapon.Value) ||
                        (unit.Weapon.Value == 2))  //Все еще приоритет луку
                    {
                        points = eps;
                    }
                    else
                    {
                        switch (weapon.TypeIndex)
                        {
                            case 0:
                                points *= WandLoot;
                                break;
                            case 1:
                                points *= StaffLoot;
                                break;
                            case 2:
                                points *= BowLoot;
                                break;
                        }

                    }

                    break;
                case Item.Ammo ammo:

                    ///Надо написать формулу очков в зависимости от кол-ва патронов и других
                    /// факторов
                    if (ammo.WeaponTypeIndex == 0)
                    {
                        points *= CalculateAmmoValue(perception, ammo, unit) * 0.05;
                    }
                    if (ammo.WeaponTypeIndex == 1)
                    {
                        points *= CalculateAmmoValue(perception, ammo, unit) * 0.25;
                    }
                    if (ammo.WeaponTypeIndex == 2)
                    {
                        points *= CalculateAmmoValue(perception, ammo, unit) * 1;
                    }

                    break;
                case Item.ShieldPotions potion:
                    points *= CalculateShieldValue(perception, potion, unit);
                    break;
            }
            if (debugInterface != null)
            {
                debugInterface.AddPlacedText(loot.Position.Add(new Vec2(1, -1)), Math.Round(points).ToString(), new Vec2(0, 0), 0.7, new Color(0.2, 0.72, 0.8, 0.7));
            }
            points *= 1 / loot.Position.SqrDistance(unit.Position);
            points *= CalculateZoneValue(perception, loot.Position);
            points *= CalculateValueCentralPoint(perception, loot.Position);
            points *= CalculateLootNearEnemy(perception, loot.Position);
            return points;
        }
    }
}