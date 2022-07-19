using System;
using System.Collections.Generic;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    class BattleBrain : EndBrain
    {
        public const int safeZone = 15;
        protected const double focusDistance = 10;

        public BattleBrain(Perception perception) : base(perception)
        {
            AddState("LookAround", new LookAroundWithEvading(), perception);
            AddState("SteeringRun", new SteeringRunToDestinationWithEvading(), perception);
            AddState("Aim", new AimToDestinationDirection(), perception);
            AddState("SteeringAim", new SteeringAimToDestinationDirection(), perception);
            AddState("SteeringShoot", new SteeringShootToDestinationDirection(), perception);
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
                    var lookAround = (LookAroundWithEvading) GetAction(perception.MyUnints[i].Id,"LookAround");
                    orderedEndActions[perception.MyUnints[i].Id] = lookAround;
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

                var safeDir = CalculateDodge(perception, debugInterface, unit);
                if (unit.RemainingSpawnTime.HasValue)
                {
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
                else if ((currentStates[unit.Id] == steeringShoot || currentStates[unit.Id] == steeringAim) && unit.Position.Distance(focusUnit.Position)<focusDistance+4 ||
                    unit.Position.Distance(focusUnit.Position)<focusDistance)
                {
                    if (unit.Aim == 1 &&
                        !Tools.RaycastObstacleWithAllies(unit.Position, focusUnit.Position,
                            perception.CloseObstacles.ToArray(), perception.MyUnints, unit.Id, perception.Constants.UnitRadius,
                            true).HasValue)
                    {
                        steeringShoot.SetDestination(unit.Position.Add(safeDir));
                        steeringShoot.SetDirection(focusUnit.Position);
                        orderedEndActions[unit.Id] = steeringShoot;
                    }
                    else
                    {
                        steeringAim.SetDestination(unit.Position.Add(safeDir));
                        steeringAim.SetDirection(focusUnit.Position);
                        orderedEndActions[unit.Id] = steeringAim;
                    }
                }
                else
                {
                    evadingRun.SetDestination(focusUnit.Position);
                    orderedEndActions[unit.Id] = evadingRun;
                }
            }
            return orderedEndActions;
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
            return estimatedEnemyPosition.Add(estimatedEnemyPosition.Substract(enemy.Position).Multi(0.45));
        }

        Vec2 CalculateDodge(Perception perception, DebugInterface debugInterface, Unit unit)
        {
            if (perception.Game.Projectiles.Length == 0)
                return new Vec2(0, 0);
            int indexNearest = 0;
            for (int i = 0; i < perception.Game.Projectiles.Length; i++)
            {
                if (perception.Game.Projectiles[i].Id != perception.Game.MyId)
                    if (perception.Game.Projectiles[i].Position.SqrDistance(unit.Position) <
                        perception.Game.Projectiles[indexNearest].Position.SqrDistance(unit.Position))
                    {
                        indexNearest = i;
                    }
            }
            var bullet = perception.Game.Projectiles[indexNearest];
            var safeDirection1 = bullet.Position.FindPerpendicularWithX(unit.Position.X);
            var safeDirection2 = bullet.Position.FindPerpendicularWithX(unit.Position.X).Multi(-1);
            var lineBullet = new Straight(bullet.Velocity, bullet.Position);
            var lineDirection = new Straight(safeDirection1, unit.Position);
            var point = lineBullet.GetIntersection(lineDirection);
            //  System.Console.WriteLine($"SafeDirection1 {safeDirection1} SafeDirection{safeDirection2}");
            if (debugInterface != null)
                debugInterface.AddSegment(bullet.Position, bullet.Position.Add(bullet.Velocity), 0.1, new Color(0.7, 0.3, 0, 0.8));
            if (point.Value.SqrDistance(unit.Position.Add(safeDirection1)) > point.Value.SqrDistance(unit.Position.Add(safeDirection2)))
                return safeDirection1;
            else
                return safeDirection2;



        }
    }
}
