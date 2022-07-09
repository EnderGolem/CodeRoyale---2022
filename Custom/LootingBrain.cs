using System;
using AiCup22.Custom;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class LootingBrain : Brain
    {
        private RunToDestination _runToDestination;
        private PickupLoot _pickupLoot;
        private UseShield _useShield;
        private Loot desireLoot;
        private Vec2 desiredDestination;
        private double desiredPoints;

        public LootingBrain()
        {
            _runToDestination = new RunToDestination();
            _pickupLoot = new PickupLoot();
            _useShield = new UseShield();
        }

        public override UnitOrder Process(Perception perception)
        {
            if (perception.Game.Loot.Length == 0)   //Проверка, вдруг вообще ничего нет
                return new UnitOrder(new Vec2(), new Vec2(), null);

            int bestLootIndex = -1;
            double bestPoints = double.MinValue;

            for (int i = 0; i < perception.Game.Loot.Length; i++)
            {

                double curPoints = CalculateLootValue(perception, perception.Game.Loot[i]);
                
                System.Console.WriteLine(perception.Game.Loot[i].Item + " " + curPoints);
                if (bestPoints < curPoints)
                {
                    bestPoints = curPoints;
                    bestLootIndex = i;
                }
            }
            System.Console.WriteLine(perception.Game.Loot[bestLootIndex].Item + " " + bestPoints);
            System.Console.WriteLine("------------------------------");
            /* double shieldPoints = perception.MyUnints[id].Shield < 100 ? 300 : -1000; //Чтобы пил, нужна формула, ну или перенести мозгу, но хз..
             System.Console.WriteLine("ShieldPoints " + shieldPoints);
             System.Console.WriteLine("bestPoints " + bestPoints);
             if (shieldPoints > bestPoints && perception.MyUnints[id].Action == null)
             {
                 return _useShield.Process(perception, id);
             }
            */

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
        /*
        0 100 
        20 70 
        40 50 
        60 20 
        100 0
       */
        private double CalculateAmmoValue(Perception perception, Item.Ammo ammo)
        {

            double procentage = perception.MyUnints[id].Ammo[ammo.WeaponTypeIndex] / perception.Constants.Weapons[ammo.WeaponTypeIndex].MaxInventoryAmmo * 100;
            double points = 1;

            if (0 <= procentage && procentage < 20)
                points *= -1.5 * procentage + 100;
            else if (procentage < 40)
                points *= -procentage + 90;
            else if (procentage < 60)
                points *= -1.5 * procentage + 110;
            else
                points *= -0.5 * procentage + 50;

            return points;
        }
        /*
        0 80
        20 65
        40 55
        60 20
        100 0
        */
        private double CalculateShieldValue(Perception perception)
        {
            double procentage = perception.MyUnints[id].ShieldPotions / perception.Constants.MaxShieldPotionsInInventory * 100;
            double points = 1;
            if (0 <= procentage && procentage < 20)
                points *= -0.75 * procentage + 80;
            else if (procentage < 40)
                points *= -0.5 * procentage + 75;
            else if (procentage < 60)
                points *= -1.75 * procentage + 125;
            else
                points *= -0.5 * procentage + 50;
            return points;
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
                        points -= 1000;
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
                                points *= 200;
                                break;
                        }

                    }

                    break;
                case Item.Ammo ammo:
                    if (perception.MyUnints[id].Weapon.HasValue &&
                        ammo.WeaponTypeIndex == perception.MyUnints[id].Weapon.Value &&
                        //Проверка не максимум ли патронов, временный костыль
                        perception.MyUnints[id].Ammo[ammo.WeaponTypeIndex] < perception.Constants.Weapons[perception.MyUnints[0].Weapon.Value].MaxInventoryAmmo)
                    {
                        ///Надо написать формулу очков в зависимости от кол-ва патронов и других
                        /// факторов
                        points *= CalculateAmmoValue(perception, ammo);
                    }
                    else
                    {
                        points -= 10000000;
                    }

                    break;
                case Item.ShieldPotions potion:
                    points *= CalculateShieldValue(perception);
                    break;
            }

            return points;
        }
    }
}