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


        public Constants Constants => _constants;
        public Game Game => _game;
        public DebugInterface Debug => _debug;
        public List<Unit> MyUnints => _myUnints;
        public List<Unit> EnemyUnints => _enemyUnints;

        public Perception(Constants consts)
        {
            _constants = consts;
        }

        public void Analyze(Game game, DebugInterface debugInterface)
        {
            _game = game;
            _debug = debugInterface;
            DebugOutput(game, debugInterface);

            _enemyUnints = new List<Unit>();
            _myUnints = new List<Unit>(); // Потому что, если находиться в конструкторе, то каждый getorder, будет увеличиваться
            foreach (var unit in game.Units)
            {
                if (unit.PlayerId != game.MyId)
                {
                    _enemyUnints.Add(unit);
                    continue;
                }

                MyUnints.Add(unit);
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

                Vec2 debugTextPos = debugInterface.GetState().Camera.Center.Add(offset);
                debugInterface.Add(new DebugData.PlacedText(debugTextPos,
                    $"Health: {player.Health}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                debugInterface.Add(new DebugData.PlacedText(debugTextPos.Subtract(new Vec2(0, textsize / 2)),
                    $"Shield: {player.Shield}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                debugInterface.Add(new DebugData.PlacedText(debugTextPos.Subtract(new Vec2(0, 2 * textsize / 2)),
                    $"Potions: {player.ShieldPotions}",
                    new Vec2(0.5, 0.5), textsize, textColor));
                debugInterface.Add(new DebugData.PlacedText(debugTextPos.Subtract(new Vec2(0, 3 * textsize / 2)),
                    $"Velocity: {player.Velocity}",
                    new Vec2(0.5, 0.5), textsize, textColor));
            }
        }
    }
}
