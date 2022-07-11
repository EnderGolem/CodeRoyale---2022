using System;
using AiCup22.Custom;
using AiCup22.Model;
using Color = AiCup22.Debugging.Color;
using System.Drawing;
using AiCup22.Debugging;
using System.Collections.Generic;

namespace AiCup22.Custom
{
    public class Perception
    {
        private Game _game;
        private DebugInterface _debug;
        private Constants _constants;

        private List<Unit> _myUnints;
        private List<Unit> _enemyUnints;

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

        public Dictionary<int, (int, double, Unit)> MemorizedEnemies => memorizedEnemies;

        private Dictionary<int, Loot> memorizedLoot;
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
            closeObstacles = new List<Obstacle>();
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

            if (lastObstacleRecalculationTick + closeObstaclesRecalculationDelay <= game.CurrentTick)
            {
                CalculateCloseObstacles(_myUnints[0].Position, obstacleSearchRadius);
            }
            EstimateDirections(game, debugInterface);
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
                        _myUnints[0].Position, directions[i], 180/directions.Length))
                    {
                        directionDangers[i] += enemy.Value.Item2;
                        break;
                    }
                }
            }
            foreach (var bullet in game.Projectiles)
            {
                if (bullet.ShooterPlayerId != game.MyId)
                    for (int i = 0; i < directions.Length; i++)
                    {
                        if (Tools.BelongDirection(bullet.Position,
                            _myUnints[0].Position, directions[i], 180 / directions.Length))
                        {
                            //Просчет, куда надо идти и в какую сторону безопасней
                            var id1 = (i + 6) % 8;  //Типо -2
                            var id2 = (i + 2) % 8;
                            var lineBullet = new Straight(bullet.Velocity, bullet.Position);
                            var lineDirection = new Straight(directions[id1], MyUnints[0].Position);
                            var point = lineBullet.GetIntersection(lineDirection);
                            if (point.Value.SqrDistance(MyUnints[0].Position.Add(directions[id1])) > point.Value.SqrDistance(MyUnints[0].Position.Add(directions[id2])))
                                directionDangers[id1] += EstimateBulletDanger(bullet);
                            else
                                directionDangers[id2] += EstimateBulletDanger(bullet);
                            System.Console.WriteLine("EstimateBulletDanger " + EstimateBulletDanger(bullet));
                            System.Console.WriteLine("Point 1 distance: " + point.Value.SqrDistance(MyUnints[0].Position.Add(directions[id1])));
                            System.Console.WriteLine("Point 2 distance: " + point.Value.SqrDistance(MyUnints[0].Position.Add(directions[id2])));

                            debugInterface.AddCircle(MyUnints[0].Position.Add(directions[id1]), 0.1, new Color(0, 1, 0, 1));
                            debugInterface.AddCircle(MyUnints[0].Position.Add(directions[id2]), 0.1, new Color(0, 1, 0, 1));
                            debugInterface.AddCircle(point.Value, 0.1, new Color(0, 0, 1, 1));
                            debugInterface.AddSegment(bullet.Position, bullet.Position.Add(bullet.Velocity), 0.1, new Color(0.7, 0.3, 0, 0.8));
                            break;
                        }
                    }
            }
            //for (int i = 0; i < directions.Length; i++) //Расскомитить
            //{
            //    if (Tools.CurrentZoneDistance(game.Zone, _myUnints[0].Position.Add(directions[i].Normalize().Multi(30))) < 0)
            //    {
             //  directionDangers[i] += Math.Pow(30-Tools.CurrentZoneDistance(game.Zone,_myUnints[0].Position),2);
              //  }
           // }
            
          //  double[] add = new double[directions.Length];
         //   for (int i = 0; i < directions.Length; i++)
         //   {
         //       int prev = (i==0)?7:i - 1;
         //       int next = (i + 1) % Directions.Length;
        //        add[prev] += directionDangers[i] * 0.5;
         //       add[next] += directionDangers[i] * 0.5;
        //    }
        //
        //    for (int i = 0; i < add.Length; i++)
         //   {
          //      directionDangers[i] += add[i];
        //    }
        
        
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
                debugInterface.AddPlacedText(debugTextPos, $"Health: {player.Health}\n  Shield: { player.Shield}Potions: {player.ShieldPotions}\nVelocity: {player.Velocity}", new Vec2(0.5, 0.5), 1, new Color(0, 0, 1, 1));
                //debugInterface.Add(new DebugData.PlacedText(debugTextPos,
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

                for (int i = 0; i < directions.Length; i++)
                {
                    Console.WriteLine($"{i}. {directionDangers[i]}");
                    Debug.AddPlacedText(_myUnints[0].Position.Add(directions[i].Multi(30)),
                        directionDangers[i].ToString(),
                        new Vec2(0, 0), 3, new Color(1, 0.2, 1, 0.7));
                }
            }
        }
    }
}
