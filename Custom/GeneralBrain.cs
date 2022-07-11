using System;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class GeneralBrain : Brain
    {
        protected const double healthValueBattle = Koefficient.healthValueBattle;
        protected const double shieldValueBattle = Koefficient.shieldValuefBattle;
        protected const double maxAmmoValue = Koefficient.maxAmmoValue;
        protected const double maxPotionsValueLoot = Koefficient.maxPotionsValueLoot;

        private LootingBrain _lootingBrain;
        private BattleBrain _battleBrain;
        private RadarBrain _radarBrain;
        private StaySafeBrain _staySafe;

        private double[] stateValues;

        public GeneralBrain()
        {
            _lootingBrain = new LootingBrain();
            _battleBrain = new BattleBrain();
            _radarBrain = new RadarBrain();
            _staySafe = new StaySafeBrain();
            allStates.Add(_lootingBrain);
            allStates.Add(_battleBrain);
            allStates.Add(_radarBrain);
            allStates.Add(_staySafe);
            
            stateValues=new double[allStates.Count];
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            if(Tools.CurrentZoneDistance(perception.Game.Zone,perception.MyUnints[0].Position)<=5)
            return _staySafe;
            
            /*double radarValue = CalculateRadarValue(perception, debugInterface);
            double battleValue = CalculateBattleValue(perception, debugInterface);

            double lootingValue = CalculateLootingValue(perception, debugInterface);*/

            stateValues[3] = CalculateStaySafeValue(perception, debugInterface);
            stateValues[2] = CalculateRadarValue(perception, debugInterface);
            stateValues[1] = CalculateBattleValue(perception, debugInterface);
            stateValues[0] = CalculateLootingValue(perception, debugInterface);
            Vec2 offset = new Vec2(-20,10);
            var textSize = 3;
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,0)),
                $"Radar: {stateValues[2]}",
                new Vec2(0.5,0.5), textSize,new Color(0,0,1,1));
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,2)),
                $"Battle: {stateValues[1]}",
                new Vec2(0.5,0.5), textSize,new Color(1,0,0,1));
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,4)),
                $"Looting {stateValues[0]}",
                new Vec2(0.5,0.5), textSize,new Color(0,1,0,1));
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,6)),
                $"StayAway {stateValues[3]}",
                new Vec2(0.5,0.5), textSize,new Color(1,1,0,1));

            int bestState = -1;
            double bestValue = double.MinValue;

            for (int i = 0; i < stateValues.Length; i++)

            {
                if (stateValues[i]>bestValue)
                {
                    bestState = i;
                    bestValue = stateValues[i];
                }
            }

            return allStates[bestState];
            //return _radarBrain;
            if ((!_radarBrain.IsActive && perception.Game.CurrentTick - _radarBrain.LastDeactivationTick > 100)
                || (_radarBrain.IsActive && perception.Game.CurrentTick - _radarBrain.LastActivationTick < 200))
            {
                return _radarBrain;
            }
            else if (perception.MyUnints[id].Weapon.HasValue && perception.MyUnints[0].Weapon.Value == 2 &&
                perception.EnemyUnints.Count > 0 && perception.MyUnints[id].Ammo[2] > 0)
            {
                return _battleBrain;
            }
            else
                return _lootingBrain;
        }

        protected virtual double CalculateRadarValue(Perception perception, DebugInterface debugInterface)
        {

            if (_radarBrain.IsActive)
            {

               // return 200 - 3*(perception.Game.CurrentTick - _radarBrain.LastActivationTick);
               if (perception.Game.CurrentTick - _radarBrain.LastActivationTick < 30) //Возможно формула
               {
                   return 170;
               }
               else
               {
                   return -10000;
               }

            }
            else
            {
                var result = (perception.Game.CurrentTick - _radarBrain.LastDeactivationTick + 50) / 5;
                return result < 180 ? result : 180;
            }
        }

        protected virtual double CalculateBattleValue(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = perception.MyUnints[0];
            double value = 0;
            if (perception.EnemyUnints.Count == 0)
            {
                return -100000;
            }
            if (unit.Ammo[unit.Weapon.Value] == 0) //ВРЕМЕННО, но к этому моменту стоит прислушаться
            {
                return -100000;
            }
            if (!unit.Weapon.HasValue)

            {
                value -= 10000;
            }
            else
            {
                value += (unit.Weapon.Value * 50) + maxAmmoValue *
                    unit.Ammo[unit.Weapon.Value] / perception.Constants.Weapons[unit.Weapon.Value].MaxInventoryAmmo;
            }

            value += healthValueBattle * unit.Health + shieldValueBattle * unit.Shield;

            value -= 150;

            return value;
        }

        protected virtual double CalculateLootingValue(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = perception.MyUnints[0];
            double value = 0;
            if (!unit.Weapon.HasValue) //???
            {
                value += 100;
            }
            else
            {

                double weaponValue = unit.Weapon.Value * 100;
                double ammoValue = maxAmmoValue *
                                   ((double)(unit.Ammo[unit.Weapon.Value]) / perception.Constants
                                       .Weapons[unit.Weapon.Value].MaxInventoryAmmo);
                double potionsValue = maxPotionsValueLoot * ((double)unit.ShieldPotions /
                                      perception.Constants.MaxShieldPotionsInInventory);
                value += (450 - weaponValue - ammoValue - potionsValue);
                Console.WriteLine($"Weapon value: {unit.Weapon.Value * 50} AmmoValue: {ammoValue}");
            }

            //value += healthValueKoefBattle * unit.Health + shieldValueKoefBattle * unit.Shield;
            return value;
        }

        protected virtual double CalculateStaySafeValue(Perception perception, DebugInterface debugInterface)
        {
            double zoneDistance = Tools.CurrentZoneDistance(perception.Game.Zone,perception.MyUnints[0].Position);
            if (zoneDistance < 0)
            {
                return 10000;
            }
            double zoneValue = 60-3*zoneDistance;
            zoneValue = Math.Max(0, zoneValue);
            double shieldValue = 200 - perception.MyUnints[0].Shield;
            double healthValue = 200 - 2*perception.MyUnints[0].Health;
            return zoneValue+shieldValue+healthValue;
        }

    }
}