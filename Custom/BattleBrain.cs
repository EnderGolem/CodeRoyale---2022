using System;
using AiCup22.Custom;
using AiCup22.Model;

namespace AiCup22.Custom
{
    class BattleBrain : Brain
    {

        ShootToPoint _shootToPoing;
        AimingToPoint _aimingToPoint;
        public BattleBrain()
        {
            _shootToPoing = new ShootToPoint();
            _aimingToPoint = new AimingToPoint();
        }
        public override UnitOrder Process(Perception perception)
        {
            if (perception.EnemyUnints.Count == 0)   //Проверка, вдруг вообще ничего нет
                return new UnitOrder(new Vec2(), new Vec2(), null);
            double bestPoints = double.MinValue;
            int bestEnemyIndex = -1;
            double point = 0;
            for (int i = 0; i < perception.EnemyUnints.Count; i++)
            {
                point = CalculateEnemyValue(perception, perception.EnemyUnints[0]);
                if (bestPoints < point)
                {
                    bestEnemyIndex = i;
                    bestPoints = point;
                }
            }
            if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, (perception.EnemyUnints[bestEnemyIndex].Position),
                                                    perception.Constants.Obstacles,true) == null)
            {
                _shootToPoing.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
                return _shootToPoing.Process(perception, id);
            }
            _aimingToPoint.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
            return _aimingToPoint.Process(perception, id);
        }
        double CalculateEnemyValue(Perception perception, Unit enemy)
        {
            double points = -enemy.Position.SqrDistance(perception.MyUnints[id].Position);  //Не корректно, лучше вообще работать без минуса
            //Высчитывается ценность противника
            return points;
        }
    }
}
