using System;
using AiCup22.Custom;
using AiCup22.Model;
using Color = AiCup22.Debugging.Color;
using System.Drawing;
using AiCup22.Debugging;
using System.Collections.Generic;
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
        private List<int> enemiesAimingYou;

        private const double obstacleSearchRadius = 80;
        private const int closeObstaclesRecalculationDelay = 10;
        private int lastObstacleRecalculationTick = -100;

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

        public List<int> EnemiesAimingYou => enemiesAimingYou;
        public Dictionary<int, (int, double, Unit)> MemorizedEnemies => memorizedEnemies;

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
            enemiesAimingYou = new List<int>();
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
            _myUnints = new List<Unit>(); // Потому что, если находится в конструкторе, то каждый getorder, будет увеличиваться
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

            for (int i = 0; i < game.Loot.Length; i++)
            {
                memorizedLoot.TryAdd(game.Loot[i].Id, game.Loot[i]);
            }

            List<int> memProjToRemove = new List<int>();

            foreach (var projectile in memorizedProjectiles)
            {
                projectile.Value.CalculateActualPosition(this);
                if ((Tools.BelongConeOfVision(projectile.Value.actualPosition, _myUnints[0].Position,
                         _myUnints[0].Direction, Constants.ViewDistance,
                         (1 - _myUnints[0].Aim) * Constants.FieldOfView) &&
                     Game.Projectiles.Count(p => p.Id == projectile.Value.projData.Id) == 0)
                    || projectile.Value.IsExpired(this)
                    || projectile.Value.actualPosition.Distance(MyUnints[0].Position) < Constants.UnitRadius)
                {
                    memProjToRemove.Add(projectile.Key);
                }
            }

            for (int i = 0; i < memProjToRemove.Count; i++)
            {
                memorizedProjectiles.Remove(memProjToRemove[i]);
            }

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
            EstimateDirections(game, debugInterface);
            if (debugInterface != null)
                DebugOutput(game, debugInterface);
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
            return weaponDanger + ammoDanger + healthDanger + shieldDanger; // ХУЕЕЕТА
        }


        public double EstimateBulletDanger(Projectile bullet)
        {
            //Рассчет сделать исходя из скорости, позиции, урона и еще можно добавить погрешность на жизнь(пуля может не долететь)
            double distance = 1 / MyUnints[0].Position.SqrDistance(bullet.Position);
            double damageDanger = 50;

            return distance * damageDanger * 1500; // ХУЕЕЕТА
        }

        protected void EstimateDirections(Game game, DebugInterface debugInterface)
        {
            directionDangers.Clear();
            for (int i = 0; i < directions.Length; i++)
            {
                directionDangers.Add(0);
            }

            foreach (var enemy in MemorizedEnemies) //Можно вроде бы просчитывать быстрее и более корректней.
            {
                if (game.CurrentTick - enemy.Value.Item1 > 200)
                {
                    continue;
                }

                for (int i = 0; i < directions.Length; i++)
                {
                    if (Tools.BelongDirection(enemy.Value.Item3.Position,
                       _myUnints[0].Position, directions[i].Multi(1), 180 / directions.Length))
                    //_myUnints[0].Position, directions[i], 180/directions.Length))
                    {
                        directionDangers[i] += enemy.Value.Item2;
                        break;
                    }
                }
            }
            for (int i = 0; i < directions.Length; i++)
            {
                double dirZoneDist = Tools.CurrentZoneDistance(game.Zone,
                    _myUnints[0].Position.Add(directions[i].Normalize().Multi(30)));
                /*if (dirZoneDist < 0)
               {
                   directionDangers[i] += Math.Pow(30 - Tools.CurrentZoneDistance(game.Zone, _myUnints[0].Position), 2);
               }*/
                directionDangers[i] +=
                        game.Zone.CurrentCenter.Distance(_myUnints[0].Position.Add(directions[i].Normalize()));

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
        public int FindIndexMaxSafeDirection()
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
                //Console.WriteLine(memorizedLoot.Count);
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
                    Console.WriteLine($"{i}. {directionDangers[i]}");
                    Debug.AddPlacedText(_myUnints[0].Position.Add(directions[i].Multi(30)),
                        (Math.Ceiling(directionDangers[i])).ToString(),
                        new Vec2(0, 0), 2, new Color(1, 0.2, 1, 0.7));
                }

                foreach (var enemy in enemiesAimingYou)
                {
                    debugInterface.AddSegment(memorizedEnemies[enemy].Item3.Position,
                        memorizedEnemies[enemy].Item3.Position.Add(memorizedEnemies[enemy].Item3.Direction.Multi(50)), 0.5, new Color(1, 0, 0, 0.5));
                }
            }
        }


        private void CalculateEnemiesAimingYou(Game game, DebugInterface debugInterface)
        {
            enemiesAimingYou.Clear();
            foreach (var enemy in memorizedEnemies)
            {
                if (enemy.Value.Item3.Weapon.HasValue && game.CurrentTick - enemy.Value.Item1 < 40 &&
                    Tools.BelongConeOfVision(_myUnints[0].Position, enemy.Value.Item3.Position,
                        enemy.Value.Item3.Direction, _constants.ViewDistance * 0.9,
                        _constants.Weapons[enemy.Value.Item3.Weapon.Value].AimFieldOfView * 0.5) &&
                    enemy.Value.Item3.Aim > 0.4)
                {
                    enemiesAimingYou.Add(enemy.Key);
                }
            }
        }


    }

    public class MemorizedProjectile
    {
        public int lastSeenTick;
        public Vec2 estimatedDeathPosition;
        public Vec2 actualPosition;
        public Projectile projData;

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

        public bool IsExpired(Perception perception)
        {
            if (Tools.TimeToTicks(projData.Position.Distance(estimatedDeathPosition) / projData.Velocity.Length(), perception.Constants.TicksPerSecond) + lastSeenTick < perception.Game.CurrentTick)
            {
                return true;
            }

            return false;
        }
    }
}
