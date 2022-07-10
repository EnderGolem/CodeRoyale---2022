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

        public List<Obstacle> CloseObstacles => closeObstacles;

        public Dictionary<int, Loot> MemorizedLoot => memorizedLoot;

        private Dictionary<int,Loot> memorizedLoot;

        private List<Obstacle> closeObstacles;
        public Perception(Constants consts)
        {
            _constants = consts;
            memorizedLoot = new Dictionary<int, Loot>();
            closeObstacles = new List<Obstacle>();
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
                    continue;
                }

                MyUnints.Add(unit);
            }

            for (int i = 0; i < game.Loot.Length; i++)
            {
                memorizedLoot.TryAdd(game.Loot[i].Id,game.Loot[i]);
            }

            if (lastObstacleRecalculationTick + closeObstaclesRecalculationDelay <= game.CurrentTick)
            {
                CalculateCloseObstacles(_myUnints[0].Position,obstacleSearchRadius);
            }

            DebugOutput(game, debugInterface);
        }

        private void CalculateCloseObstacles(Vec2 startPosition, double distance)
        {
            closeObstacles.Clear();
            for (int i = 0; i < Constants.Obstacles.Length; i++)
            {
                if (_constants.Obstacles[i].Position.SqrDistance(startPosition)<distance*distance)
                {
                    closeObstacles.Add(_constants.Obstacles[i]);
                }
            }
        }

        private void DebugOutput(Game game, DebugInterface debugInterface)
        {
            if (debugInterface != null)
            {
                Vec2 offset = new Vec2(-5, -20);
                double textsize = 2;
                Color textColor = new Color(0, 0, 1, 1);
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
                debugInterface.Add(new DebugData.PlacedText(debugTextPos,
                    $"Health: {player.Health}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                debugInterface.Add(new DebugData.PlacedText(debugTextPos.Substract(new Vec2(0, textsize / 2)),
                    $"Shield: {player.Shield}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                debugInterface.Add(new DebugData.PlacedText(debugTextPos.Substract(new Vec2(0, 2 * textsize / 2)),
                    $"Potions: {player.ShieldPotions}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                debugInterface.Add(new DebugData.PlacedText(debugTextPos.Substract(new Vec2(0, 3 * textsize / 2)),
                    $"Velocity: {player.Velocity}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                
                
                debugInterface.AddRing(game.Zone.CurrentCenter,game.Zone.CurrentRadius,1,new Color(1,0,0,1));
                debugInterface.AddRing(game.Zone.NextCenter,game.Zone.NextRadius,1,new Color(0,1,0,1));
                //Console.WriteLine(memorizedLoot.Count);
                foreach (var l in memorizedLoot)
                {
                    debugInterface.AddRing(l.Value.Position,1,0.3,new Color(0.8,0.8,0.8,1));
                }
                
                foreach (var o in closeObstacles)
                {
                    debugInterface.AddRing(o.Position,0.3,0.3,new Color(0.8,0.8,0,1));
                }
            }
        }
    }
}
