using System;
using AiCup22.Custom;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public interface Processable
    {
        public UnitOrder Process(Perception perception);
    }

    public class Brain : Processable
    {

        private Processable currentState;

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

        public virtual UnitOrder Process(Perception perception)
        {
            if (perception.EnemyUnints.Count == 0)
                return runToCenterRadar.Process(perception, 0);
            else
                return stayShootToEnemy.Process(perception, 0);
        }
    }

}
