using System;
using System.Collections.Generic;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;
using System.Linq;

namespace AiCup22.Custom
{
    class BattleBrain : EndBrain
    {
        public const int safeZone = 15;
        protected double[][] focusTable = new double[][] {new double[] { 12,12,12}, new double[] { 14,14,10}, new double[]{22,22,20}};

        private Dictionary<int,bool> isEvading ;
        private Dictionary<int,Vec2> evadingNullPosition;

        private Dictionary<int, Unit?> curTarget;

        public BattleBrain(Perception perception) : base(perception)
        {
            isEvading = new Dictionary<int, bool>();
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                isEvading[perception.MyUnints[i].Id] = false;
            }
            evadingNullPosition = new Dictionary<int, Vec2>();
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                evadingNullPosition[perception.MyUnints[i].Id] = new Vec2();
            }
            curTarget = new Dictionary<int, Unit?>();
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                curTarget[perception.MyUnints[i].Id] = null;
            }

            AddState("LookAround", new LookAroundWithEvading(), perception);
            AddState("SteeringRun", new SteeringRunToDestinationWithEvading(), perception);
            AddState("Aim", new AimToDestinationDirection(), perception);
            AddState("SteeringAim", new SteeringAimToDestinationDirection(), perception);
            AddState("SteeringShoot", new SteeringShootToDestinationDirection(), perception);
            AddState("UsePotion", new UseShieldToDestinationWithEvading(), perception);
            AddState("Evading", new Evading(), perception);
            AddState("Pickup",new PickupLoot(), perception);
        }

        /*protected override Dictionary<int, EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();
            for (int idInMyUnints = 0; idInMyUnints < perception.MyUnints.Count; idInMyUnints++)
            {
                var unit = perception.MyUnints[idInMyUnints];
                int unitId = unit.Id;

                var stShoot = (SteeringShootToDestinationDirection)GetAction(unitId, "SteeringShoot");
                var stAim = (SteeringAimToDestinationDirection)GetAction(unitId, "SteeringAim");

                if (perception.EnemyUnints.Count == 0) //Проверка, вдруг вообще ничего нет
                {
                    orderedEndActions[unitId] = GetAction(unitId, "LookAround");
                    continue;
                }

                double bestPoints = double.MinValue;
                int bestEnemyIndex = -1;
                double point = 0;
                for (int i = 0; i < perception.EnemyUnints.Count; i++)
                {
                    point = CalculateEnemyValue(perception, perception.EnemyUnints[i], unit);
                    if (debugInterface != null)
                        debugInterface.AddPlacedText(perception.EnemyUnints[i].Position, (point).ToString(), new Vec2(0, 0), 0.5, new Color(0, 1, 0.5, 0.7));
                    if (bestPoints < point)
                    {
                        bestEnemyIndex = i;
                        bestPoints = point;
                    }
                }


                var enemy = perception.EnemyUnints[bestEnemyIndex];
                var safeDirection = CalculateDodge(perception, debugInterface, unit);
                var distanceToEnemy = unit.Position.SqrDistance(perception.EnemyUnints[bestEnemyIndex].Position);
                var estimatedEnemyPosition = CalculateAimToTargetPrediction(ref enemy, perception.Constants.Weapons[unit.Weapon.Value].ProjectileSpeed, unit.Position);


                if (debugInterface != null)
                {

                    debugInterface.AddSegment(unit.Position, unit.Position.Add(unit.Direction.Multi(100)), 0.3, new Color(0, 1, 0, 0.5));
                    debugInterface.AddRing(unit.Position, safeZone, 0.5, new Color(0, 1, 0.5, 1));
                    debugInterface.AddRing(unit.Position, 30, 0.5, new Color(0, 1, 0.5, 1));
                    debugInterface.AddCircle(estimatedEnemyPosition, 0.4, new Color(1, 0, 0, 1));
                    debugInterface.AddPlacedText(enemy.Position.Add(new Vec2(0, 1)), enemy.Velocity.Length().ToString(), new Vec2(0.5, 0.5), 0.5, new Color(1, 0.4, 0.6, 0.5));
                    debugInterface.AddSegment(enemy.Position, estimatedEnemyPosition, 0.1, new Color(1, 0.4, 0.6, 0.5));
                }

                if (((currentStates[unitId] != stAim && currentStates[unitId] != stShoot) && distanceToEnemy > 30 * 30) ||
                    ((currentStates[unitId] == stAim || currentStates[unitId] == stShoot) && distanceToEnemy > 35 * 35)) //Приблежаемся, возможно нужно стрелять. Можно красивее через Active
                {
                    ((SteeringRunToDestinationWithEvading)GetAction(unitId, "SteeringRun")).SetDestination(perception.EnemyUnints[bestEnemyIndex].Position);
                    orderedEndActions[unitId] = GetAction(unitId, "SteeringRun");
                    continue;
                }
                else if (safeZone * safeZone < distanceToEnemy) //Стреляем
                {
                    int maxSafeIndex = perception.FindIndexMaxSafeDirection();
                    if (unit.Aim == 1 && Tools.RaycastObstacle(unit.Position, estimatedEnemyPosition,
                                                       perception.Constants.Obstacles, true) == null)
                    {
                        stShoot.SetDestination(unit.Position.Add(safeDirection));
                        stShoot.SetDirection(estimatedEnemyPosition);
                        orderedEndActions[unitId] = stShoot;
                        continue;
                    }


                    if (Tools.RaycastObstacle(unit.Position, estimatedEnemyPosition, perception.Constants.Obstacles, true) == null) //Если нет укрытия, просто прицеливаемся, уклоняясь
                        stAim.SetDestination(unit.Position.Add(safeDirection));
                    if (Tools.RaycastObstacle(unit.Position, estimatedEnemyPosition, perception.Constants.Obstacles, true) != null) //Если есть укрытие то
                    {
                        if (perception.EnemiesAimingYou[idInMyUnints].Contains(enemy.Id))
                            stAim.SetDestination(unit.Position.FindMirrorPoint(enemy.Position)); //Если Смотрит на нас, то отходим, отдалясь от укрытия
                        else                                                                                                                    //Если не смотрит, то приближаемся
                            stAim.SetDestination(enemy.Position);


                    }
                    stAim.SetDirection(estimatedEnemyPosition);
                    orderedEndActions[unitId] = stAim;
                    continue;
                }

                else  
                {
                    if (unit.Aim == 1 && Tools.RaycastObstacle(unit.Position, estimatedEnemyPosition,
                            perception.Constants.Obstacles, true) == null)
                    {
                        stShoot.SetDestination(unit.Position.FindMirrorPoint(enemy.Position));
                        stShoot.SetDirection(estimatedEnemyPosition);
                        orderedEndActions[unitId] = stShoot;
                        continue;
                    }
                    stShoot.SetDestination(unit.Position.FindMirrorPoint(enemy.Position));
                    stShoot.SetDirection(estimatedEnemyPosition);
                    orderedEndActions[unitId] = stAim;
                    continue;

                }
            }
            return orderedEndActions;
        }*/
        

        protected override Dictionary<int, EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();
            var fUnit = FindTastiestUnit(perception, debugInterface);
            if (!fUnit.HasValue)
            {
                for (int i = 0; i < perception.MyUnints.Count; i++)
                {
                    isEvading[perception.MyUnints[i].Id] = false;
                    if (perception.MyUnints[i].Shield < 160 && perception.MyUnints[i].ShieldPotions > 0)
                    {
                        var useShield = (UseShieldToDestinationWithEvading) GetAction(perception.MyUnints[i].Id, "UsePotion");
                        useShield.SetDestination(perception.Game.Zone.NextCenter);
                        orderedEndActions[perception.MyUnints[i].Id] = useShield;
                    }
                    else
                    {
                        var lookAround = (LookAroundWithEvading) GetAction(perception.MyUnints[i].Id,"LookAround");
                        orderedEndActions[perception.MyUnints[i].Id] = lookAround;
                    }
                }

                return orderedEndActions;
            }
            var focusUnit = fUnit.Value;
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                Unit unit = perception.MyUnints[i];
                var steeringShoot = (SteeringShootToDestinationDirection) GetAction(unit.Id, "SteeringShoot");
                var evadingRun = (SteeringRunToDestinationWithEvading) GetAction(unit.Id, "SteeringRun");
                var steeringAim = (SteeringAimToDestinationDirection) GetAction(unit.Id,"SteeringAim");
                var useShield = (UseShieldToDestinationWithEvading) GetAction(unit.Id, "UsePotion");
                var pickup = (PickupLoot) GetAction(unit.Id, "Pickup");
                /*var evading = (Evading) GetAction(unit.Id, "Evading");
                orderedEndActions[unit.Id] = evading;*/
                var targetUnit = TryToFindBetterTargetToUnit(unit, focusUnit, perception);
                curTarget[unit.Id] = targetUnit;
                var focusDistance = GetFocusDistance(unit.Weapon.Value, targetUnit.Weapon);
                var safeDir = CalculateDodge(perception, debugInterface, unit);
                debugInterface?.AddRing(targetUnit.Position,focusDistance,0.1,new Color(1,0,0,0.5));
                if (unit.RemainingSpawnTime.HasValue)
                {
                    isEvading[unit.Id] = false;
                    double minDist = 100000;
                    Loot bestLoot = new Loot();
                    foreach (var loot in perception.MemorizedLoot)
                    {
                        double dist = loot.Value.Position.SqrDistance(unit.Position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestLoot = loot.Value;
                        }
                    }
                    evadingRun.SetDestination(bestLoot.Position);
                    orderedEndActions[unit.Id] = evadingRun;
                }
                else if(NeedLooting(unit))
                {
                    if (!unit.Weapon.HasValue || unit.Weapon == 0)
                    {
                        double minDist = 1000000;
                        Loot bestLoot = new Loot();
                        foreach (var loot in perception.MemorizedLoot)
                        {
                            if (loot.Value.Item is Item.Weapon w)
                            {
                                if (w.TypeIndex != 0 && loot.Value.Position.SqrDistance(unit.Position)<minDist)
                                {
                                    minDist = loot.Value.Position.SqrDistance(unit.Position);
                                    bestLoot = loot.Value;
                                }
                            }
                        }

                        if (minDist != 1000000)
                        {
                            if (minDist > perception.Constants.UnitRadius / 2)
                            {
                                evadingRun.SetDestination(bestLoot.Position);
                                orderedEndActions[unit.Id] = evadingRun;
                            }
                            else
                            {
                                pickup.SetPickableLootId(bestLoot.Id);
                                orderedEndActions[unit.Id] = pickup;
                            }
                        }
                        else
                        {
                            evadingRun.SetDestination(perception.Game.Zone.CurrentCenter);
                            orderedEndActions[unit.Id] = evadingRun;
                        }
                    }
                    else
                    {
                        double minDist = 1000000;
                        Loot bestLoot = new Loot();
                        foreach (var loot in perception.MemorizedLoot)
                        {
                            if (loot.Value.Item is Item.Ammo a)
                            {
                                if (a.WeaponTypeIndex == unit.Weapon.Value && loot.Value.Position.SqrDistance(unit.Position)<minDist)
                                {
                                    minDist = loot.Value.Position.SqrDistance(unit.Position);
                                    bestLoot = loot.Value;
                                }
                            }
                        }

                        if (minDist != 1000000)
                        {
                            if (minDist > perception.Constants.UnitRadius / 2)
                            {
                                evadingRun.SetDestination(bestLoot.Position);
                                orderedEndActions[unit.Id] = evadingRun;
                            }
                            else
                            {
                                pickup.SetPickableLootId(bestLoot.Id);
                                orderedEndActions[unit.Id] = pickup;
                            }
                        }
                        else
                        {
                            evadingRun.SetDestination(perception.Game.Zone.CurrentCenter);
                            orderedEndActions[unit.Id] = evadingRun;
                        }
                    }
                }
                else if ((currentStates[unit.Id] == steeringShoot || currentStates[unit.Id] == steeringAim) && unit.Position.Distance(targetUnit.Position)<focusDistance+4 ||
                    unit.Position.Distance(targetUnit.Position)<focusDistance)
                {
                    isEvading[unit.Id] = false;
                    var estimatedEnemyPosition = CalculateAimToTargetPrediction(ref targetUnit, perception.Constants.Weapons[unit.Weapon.Value].ProjectileSpeed, unit.Position);
                    var raycast = Tools.RaycastObstacleWithAllies(unit.Position, estimatedEnemyPosition,
                        perception.CloseObstacles.ToArray(), perception.MyUnints, unit.Id,
                        perception.Constants.UnitRadius,
                        true);
                    if (raycast.HasValue)
                    {
                        if (unit.Shield <= 0 && unit.ShieldPotions > 0)
                        {
                            useShield.SetDestination(raycast.Value.Position.Add(raycast.Value.Position.Substract(targetUnit.Position).Normalize().Multi(raycast.Value.Radius+1.5)));
                            orderedEndActions[unit.Id] = useShield;
                        }
                        else
                        {
                            steeringAim.SetDestination(estimatedEnemyPosition);
                            steeringAim.SetDirection(estimatedEnemyPosition);
                            orderedEndActions[unit.Id] = steeringAim;
                        }
                    }
                    else
                    {
                        if (unit.Aim == 1 && estimatedEnemyPosition.Substract(unit.Position).AngleToVector(unit.Direction)<3)
                        {
                            steeringShoot.SetDestination(unit.Position.Add(safeDir));
                            steeringShoot.SetDirection(estimatedEnemyPosition);
                            orderedEndActions[unit.Id] = steeringShoot;
                        }
                        else
                        {
                            steeringAim.SetDestination(unit.Position.Add(safeDir));
                            steeringAim.SetDirection(estimatedEnemyPosition);
                            orderedEndActions[unit.Id] = steeringAim;
                        }
                    }
                }
                else
                {
                    Vec2 dest = new Vec2();
                    if (isEvading[unit.Id] && evadingNullPosition[unit.Id].SqrDistance(targetUnit.Position)<6*6)
                    {
                        dest = evadingNullPosition[unit.Id];
                    }
                    else
                    {
                        isEvading[unit.Id] = true;
                        dest = targetUnit.Position;
                        evadingNullPosition[unit.Id] = dest;
                    }
                    debugInterface?.AddCircle(dest,3,new Color(0,1,0,0.5));
                    if (unit.ShieldPotions > 0 & unit.Shield < 160)
                    {
                        useShield.SetDestination(dest);
                        orderedEndActions[unit.Id] = useShield;
                    }
                    else
                    {
                        evadingRun.SetDestination(dest);
                        orderedEndActions[unit.Id] = evadingRun;
                    }
                }
            }
            return orderedEndActions;
        }

        protected double GetFocusDistance(int yourWeapon, int? enemyWeapon)
        {
            int ew = 0;
            if (enemyWeapon.HasValue)
            {
                ew = enemyWeapon.Value;
            }
            return focusTable[yourWeapon][ew];
        }

        protected bool NeedLooting(Unit unit)
        {
            if (unit.Weapon.HasValue)
            {
                if (unit.Weapon.Value == 0 || unit.Ammo[unit.Weapon.Value] == 0)
                {
                    return true;
                }
                
                return false;
            }

            return true;
        }

        protected Unit? FindTastiestUnit(Perception perception, DebugInterface debugInterface)
        {
            double minDist = 10000000;
            Unit? bestEnemy = null;
            foreach (var enemy in perception.MemorizedEnemies)
            {
                if (enemy.Value.Item3.RemainingSpawnTime.HasValue || perception.Game.CurrentTick - enemy.Value.Item1>10)
                {
                    continue;
                }

                double maxDist = -1000000;
                for (int i = 0; i < perception.MyUnints.Count; i++)
                {
                    if(perception.MyUnints[i].RemainingSpawnTime.HasValue)
                        continue;
                    double dist = perception.MyUnints[i].Position.SqrDistance(enemy.Value.Item3.Position);

                    if (dist > maxDist)
                    {
                        maxDist = dist;
                    }
                }

                if (maxDist < minDist)
                {
                    minDist = maxDist;
                    bestEnemy = enemy.Value.Item3;
                }
            }

            return bestEnemy;
        }

        protected Unit TryToFindBetterTargetToUnit(Unit myUnit, Unit focusUnit,Perception perception)
        {
            if (curTarget[myUnit.Id].HasValue && perception.EnemyUnints.Count((e)=>e.Id==curTarget[myUnit.Id].Value.Id)>0 )
            {
                for (int i = 0; i < perception.EnemyUnints.Count; i++)
                {
                    if (perception.EnemyUnints[i].Id == curTarget[myUnit.Id].Value.Id && !perception.EnemyUnints[i].RemainingSpawnTime.HasValue &&(perception.EnemyUnints[i].Shield + perception.EnemyUnints[i].Health)<=300)
                    {
                        return perception.EnemyUnints[i];
                    }
                }
            }

            return focusUnit;
        }
        
        

        double CalculateEnemyValue(Perception perception, Unit enemy, Unit unit)
        {
            double points = 1 / enemy.Position.SqrDistance(unit.Position);
            points *= Tools.RaycastObstacle(unit.Position, (enemy.Position), perception.Constants.Obstacles, true) == null ? 2 : 1; //Под вопросом такое
            //Просчет по тому, насколько он близок к выходу из укрытия, как идея, ведь в финале это не нужно будет
            //Высчитывается ценность противника
            return points;
        }

        Vec2 CalculateAimToTargetPrediction(ref Unit enemy, double bulletSpeed, Vec2 shotPosition) //Возможно неправильно просчитывает
        {
            double estimatedFlyTime = enemy.Position.Distance(shotPosition) / bulletSpeed;

            Vec2 estimatedEnemyPosition = enemy.Position.Add(enemy.Velocity.Multi(estimatedFlyTime));


            for (int i = 0; i < 100; i++) //Тут был раньше 5!
            {
                estimatedFlyTime = estimatedEnemyPosition.Distance(shotPosition) / bulletSpeed;
                estimatedEnemyPosition = enemy.Position.Add(enemy.Velocity.Multi(estimatedFlyTime));
            }
            return estimatedEnemyPosition.Add(estimatedEnemyPosition.Substract(enemy.Position).Multi(0.35));
        }

        Vec2 CalculateDodge(Perception perception, DebugInterface debugInterface, Unit unit)
        {
            if (perception.MemorizedProjectiles.Count == 0)
                return new Vec2(0, 0);
            var indexNearest = perception.MemorizedProjectiles.First().Key;
            System.Console.WriteLine($"{indexNearest}");
            foreach(var g in perception.MemorizedProjectiles)
            {
                int i = g.Key;
                System.Console.WriteLine(g.Key);
                if (g.Value.projData.ShooterId != perception.Game.MyId)
                    if (g.Value.actualPosition.SqrDistance(unit.Position) <
                        perception.MemorizedProjectiles[indexNearest].actualPosition.SqrDistance(unit.Position))
                    {
                        indexNearest = i;
                    }
            }
            
            var bullet = perception.MemorizedProjectiles[indexNearest];
            var safeDirection1 = bullet.actualPosition.FindPerpendicularWithX(unit.Position.X);
            var safeDirection2 = bullet.actualPosition.FindPerpendicularWithX(unit.Position.X).Multi(-1);
            var lineBullet = new Straight(bullet.projData.Velocity, bullet.actualPosition);
            var lineDirection = new Straight(safeDirection1, unit.Position);
            var point = lineBullet.GetIntersection(lineDirection);
            //  System.Console.WriteLine($"SafeDirection1 {safeDirection1} SafeDirection{safeDirection2}");
            if (debugInterface != null)
                debugInterface.AddSegment(bullet.actualPosition, bullet.actualPosition.Add(bullet.projData.Velocity), 0.1, new Color(0.7, 0.3, 0, 0.8));
            if (point.Value.SqrDistance(unit.Position.Add(safeDirection1)) > point.Value.SqrDistance(unit.Position.Add(safeDirection2)))
                return safeDirection1;
            else
                return safeDirection2;



        }
    }
}
