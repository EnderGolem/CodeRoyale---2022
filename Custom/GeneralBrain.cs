using System;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class GeneralBrain : Brain
    {
        protected const double healthValueKoefBattle = 1;
        protected const double shieldValueKoefBattle = 1;
        protected const double maxAmmoValueConst = 100;
        protected const double maxPotionsValueLoot = 100;
        
        private LootingBrain _lootingBrain;
        private BattleBrain _battleBrain;
        private RadarBrain _radarBrain;

        public GeneralBrain()
        {
            _lootingBrain = new LootingBrain();
            _battleBrain = new BattleBrain();
            _radarBrain = new RadarBrain();
            allStates.Add(_lootingBrain);
            allStates.Add(_battleBrain);
            allStates.Add(_radarBrain);
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            double radarValue = CalculateRadarValue(perception, debugInterface);
            double battleValue = CalculateBattleValue(perception, debugInterface);
            double lootingValue = CalculateLootingValue(perception, debugInterface);
            Vec2 offset = new Vec2(-20,10);
            var textSize = 2;
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,0)),
                $"Radar: {radarValue}",
                new Vec2(0.5,0.5), textSize,new Color(0,0,1,1));
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,2)),
                $"Battle: {battleValue}",
                new Vec2(0.5,0.5), textSize,new Color(1,0,0,1));
            debugInterface.AddPlacedText(debugInterface.GetState().Camera.Center.Add(offset).Add(new Vec2(0,4)),
                $"Looting {lootingValue}",
                new Vec2(0.5,0.5), textSize,new Color(0,1,0,1));

            if (radarValue > battleValue && radarValue > lootingValue)
            {
                return _radarBrain;
            }
            else if (battleValue>lootingValue)
            {
                return _battleBrain;
            }

            return _lootingBrain;
            //return _radarBrain;
            if ((!_radarBrain.IsActive && perception.Game.CurrentTick-_radarBrain.LastDeactivationTick>100)
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
                return  _lootingBrain;
        }

        protected virtual double CalculateRadarValue(Perception perception, DebugInterface debugInterface)
        {
            if (_radarBrain.IsActive)
            {
                return 200 - (perception.Game.CurrentTick - _radarBrain.LastActivationTick);
            }
            else
            {
                return perception.Game.CurrentTick - _radarBrain.LastDeactivationTick+50;
            }
        }
        
        protected virtual double CalculateBattleValue(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = perception.MyUnints[0];
            double value = 0;
            if (perception.EnemyUnints.Count==0)
            {
                return -100000;
            }

            if (!unit.Weapon.HasValue)
            {
                value -= 10000;
            }
            else
            {
                value += (unit.Weapon.Value * 50) + maxAmmoValueConst * unit.Ammo[unit.Weapon.Value]/perception.Constants.Weapons[unit.Weapon.Value].MaxInventoryAmmo;
            }

            value += healthValueKoefBattle * unit.Health + shieldValueKoefBattle * unit.Shield;
            return value;
        }
        
        protected virtual double CalculateLootingValue(Perception perception, DebugInterface debugInterface)
        {
            Unit unit = perception.MyUnints[0];
            double value = 0;
            if (!unit.Weapon.HasValue)
            {
                value += 100;
            }
            else
            {
                
                double weaponValue = unit.Weapon.Value * 100;
                double ammoValue = maxAmmoValueConst *
                                   ((double) (unit.Ammo[unit.Weapon.Value]) / perception.Constants
                                       .Weapons[unit.Weapon.Value].MaxInventoryAmmo); 
                Console.WriteLine($"Weapon value: {unit.Weapon.Value * 50} AmmoValue: {ammoValue}");
                double potionsValue = maxPotionsValueLoot * ((double)unit.ShieldPotions /
                                      perception.Constants.MaxShieldPotionsInInventory);
                value += (450 - weaponValue - ammoValue - potionsValue);
            }

            //value += healthValueKoefBattle * unit.Health + shieldValueKoefBattle * unit.Shield;
            return value;
        }

    }
}