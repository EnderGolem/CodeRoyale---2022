﻿using System;
using System.Linq;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class GeneralBrain : SuperBrain
    {
        protected const double healthValueBattle = Koefficient.healthValueBattle;
        protected const double shieldValueBattle = Koefficient.shieldValuefBattle;
        protected const double maxAmmoValue = Koefficient.maxAmmoValue;
        protected const double maxPotionsValueLoot = Koefficient.maxPotionsValueLoot;
        protected const double bowValue = Koefficient.bowValue;
        protected const double WeaponValueBattle = Koefficient.WeaponValueBattle;
        protected const double PunishmentForLeavingBattle = Koefficient.PunishmentForLeavingBattle;


        private LootingBrain _lootingBrain;
        private BattleBrain _battleBrain;
        private RadarBrain _radarBrain;
        private StaySafeBrain _staySafe; //Для теста
        private WanderingBrain _wanderingBrain;


        private double[] stateValues;

        public GeneralBrain(Perception perception) : base(perception)
        {
            _lootingBrain = new LootingBrain(perception);
            _battleBrain = new BattleBrain(perception);
            _radarBrain = new RadarBrain(perception);
            _staySafe = new StaySafeBrain(perception);
            _wanderingBrain = new WanderingBrain(perception);
            allStates.Add(_lootingBrain);
            allStates.Add(_battleBrain);
            allStates.Add(_radarBrain);
            //allStates.Add(_staySafe);
            allStates.Add(_wanderingBrain);

            stateValues = new double[allStates.Count];
            timeStates = new long[allStates.Count];
        }

        protected override Brain ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            if (debugInterface != null)
            {
                if (currentState == _lootingBrain)
                {
                    timeStates[0] += 1;
                }

                if (currentState == _battleBrain)
                {
                    timeStates[1] += 1;
                }

                if (currentState == _radarBrain)
                {
                    timeStates[2] += 1;
                }

                if (currentState == _staySafe)
                {
                    timeStates[3] += 1;
                }
            }

            stateValues[4] = CalculateWanderingBrain(perception, debugInterface);
            stateValues[3] = CalculateStaySafeValue(perception, debugInterface);
            stateValues[2] = CalculateRadarValue(perception, debugInterface);
            stateValues[1] = CalculateBattleValue(perception, debugInterface);
            stateValues[0] = CalculateLootingValue(perception, debugInterface);

            int bestState = -1;
            double bestValue = double.MinValue;

            for (int i = 0; i < stateValues.Length; i++)

            {
                if (stateValues[i] > bestValue)
                {
                    bestState = i;
                    bestValue = stateValues[i];
                }
            }

            if (debugInterface != null)
            {
                Vec2 offset = new Vec2(-30, 20);
                var textSize = 2.5;
                var center = perception.MyUnints[0].Position.Add(offset);
                debugInterface.AddPlacedText(center.Add(new Vec2(0, 0)),
                    $"Radar: {stateValues[2]}",
                    new Vec2(0.5, 0.5), textSize, new Color(0, 0, 1, 1));
                debugInterface.AddPlacedText(center.Add(new Vec2(0, 2)),
                    $"Battle: {stateValues[1]}",
                    new Vec2(0.5, 0.5), textSize, new Color(1, 0, 0, 1));
                debugInterface.AddPlacedText(center.Add(new Vec2(0, 4)),
                    $"Looting {stateValues[0]}",
                    new Vec2(0.5, 0.5), textSize, new Color(0, 1, 0, 1));
                debugInterface.AddPlacedText(center.Add(new Vec2(0, 6)),
                    $"StayAway {stateValues[3]}",
                    new Vec2(0.5, 0.5), textSize, new Color(1, 1, 0, 1));
                debugInterface.AddPlacedText(center.Add(new Vec2(0, 8)),
                    $"Wandering {stateValues[4]}",
                    new Vec2(0.5, 0.5), textSize, new Color(1, 1, 0, 1));
                debugInterface.AddPlacedText(center.Add(new Vec2(0, -1.5)),
                    $"CurrentStay {allStates[bestState].GetType().Name}",
                    new Vec2(0.5, 0.5), textSize, new Color(1, 0, 1, 1));
            }

            return allStates[bestState];
        }

        protected virtual double CalculateRadarValue(Perception perception, DebugInterface debugInterface)
        {
            return -100000;
            var soundBullet = false;
            foreach (var sound in perception.Game.Sounds)
            {
                if (sound.Position.Distance(perception.MyUnints[0].Position) < 1.5)
                {
                    if (!Tools.BelongConeOfVision(sound.Position, perception.MyUnints[0].Position,
                        perception.MyUnints[0].Direction, perception.Constants.ViewDistance,
                        perception.Constants.FieldOfView)) //Идет проверка по зрению без учета прицеливания
                    {
                        soundBullet = true;
                    }
                }

                if (debugInterface != null)
                    debugInterface.AddCircle(sound.Position, 0.5, new Color(1, 1, 0, 1));
            }

            if (_radarBrain.IsActive)
            {
                if (perception.Game.CurrentTick - _radarBrain.LastActivationTick < 30)
                {
                    return 4100;
                }
                else
                {
                    return -10000;
                }
            }
            else
            {
                var result = (perception.Game.CurrentTick - _radarBrain.LastDeactivationTick) * 10;
                if ((perception.Game.CurrentTick - _radarBrain.LastDeactivationTick) < 120 && soundBullet)
                    return 0;
                if (soundBullet)
                {
                    result *= 10;
                }

                return result-10000;
            }
        }

        protected virtual double CalculateBattleValue(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = perception.MyUnints[0];

            bool hasEnemy = false;
            foreach (var enemy in perception.MemorizedEnemies)
            {
                if (perception.Game.CurrentTick - enemy.Value.Item1 <
                    Tools.TimeToTicks(1, perception.Constants.TicksPerSecond))
                {
                    hasEnemy = true;
                }
            }
            
            if (!hasEnemy && perception.EnemyUnints.Count == 0)
            {
                return -100000;
            }


            return 10000;
        }

        protected virtual double CalculateLootingValue(Perception perception, DebugInterface debugInterface)
        {
            if (perception.MemorizedLoot.Count == 0)
                return -10000;
            Unit unit = perception.MyUnints[0];
            double value = 0;

            if (perception.MyUnints.Count((Unit u) => u.ShieldPotions == 10) != 2)
            {
                return 3750;
            }
            if (perception.MyUnints.Count((Unit u) => u.Ammo[u.Weapon.Value] == perception.Constants.Weapons[u.Weapon.Value].MaxInventoryAmmo) != 2)
            {
                return 4000;
            }
            //Добавить еще проверку на качество оружия, но позже

            return value < 9999 ? value : 9999;
        }

        protected virtual double CalculateStaySafeValue(Perception perception, DebugInterface debugInterface)
        {
            double zoneDistance = Tools.CurrentZoneDistance(perception.Game.Zone, perception.MyUnints[0].Position);
            if (zoneDistance < 0)
            {
                return 10000;
            }

            double enemiesValue = 0;
            int i = 0;
            int sum = 0;
            double totalDanger = 0;
            if (perception.EnemiesAimingYou.Count > 0)
            {

                foreach (var enemy in perception.EnemiesAimingYou[0])  //Должно по другому быть, но я исправил, чтобы не было ошибок и не пришлось большой кусок кода коммитить
                {
                    i++;
                    sum += i;
                    totalDanger += perception.MemorizedEnemies[enemy].Item2;
                }

                enemiesValue = sum * (totalDanger) / i;
            }

            double zoneValue = Koefficient.StayAwayZoneMaxValue *
                               (1 - zoneDistance / perception.Game.Zone.CurrentRadius);
            zoneValue = Math.Max(0, zoneValue);
            double healthValue = Koefficient.StayAwayMaxHealthValue - Koefficient.StayAwayMaxHealthValue *
                (perception.MyUnints[0].Health + perception.MyUnints[0].Shield)
                / (perception.Constants.MaxShield + perception.Constants.UnitHealth);
            healthValue = healthValue * sum;
            double value = zoneValue + healthValue + enemiesValue + Koefficient.StayAwayBaseValue;
            if (_staySafe.IsActive)
            {
                value += Koefficient.PunishmentForLeavingStaySafe;
            }

            return value-100000;
        }
        protected virtual double CalculateWanderingBrain(Perception perception, DebugInterface debugInterface)
        {
            return 3000;
        }
    }
}

