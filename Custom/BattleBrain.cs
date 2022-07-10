using System;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    class BattleBrain : Brain
    {

        ShootToPoint _shootToPoing;
        AimingToPoint _aimingToPoint;
        private LookAroundAction _lookAroundAction;
        SteeringRunToDestination _steeringRunToDestination;
        SteeringAimToDestination _steeringAimToDestination;
        
        public BattleBrain()
        {
            _shootToPoing = new ShootToPoint();
            _aimingToPoint = new AimingToPoint();
            _lookAroundAction = new LookAroundAction();
            _steeringRunToDestination = new SteeringRunToDestination();
            _steeringAimToDestination = new SteeringAimToDestination();
            allStates.Add(_shootToPoing);
            allStates.Add(_aimingToPoint);
            allStates.Add(_lookAroundAction);
            allStates.Add(_steeringAimToDestination);
            allStates.Add(_steeringRunToDestination);
        }

        protected override Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            if (perception.EnemyUnints.Count == 0)   //Проверка, вдруг вообще ничего нет
                return _lookAroundAction;
            double bestPoints = double.MinValue;
            int bestEnemyIndex = -1;
            double point = 0;
            for (int i = 0; i < perception.EnemyUnints.Count; i++)
            {
                point = CalculateEnemyValue(perception, perception.EnemyUnints[i]);
                debugInterface.AddPlacedText(perception.EnemyUnints[i].Position, (point).ToString(), new Vec2(0.5, 0.5), 3, new Color(0, 1, 0.5, 1));
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
                return _shootToPoing;
            }
            _aimingToPoint.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
            return _aimingToPoint;

            debugInterface.AddRing(perception.MyUnints[id].Position, 30, 0.5, new Color(0, 1, 0.5, 1));

            //Надо бы хранить состояния еще, чтобы была возможно, стрелять по убегающему, не переходя в режим догоняния
            /*if (perception.MyUnints[id].Position.SqrDistance(perception.EnemyUnints[bestEnemyIndex].Position) > 35 * 35) //Приблежаемся, возможно нужно стрелять
            {
                _steeringRunToDestination.SetDestination(perception.EnemyUnints[bestEnemyIndex].Position);
                return _steeringRunToDestination.Process(perception, debugInterface, id);
            }
            else if (30 * 30 < perception.MyUnints[id].Position.SqrDistance(perception.EnemyUnints[bestEnemyIndex].Position)) //Стреляем
            {
                if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, (perception.EnemyUnints[bestEnemyIndex].Position),
                                                   perception.Constants.Obstacles, true) == null)
                {
                    _shootToPoing.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
                    return _shootToPoing.Process(perception, id);
                }
                _aimingToPoint.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
                return _aimingToPoint.Process(perception, id);
            }

            else  //Отступаем
            {
                System.Console.WriteLine("RUUUUN");
                /* var unit = perception.MyUnints[id];
               _runToDestinationDirection.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
               _runToDestinationDirection.SetDestination(unit.Position.Subtract(unit.Velocity));
               return _runToDestinationDirection.Process(perception, debugInterface, id);
               if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, (perception.EnemyUnints[bestEnemyIndex].Position),
                                                     perception.Constants.Obstacles, true) == null)
                 {
                     _steeringShootToDestination.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
                     _steeringShootToDestination.SetDestination(new Vec2(-perception.MyUnints[id].Direction.X, -perception.MyUnints[id].Direction.Y)); //Возможно отсупать от движения противника
                     return _steeringShootToDestination.Process(perception, debugInterface, id);
                 }
                 _steeringAimToDestination.SetTarget(perception.EnemyUnints[bestEnemyIndex].Position);
                 _steeringAimToDestination.SetDestination(new Vec2(-perception.MyUnints[id].Direction.X, -perception.MyUnints[id].Direction.Y)); //Возможно отсупать от движения противника
                 return _steeringAimToDestination.Process(perception, debugInterface, id);
               
                throw new System.NotImplementedException();
            }*/
            
        }
        
        double CalculateEnemyValue(Perception perception, Unit enemy)
        {
            double points = 1 / enemy.Position.SqrDistance(perception.MyUnints[id].Position);
            points *= Tools.RaycastObstacle(perception.MyUnints[id].Position, (enemy.Position), perception.Constants.Obstacles, true) == null ? 2 : 1; //Под вопросом такое
            //Просчет по тому, насколько он близок к выходу из укрытия, как идея, ведь в финале это не нужно будет
            //Высчитывается ценность противника
            return points;
        }
    }
}
