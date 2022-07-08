using System.Collections.Generic;
using AiCup22.Model;
using AiCup22.Custom;

namespace AiCup22
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
            _enemyUnints = new List<Unit>();
            _myUnints = new List<Unit>(); // Потому что, если находиться в конструкторе, то каждый getorder, будет увеличиваться
            foreach (var unit in game.Units)
            {
                if (unit.PlayerId != game.MyId) { _enemyUnints.Add(unit); continue; }
                MyUnints.Add(unit);
            }
        }
    }

    public interface Processable
    {
        public UnitOrder Process(Perception perception);
    }

    public class Brain : Processable
    {
        RunToCenterRadar runToCenterRadar;
        RunToCenter runToCenter;
        StayShootToEnemy stayShootToEnemy;
        /// <summary>
        /// Здесь дожен быть какой - то общий список всех конечных действий на выбор данного мозга
        /// </summary>
        /// <param name="perception"></param>
        public Brain()
        {
            runToCenterRadar = new RunToCenterRadar();
            runToCenter = new RunToCenter();
            stayShootToEnemy = new StayShootToEnemy();
        }

        public UnitOrder Process(Perception perception)
        {
            if (perception.EnemyUnints.Count == 0)
                return runToCenterRadar.Process(perception, 0);
            else
                return stayShootToEnemy.Process(perception, 0);
        }
    }

    public class MyStrategy
    {
        private Perception perception;
        private Brain brain;

        public MyStrategy(AiCup22.Model.Constants constants)
        {
            perception = new Perception(constants);
            brain = new Brain();
        }

        public AiCup22.Model.Order GetOrder(AiCup22.Model.Game game, DebugInterface debugInterface)
        {

            perception.Analyze(game, debugInterface);
            Dictionary<int, AiCup22.Model.UnitOrder> orders = new Dictionary<int, UnitOrder>();
            orders.Add(perception.MyUnints[0].Id, brain.Process(perception));
            return new Order(orders);
        }
        public void DebugUpdate(DebugInterface debugInterface) { }
        public void Finish() { }
    }

}