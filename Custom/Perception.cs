using System;
using AiCup22.Custom;
using AiCup22.Model;
using Color = AiCup22.Debugging.Color;
using System.Drawing;
using AiCup22.Debugging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AiCup22.Custom
{
    public class Perception
    {
        private Game _game;
        private DebugInterface _debug;
        private Constants _constants;

        private List<Unit> _myUnints;
        private List<Unit> _enemyUnints;
        private Dictionary<int, List<int>> enemiesAimingYou;
        private List<Sound> sounds;  //Впилить проверку на то, кто именно слышит звук, чтобы не было повторов

        private const double obstacleSearchRadius = 80;
        private const int closeObstaclesRecalculationDelay = 10;
        private int lastObstacleRecalculationTick = -100;

        private Vec2 _averageUnitPosition;

        public Constants Constants => _constants;
        public Game Game => _game;
        public DebugInterface Debug => _debug;
        public List<Unit> MyUnints => _myUnints;
        public List<Unit> EnemyUnints => _enemyUnints;

        protected Vec2[] directions;
        /// <summary>
        /// Веса направлений
        /// </summary>
        protected List<double> directionDangers;
        public List<Obstacle> CloseObstacles => closeObstacles;
        public Vec2[] Directions => directions;
        public List<double> DirectionDangers => directionDangers;
        public Dictionary<int, Loot> MemorizedLoot => memorizedLoot;
        public Dictionary<int, MemorizedProjectile> MemorizedProjectiles => memorizedProjectiles;
        public Dictionary<int, List<int>> EnemiesAimingYou => enemiesAimingYou;
        public Dictionary<int, (int, double, Unit)> MemorizedEnemies => memorizedEnemies;

        public Vec2 AverageUnitPosition
        {
            get => _averageUnitPosition;
            private set => _averageUnitPosition = value;
        }

        private Dictionary<int, Loot> memorizedLoot;
        /// <summary>
        /// Массив запомненных пуль
        /// Ключ - ид пули
        /// </summary>
        private Dictionary<int, MemorizedProjectile> memorizedProjectiles;
        /// <summary>
        /// Список запомненных противников
        /// ключ - Id юнита
        /// значение - (последний тик в котором юнит был виден,рассчитанная опасность противника, сам юнит) 
        /// </summary>
        private Dictionary<int, (int, double, Unit)> memorizedEnemies;

        private List<Obstacle> closeObstacles;
        public Perception(Constants consts)
        {
            _constants = consts;
            memorizedLoot = new Dictionary<int, Loot>();
            memorizedEnemies = new Dictionary<int, (int, double, Unit)>();
            memorizedProjectiles = new Dictionary<int, MemorizedProjectile>();
            closeObstacles = new List<Obstacle>();
            enemiesAimingYou = new Dictionary<int, List<int>>();
            enemiesAimingYou[0] = new List<int>();
            enemiesAimingYou[1] = new List<int>();
            enemiesAimingYou[2] = new List<int>();
            directions = new Vec2[8];
            directions[0] = new Vec2(1, 0);
            directions[1] = new Vec2(0.5, 0.5);
            directions[2] = new Vec2(0, 1);
            directions[3] = new Vec2(-0.5, 0.5);
            directions[4] = new Vec2(-1, 0);
            directions[5] = new Vec2(-0.5, -0.5);
            directions[6] = new Vec2(0, -1);
            directions[7] = new Vec2(0.5, -0.5);
            directionDangers = new List<double>(directions.Length);
        }

        public void Analyze(Game game, DebugInterface debugInterface)
        {
            _game = game;
            _debug = debugInterface;

            _enemyUnints = new List<Unit>();
            _myUnints = new List<Unit>();            // Потому что, если находится в конструкторе, то каждый getorder, будет увеличиваться
            foreach (var unit in game.Units)
            {
                if (unit.PlayerId != game.MyId)
                {
                    _enemyUnints.Add(unit);
                    memorizedEnemies[unit.Id] = (game.CurrentTick, EstimateEnemyDanger(unit), unit);
                    continue;
                }

                MyUnints.Add(unit);
            }
            CalculateAverageUnitPosition();
            for (int i = 0; i < game.Loot.Length; i++)
            {
                memorizedLoot.TryAdd(game.Loot[i].Id, game.Loot[i]);
            }

            removeProjectiles();
            removeEnemies();

            for (int i = 0; i < game.Projectiles.Length; i++)
            {
                if (game.Projectiles[i].ShooterPlayerId != game.MyId)
                    if (!memorizedProjectiles.ContainsKey(game.Projectiles[i].Id))
                    {
                        memorizedProjectiles[game.Projectiles[i].Id] = new MemorizedProjectile(game.Projectiles[i], this);
                    }
                    else
                    {
                        var mem = memorizedProjectiles[game.Projectiles[i].Id];
                        mem.lastSeenTick = game.CurrentTick;
                        mem.projData = game.Projectiles[i];
                    }
                else   //УДАЛИТЬ!!!!!!!!!!!!!!!!!!!!!!! Хотя, пусть будет
                {
                    if (debugInterface != null)
                        debugInterface.AddSegment(game.Projectiles[i].Position, game.Projectiles[i].Position.Add(game.Projectiles[i].Velocity), 0.1, new Color(0.48, 0.48, 0.88, 0.5));
                }
            }
            CalculateEnemiesAimingYou(game, debugInterface);

            if (lastObstacleRecalculationTick + closeObstaclesRecalculationDelay <= game.CurrentTick)
            {
                CalculateCloseObstacles(_myUnints[0].Position, obstacleSearchRadius);
            }
            EstimateDirections(game, debugInterface);//Пока не нужно...
            if (debugInterface != null)
                DebugOutput(game, debugInterface);
        }

        private void removeEnemies()
        {
            List<int> memEnemiesToRemove = new List<int>();
            foreach (var enemy in memorizedEnemies)
            {
                if (Game.CurrentTick - enemy.Value.Item1 > Tools.TimeToTicks(6, Constants.TicksPerSecond))
                {
                    memEnemiesToRemove.Add(enemy.Key);
                }
            }
            for (int i = 0; i < memEnemiesToRemove.Count; i++) //Есть более быстрый способ удаления за O(1)
            {
                memorizedEnemies.Remove(memEnemiesToRemove[i]);
            }
        }
        private void removeLoot()
        {
            List<int> lootToRemove = new List<int>();
            foreach (var unit in MyUnints) //Удаление по видимости
            {
                foreach (var loot in MemorizedLoot)
                {
                    if ((Tools.BelongConeOfVision(loot.Value.Position, unit.Position,
                            unit.Direction, Constants.ViewDistance,
                            Constants.FieldOfView) &&
                              Game.Loot.Count(
                             (Loot l) => l.Id == loot.Key &&
                                       l.Position.X == loot.Value.Position.X
                                       && l.Position.Y == loot.Value.Position.Y) == 0))
                    {
                        lootToRemove.Add(loot.Key);

                    }
                }
            }
            foreach (var loot in MemorizedLoot)  //Удаление по расстоянию
            {
                var minDistance = _myUnints.Select((Unit unit) => unit.Position.SqrDistance(loot.Value.Position)).Min();
                if(minDistance > 2000)
                {
                    lootToRemove.Add(loot.Key);
                }
            }
            for (int i = 0; i < lootToRemove.Count; i++)
            {
                MemorizedLoot.Remove(lootToRemove[i]);
            }

        }
        private void removeProjectiles()
        {
            List<int> memProjToRemove = new List<int>();
            for (int idInMyUnints = 0; idInMyUnints < MyUnints.Count; idInMyUnints++)
            {
                foreach (var projectile in memorizedProjectiles)
                {
                    projectile.Value.CalculateActualPosition(this);
                    if ((Tools.BelongConeOfVision(projectile.Value.actualPosition, _myUnints[idInMyUnints].Position,
                             _myUnints[idInMyUnints].Direction, Constants.ViewDistance,
                             (1 - _myUnints[idInMyUnints].Aim) * Constants.FieldOfView) &&
                         Game.Projectiles.Count(p => p.Id == projectile.Value.projData.Id) == 0)
                        || projectile.Value.IsExpired(this, _game.CurrentTick)
                        || projectile.Value.actualPosition.Distance(_myUnints[idInMyUnints].Position) < Constants.UnitRadius)
                    {
                        memProjToRemove.Add(projectile.Key);
                    }
                }
            }

            for (int i = 0; i < memProjToRemove.Count; i++) //Есть более быстрый способ удаления за O(1)
            {
                memorizedProjectiles.Remove(memProjToRemove[i]);
            }
        }

        private void CalculateCloseObstacles(Vec2 startPosition, double distance)
        {
            closeObstacles.Clear();
            for (int i = 0; i < Constants.Obstacles.Length; i++)
            {
                if (_constants.Obstacles[i].Position.SqrDistance(startPosition) < distance * distance)
                {
                    closeObstacles.Add(_constants.Obstacles[i]);
                }
            }
        }

        public double EstimateEnemyDanger(Unit unit)
        {
            double weaponDanger = (unit.Weapon + 1) * 50 ?? 0;
            double ammoDanger = weaponDanger * unit.Ammo[unit.Weapon.Value] / _constants.Weapons[unit.Weapon.Value].MaxInventoryAmmo; //Тут точно балансить
            double healthDanger = unit.Health;
            double shieldDanger = unit.Shield;
            return (weaponDanger + ammoDanger + healthDanger + shieldDanger); // ХУЕЕЕТА
        }


        public double EstimateBulletDanger(Projectile bullet, int idInMyUnints)
        {
            //Рассчет сделать исходя из скорости, позиции, урона и еще можно добавить погрешность на жизнь(пуля может не долететь)
            double distance = 1 / MyUnints[idInMyUnints].Position.SqrDistance(bullet.Position);
            double damageDanger = 50;

            return distance * damageDanger * 1500; // ХУЕЕЕТА
        }

        protected void EstimateDirections(Game game, DebugInterface debugInterface, int idInMyUnints = 0)
        {
            directionDangers.Clear(); //Зачем тут Clear?
            for (int i = 0; i < directions.Length; i++)
            {
                directionDangers.Add(0);
            }

            foreach (var enemy in MemorizedEnemies)
            {
                if (game.CurrentTick - enemy.Value.Item1 > 200)
                {
                    continue;
                }

                for (int i = 0; i < directions.Length; i++)
                {
                    if (Tools.BelongDirection(enemy.Value.Item3.Position,
                       _myUnints[0].Position, directions[i].Multi(1), 180 / directions.Length))
                    {
                        var distance = MyUnints[0].Position.Distance(enemy.Value.Item3.Position);
                        double distanceDanger = 1;                //ИИ должен бояться, тех кто ближе больше, чем те кто дальше
                        if (distance > 30)
                            distanceDanger = -0.025 * distance + 1.75;
                        directionDangers[i] += enemy.Value.Item2 * distanceDanger;
                        if (enemiesAimingYou[idInMyUnints].Contains(enemy.Key))
                        {
                            ///Увеличиваем опасность для направления противоположному тому
                            /// Из которого по нам целят
                            directionDangers[(i + directions.Length / 2) % directions.Length] +=
                                enemy.Value.Item2 * distanceDanger * 0.8;
                        }

                        break;
                    }
                }
            }
            var vec = _myUnints[0].Position.Substract(game.Zone.CurrentCenter).Normalize().Multi(game.Zone.CurrentRadius * Koefficient.StayAwayFromCenter);
            var point = game.Zone.CurrentCenter.Add(vec);
            for (int i = 0; i < directions.Length; i++)
            {
                /*if (dirZoneDist < 0)
               {
                   directionDangers[i] += Math.Pow(30 - Tools.CurrentZoneDistance(game.Zone, _myUnints[0].Position), 2);
               }*/
                directionDangers[i] += point.Distance(_myUnints[0].Position.Add(directions[i].Normalize())) * 1;

            }

            double[] add = new double[directions.Length];
            for (int i = 0; i < directions.Length; i++)
            {
                int prev = (i == 0) ? 7 : i - 1;
                int next = (i + 1) % Directions.Length;
                add[prev] += directionDangers[i] * 0.5;
                add[next] += directionDangers[i] * 0.5;
            }

            for (int i = 0; i < add.Length; i++)
            {
                directionDangers[i] += add[i];
            }



        }
        public int FindIndexMaxSafeDirection() //Удалить, вроде бы это был мой костыль...
        {
            int maxSafeIndex = 0;
            for (int i = 0; i < DirectionDangers.Count; i++)
            {
                if (DirectionDangers[i] > DirectionDangers[maxSafeIndex])
                {

                    maxSafeIndex = i;
                }
            }
            return maxSafeIndex;

        }


        private void DebugOutput(Game game, DebugInterface debugInterface)
        {
            if (debugInterface != null)
            {
                Vec2 offset = new Vec2(-5, -20);
                DebugData.PlacedText text = new DebugData.PlacedText();
                text.Text = "Hello world!";
                Unit player = new Unit();
                foreach (var unit in game.Units)
                {
                    if (unit.Id == debugInterface.GetState().LockedUnit)
                    {
                        player = unit;
                    }
                }

                Vec2 debugTextPos = debugInterface.GetState().Camera.Center.Substract(offset);
                try
                {
                    if (debugInterface != null)
                        debugInterface.AddPlacedText(debugTextPos, $"Health: {player.Health} Ammo: {player.Ammo[player.Weapon.Value]}\n  Shield: {player.Shield}Potions: {player.ShieldPotions}\nVelocity: {player.Velocity}", new Vec2(0.5, 0.5), 1, new Color(0, 0, 1, 1));
                }
                catch (Exception)
                {

                }
                //    $"Health: {player.Health}",
                //    new Vec2(0.5, 0.5), textsize, textColor));
                //debugInterface.Add(new DebugData.PlacedText(debugTextPos.Substract(new Vec2(0, textsize / 2)),
                //    $"Shield: {player.Shield}",
                //    new Vec2(0.5, 0.5), textsize, textColor));
                //debugInterface.Add(new DebugData.PlacedText(debugTextPos.Substract(new Vec2(0, 2 * textsize / 2)),
                //    $"Potions: {player.ShieldPotions}",
                //    new Vec2(0.5, 0.5), textsize, textColor));
                //debugInterface.Add(new DebugData.PlacedText(debugTextPos.Substract(new Vec2(0, 3 * textsize / 2)),
                //    $"Velocity: {player.Velocity}",
                //    new Vec2(0.5, 0.5), textsize, textColor));


                debugInterface.AddRing(game.Zone.CurrentCenter, game.Zone.CurrentRadius, 1, new Color(1, 0, 0, 1));
                debugInterface.AddRing(game.Zone.NextCenter, game.Zone.NextRadius, 1, new Color(0, 1, 0, 1));
                foreach (var l in memorizedLoot)
                {
                    debugInterface.AddRing(l.Value.Position, 1, 0.3, new Color(0.8, 0.8, 0.8, 1));
                }

                foreach (var o in closeObstacles)
                {
                    debugInterface.AddRing(o.Position, 0.3, 0.3, new Color(0.8, 0.8, 0, 1));
                }

                foreach (var enemy in memorizedEnemies)
                {
                    debugInterface.AddRing(enemy.Value.Item3.Position, 0.3, 0.3, new Color(1, 0, 0, 1));
                    debugInterface.AddPlacedText(enemy.Value.Item3.Position,
                        enemy.Value.Item2.ToString(), new Vec2(0, 0), 2, new Color(1, 0, 0, 1));
                }

                foreach (var projectile in memorizedProjectiles)
                {
                    debugInterface.AddCircle(projectile.Value.actualPosition, 0.3, new Color(1, 0, 0, 1));
                    //Debug.AddPlacedText(projectile.Value.actualPosition,
                    //    projectile.Key.ToString(),
                    //    new Vec2(0, 0), 3, new Color(1, 0.2, 1, 0.7));
                }

                for (int i = 0; i < directions.Length; i++)
                {
                    Debug.AddPlacedText(_myUnints[0].Position.Add(directions[i].Multi(30)),
                        (Math.Ceiling(directionDangers[i])).ToString(),
                        new Vec2(0, 0), 2, new Color(1, 0.2, 1, 0.7));
                }

                for (int idInMyUnints = 0; idInMyUnints < MyUnints.Count; idInMyUnints++)
                {
                    foreach (var enemy in enemiesAimingYou[idInMyUnints])
                    {
                        debugInterface.AddSegment(memorizedEnemies[enemy].Item3.Position,
                            memorizedEnemies[enemy].Item3.Position.Add(memorizedEnemies[enemy].Item3.Direction.Multi(50)), 0.5, new Color(1, 0, 0, 0.5));
                    }
                }

            }
        }


        private void CalculateEnemiesAimingYou(Game game, DebugInterface debugInterface)
        {
            for (int idInMyUnints = 0; idInMyUnints < MyUnints.Count; idInMyUnints++)
            {
                enemiesAimingYou[idInMyUnints].Clear();
                foreach (var enemy in memorizedEnemies)
                {
                    if (enemy.Value.Item3.Weapon.HasValue && game.CurrentTick - enemy.Value.Item1 < 40 &&
                        Tools.BelongConeOfVision(_myUnints[idInMyUnints].Position, enemy.Value.Item3.Position,
                            enemy.Value.Item3.Direction, _constants.ViewDistance * 0.9,
                            _constants.Weapons[enemy.Value.Item3.Weapon.Value].AimFieldOfView * 0.5) &&
                        enemy.Value.Item3.Aim > 0.4)
                    {
                        enemiesAimingYou[idInMyUnints].Add(enemy.Key);
                    }
                }
            }
        }


        public Unit SimulateUnitMovement(Unit unit, UnitOrder order, int rotationDir, List<Obstacle> obstacles, List<MemorizedProjectile> projectiles,
            bool[] projectileMask, out List<int> destroyedProjectiles,
            int simulationStep, int startSimulationTick, DebugInterface debugInterface)
        {

            double simulationTime = Tools.TicksToTime(simulationStep, _constants.TicksPerSecond);
            double aimModifier = _constants.Weapons[unit.Weapon.Value].AimMovementSpeedModifier;
            double maxForwardSpeed = _constants.MaxUnitForwardSpeed * (1 - (1 - aimModifier) * unit.Aim);
            double maxBackwardSpeed = _constants.MaxUnitBackwardSpeed * (1 - (1 - aimModifier) * unit.Aim);

            double rotationAngle = rotationDir * (_constants.RotationSpeed -
                                   (_constants.RotationSpeed - _constants.Weapons[unit.Weapon.Value].AimRotationSpeed) *
                                   unit.Aim);
            rotationAngle *= simulationTime;
            unit.Direction = unit.Direction.Rotate(rotationAngle);

            double circleRadius = (maxForwardSpeed + maxBackwardSpeed) / 2;
            Vec2 circleCenterRelative = unit.Direction.Multi((maxForwardSpeed - maxBackwardSpeed) / 2);
            Vec2 limitedTargetVelocity = order.TargetVelocity.Substract(circleCenterRelative);
            if (limitedTargetVelocity.SqrDistance(new Vec2()) > circleRadius * circleRadius)
            {
                limitedTargetVelocity = limitedTargetVelocity.Normalize().Multi(circleRadius);
            }

            limitedTargetVelocity = limitedTargetVelocity.Add(circleCenterRelative);
            Vec2 diff = limitedTargetVelocity.Substract(unit.Velocity);
            Vec2 ndiff = diff.Normalize()
                .Multi(_constants.UnitAcceleration * (1 / _constants.TicksPerSecond) * simulationStep);
            if (ndiff.SqrDistance(new Vec2()) > diff.SqrDistance(new Vec2()))
            {
                ndiff = diff;
            }

            Vec2 simulationVelocity = unit.Velocity.Add(ndiff);
            /*debugInterface.AddCircle(unit.Position.Add(circleCenterRelative),0.5,new Color(1,1,1,1));
            debugInterface.AddRing(unit.Position.Add(circleCenterRelative),circleRadius,0.2,new Color(1,1,1,0.5));
            debugInterface.AddSegment(unit.Position,unit.Position.Add(order.TargetVelocity),0.5,new Color(1,0,0,0.5));
            debugInterface.AddSegment(unit.Position,unit.Position.Add(limitedTargetVelocity),0.5,new Color(0,1,0,0.5));
            debugInterface.AddSegment(unit.Position,unit.Position.Add(unit.Velocity),0.5,new Color(1,1,0,0.5));
            debugInterface.AddSegment(unit.Position,unit.Position.Add(simulationVelocity),0.5,new Color(0,0,1,0.5));*/
            Unit upUnit = unit;
            upUnit.Position = unit.Position.Add(simulationVelocity.Multi(simulationTime));
            upUnit.Velocity = simulationVelocity;
            destroyedProjectiles = new List<int>();
            double quareUnitRadius = 1.4 * _constants.UnitRadius * _constants.UnitRadius;
            for (int i = 0; i < projectiles.Count; i++)
            {
                if (projectileMask[i])
                {
                    continue;
                }

                MemorizedProjectile proj = projectiles[i];
                /*Vec2 newProjPosition = proj.projData.Position.Add(proj.projData.Velocity.Multi(
                    Tools.TicksToTime(startSimulationTick  - proj.lastSeenTick,
                        Constants.TicksPerSecond)));*/
                int index = (startSimulationTick - _game.CurrentTick) / simulationStep;
                Vec2 lastProjPosition = proj.estimatedPositions[index];
                Vec2 newProjPosition = proj.estimatedPositions[index + 1];
                if (newProjPosition.X == -10000)
                {
                    continue;
                }
                var obst = Tools.RaycastObstacle(lastProjPosition,
                    newProjPosition, new[] { new Obstacle(0, upUnit.Position, quareUnitRadius, true, false) }, false);
                if (/*newProjPosition.SqrDistance(upUnit.Position)<= quareUnitRadius*/obst.HasValue)
                {
                    double damage = _constants.Weapons[proj.projData.WeaponTypeIndex].ProjectileDamage;
                    try
                    {
                        double shieldDamage = Math.Clamp(damage, 0, unit.Shield);
                        upUnit.Shield -= shieldDamage;
                        upUnit.Health -= (damage - shieldDamage);
                        destroyedProjectiles.Add(i);
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return upUnit;
        }


        public Vec2 SimulateEvading(Unit unit, int rotationDir, List<Obstacle> obstacles, List<MemorizedProjectile> projectiles,
            int directionCount, Vec2 zeroDirection, int simulationStep, int simulationDepth, DebugInterface debugInterface)
        {
            Vec2[] directions = new Vec2[directionCount];
            double dirAngle = 360 / directionCount;
            Unit[] simulatedUnits = new Unit[directions.Length];
            double bestRes = -100;
            double bestScore = -100;
            int bestIndex = -1;
            List<Unit> bestList = new List<Unit>();
            for (int i = 0; i < directionCount; i++)
            {
                double angle = dirAngle * i;
                directions[i] = (zeroDirection).Rotate(angle);
            }

            for (int i = 0; i < projectiles.Count; i++)
            {
                projectiles[i].CalculateEstimatedPositions(this, simulationDepth, simulationStep);
            }
            bool[] projectileMask = new bool[projectiles.Count];
            for (int i = 0; i < directionCount; i++)
            {
                /*var timer = Stopwatch.StartNew();
                long nanosecPerTick = (1000L*1000L*1000L) / Stopwatch.Frequency;
                timer.Start();*/

                var (u, lst) = CascadeSimulation(unit,
                    new UnitOrder(directions[i].Multi(_constants.MaxUnitForwardSpeed), unit.Direction, null),
                    rotationDir,
                    obstacles, projectiles, projectileMask, directions, simulationStep, _game.CurrentTick,
                    0, simulationDepth, bestScore, debugInterface);
                simulatedUnits[i] = u;
                /*timer.Stop();
                if (timer.ElapsedMilliseconds > 1)
                {
                    Console.WriteLine($"Simulation took:{timer.ElapsedMilliseconds} ms");
                    Console.WriteLine($"Simulation took:{timer.ElapsedTicks * nanosecPerTick} ns");
                }*/
                if (simulatedUnits[i].Shield == unit.Shield && simulatedUnits[i].Health == unit.Health)
                {
                    return directions[i];
                }

                if (simulatedUnits[i].Health + simulatedUnits[i].Shield > bestRes)
                {
                    bestRes = simulatedUnits[i].Health + simulatedUnits[i].Shield;
                    bestIndex = i;
                    bestList = lst;
                    if (bestScore < bestRes)
                    {
                        bestScore = bestRes;
                    }
                }
            }

            if (debugInterface != null)
            {
                for (int i = 0; i < bestList.Count; i++)
                {
                    debugInterface.AddCircle(bestList[i].Position, 0.1,
                        new Color(1 - (bestList[i].Health / 100), (bestList[i].Health / 100) * 1, 0, 1));
                    debugInterface.AddArc(bestList[i].Position, 0.2, 0.1, 0, 360 * bestList[i].Shield / 200,
                        new Color(0, 0, 1, 1));
                }
            }

            return directions[bestIndex];
        }

        protected (Unit, List<Unit>) CascadeSimulation(Unit unit, UnitOrder order, int rotationDir, List<Obstacle> obstacles, List<MemorizedProjectile> projectiles,
            bool[] projectileMask,
            Vec2[] directions, int simulationStep, int curSimulationTick, int curSimulationDepth, int maxSimulationDepth, double bestScore, DebugInterface debugInterface)
        {
            List<int> catchedProj;


            Unit simUnit = SimulateUnitMovement(unit, order, rotationDir, obstacles, projectiles, projectileMask, out catchedProj, simulationStep, curSimulationTick, debugInterface);

            //debugInterface.AddCircle(simUnit.Position,0.2,new Color(1-(simUnit.Health/100),(simUnit.Health/100)*1,0,1));



            if (curSimulationDepth == maxSimulationDepth || simUnit.Health <= 0 || simUnit.Health + simUnit.Shield <= bestScore)
            {
                List<Unit> lst = new List<Unit>();
                lst.Add(simUnit);
                return (simUnit, lst);
            }

            for (int i = 0; i < catchedProj.Count; i++)
            {
                projectileMask[catchedProj[i]] = true;
            }
            Unit[] simulatedUnits = new Unit[directions.Length];
            double bs = bestScore;
            double bestRes = -100;
            int bestIndex = -1;
            List<Unit> bestList = new List<Unit>();
            for (int i = 0; i < directions.Length; i++)
            {

                var (u, lst) = CascadeSimulation(simUnit,
                    new UnitOrder(directions[i].Multi(_constants.MaxUnitForwardSpeed), simUnit.Direction, null),
                    rotationDir,
                    obstacles, projectiles, projectileMask, directions, simulationStep, curSimulationTick + simulationStep,
                    curSimulationDepth + 1, maxSimulationDepth, bs, debugInterface);

                simulatedUnits[i] = u;
                if (simulatedUnits[i].Shield == simUnit.Shield && simulatedUnits[i].Health == simUnit.Health)
                {
                    //debugInterface.AddRing(simUnit.Position,1,0.5,new Color(0,1,0,1));
                    lst.Add(simUnit);
                    for (int j = 0; j < catchedProj.Count; j++)
                    {
                        projectileMask[catchedProj[j]] = false;
                    }
                    return (simulatedUnits[i], lst);
                }

                if (simulatedUnits[i].Health + simulatedUnits[i].Shield > bestRes)
                {
                    bestRes = simulatedUnits[i].Health + simulatedUnits[i].Shield;
                    bestIndex = i;
                    bestList = lst;
                    if (bestRes > bs)
                    {
                        bs = bestRes;
                    }

                    //Console.WriteLine("Search best!");
                }
            }
            bestList.Add(simUnit);
            for (int i = 0; i < catchedProj.Count; i++)
            {
                projectileMask[catchedProj[i]] = false;
            }
            return (simulatedUnits[bestIndex], bestList);
        }

        public List<MemorizedProjectile> ClipSafeProjectiles(ref Unit unit)
        {
            List<MemorizedProjectile> res = new List<MemorizedProjectile>();

            foreach (var projectile in memorizedProjectiles)
            {
                if (projectile.Value.actualPosition.SqrDistance(unit.Position) < 800)

                {
                    res.Add(projectile.Value);
                }
            }

            return res;
        }

        protected void CalculateAverageUnitPosition()
        {
            Vec2 sum = new Vec2();
            for (int i = 0; i < _myUnints.Count; i++)
            {
                sum = sum.Add(_myUnints[i].Position);
            }

            AverageUnitPosition = sum.Multi((double) 1 / _myUnints.Count);
        }

    }

    public class MemorizedProjectile
    {
        public int lastSeenTick;
        public Vec2 estimatedDeathPosition;
        public Vec2 actualPosition;
        public Projectile projData;
        public Vec2[] estimatedPositions;

        public MemorizedProjectile(Projectile proj, Perception perception)
        {
            lastSeenTick = perception.Game.CurrentTick;
            actualPosition = proj.Position;
            projData = proj;
            CalculateProjectileDeathPosition(proj, perception.CloseObstacles);
        }

        public void CalculateProjectileDeathPosition(Projectile proj, List<Obstacle> closeObstacles)
        {
            estimatedDeathPosition = proj.Position.Add(proj.Velocity.Multi(proj.LifeTime));
            var ob = Tools.RaycastObstacle(proj.Position, estimatedDeathPosition, closeObstacles.ToArray(), true);
            if (ob.HasValue)
            {
                var s = new Straight(proj.Velocity, proj.Position);
                var perpS = new Straight();
                perpS.SetByNormalAndPoint(proj.Velocity, ob.Value.Position);
                estimatedDeathPosition = s.GetIntersection(perpS).Value;
            }
        }

        public void CalculateActualPosition(Perception perception)
        {
            actualPosition = projData.Position.Add(projData.Velocity.Multi(Tools.TicksToTime(perception.Game.CurrentTick - lastSeenTick, perception.Constants.TicksPerSecond)));
        }

        public bool IsExpired(Perception perception, int tick)
        {
            if (Tools.TimeToTicks(projData.Position.Distance(estimatedDeathPosition) / projData.Velocity.Length(), perception.Constants.TicksPerSecond) + lastSeenTick < tick)
            {
                return true;
            }

            return false;
        }

        public void CalculateEstimatedPositions(Perception perception, int simulationDepth, int simulationStep)
        {
            estimatedPositions = new Vec2[simulationDepth + 2];
            estimatedPositions[0] = actualPosition;

            Vec2 tickMovement = projData.Velocity.Multi(Tools.TicksToTime(simulationStep,
                perception.Constants.TicksPerSecond));
            for (int i = 1; i < estimatedPositions.Length; i++)
            {
                if (!IsExpired(perception, perception.Game.CurrentTick + i * simulationStep))
                {
                    estimatedPositions[i] = estimatedPositions[i - 1].Add(tickMovement);
                }
                else
                {
                    estimatedPositions[i] = new Vec2(-100000, 0);
                }
            }
        }
    }
}
