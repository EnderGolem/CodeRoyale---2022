using System;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    class BattleBrain : Brain
    {
        public const int safeZone = 15;

        private LookAroundAction _lookAroundAction;
        private SteeringRunToDestination _steeringRunToDestination;
        private AimToDestinationDirection _aimToDestinationDirection;
        private SteeringShootToDestinationDirection _steeringShootToDestinationDirection;
        private SteeringAimToDestinationDirection _steeringAimToDestinationDirection;
        public BattleBrain()
        {
            _lookAroundAction = new LookAroundAction();
            _steeringRunToDestination = new SteeringRunToDestination();
            _aimToDestinationDirection = new AimToDestinationDirection();
            _steeringAimToDestinationDirection = new SteeringAimToDestinationDirection();
            _steeringShootToDestinationDirection = new SteeringShootToDestinationDirection();
            allStates.Add(_lookAroundAction);
            allStates.Add(_steeringRunToDestination);
            allStates.Add(_aimToDestinationDirection);
            allStates.Add(_steeringAimToDestinationDirection);
            allStates.Add(_steeringShootToDestinationDirection);
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
            //Удалить эти комментарии после 12 июля, есть не пригодились до этого, то зачем они нужны в дальнейшем
            //var enemy = perception.EnemyUnints[bestEnemyIndex];
            //if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, (perception.EnemyUnints[bestEnemyIndex].Position),
            //        perception.Constants.Obstacles, true) == null)
            //{
            //    _steeringShootToDestinationDirection.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position));
            //    _steeringShootToDestinationDirection.SetDirection(enemy.Position);
            //    return _steeringShootToDestinationDirection;
            //}
            //_steeringAimToDestinationDirection.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position));
            //_steeringAimToDestinationDirection.SetDirection(enemy.Position);
            //return _steeringAimToDestinationDirection;

            debugInterface.AddRing(perception.MyUnints[id].Position, safeZone, 0.5, new Color(0, 1, 0.5, 1));
            var enemy = perception.EnemyUnints[bestEnemyIndex];

            // Надо бы хранить состояния еще, чтобы была возможно, стрелять по убегающему, не переходя в режим догоняния
            if (perception.MyUnints[id].Position.SqrDistance(perception.EnemyUnints[bestEnemyIndex].Position) > 35 * 35) //Приблежаемся, возможно нужно стрелять
            {
                _steeringRunToDestination.SetDestination(perception.EnemyUnints[bestEnemyIndex].Position);
                return _steeringRunToDestination;
            }
            else if (safeZone * safeZone < perception.MyUnints[id].Position.SqrDistance(perception.EnemyUnints[bestEnemyIndex].Position)) //Стреляем
            {
                if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, (perception.EnemyUnints[bestEnemyIndex].Position),
                                                   perception.Constants.Obstacles, true) == null)
                {
                    _steeringShootToDestinationDirection.SetDestination(perception.MyUnints[id].Position.Add(Vec2Extensions.GetRandomVecNormalize()));
                    _steeringShootToDestinationDirection.SetDirection(enemy.Position);
                    return _steeringShootToDestinationDirection;
                }
                _steeringAimToDestinationDirection.SetDestination(perception.MyUnints[id].Position.Add(Vec2Extensions.GetRandomVecNormalize()));
                _steeringAimToDestinationDirection.SetDirection(enemy.Position);
                return _steeringAimToDestinationDirection;
            }
            else  //Отступаем
            {
                System.Console.WriteLine("RUUUUN id: " + id);
                if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, (perception.EnemyUnints[bestEnemyIndex].Position),
                        perception.Constants.Obstacles, true) == null)
                {
                    _steeringShootToDestinationDirection.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position));
                    _steeringShootToDestinationDirection.SetDirection(enemy.Position);
                    return _steeringShootToDestinationDirection;
                }
                _steeringAimToDestinationDirection.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position));
                _steeringAimToDestinationDirection.SetDirection(enemy.Position);
                return _steeringAimToDestinationDirection;

                throw new System.NotImplementedException();
            }

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
