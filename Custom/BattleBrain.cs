using System;
using System.Collections.Generic;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    class BattleBrain : EndBrain
    {
        public const int safeZone = 15;

        private LookAroundAction _lookAroundAction;
        private SteeringRunToDestination _steeringRunToDestination;
        private AimToDestinationDirection _aimToDestinationDirection;
        private SteeringShootToDestinationDirection _steeringShootToDestinationDirection;
        private SteeringAimToDestinationDirection _steeringAimToDestinationDirection;
        public BattleBrain(Perception perception):base(perception)
        {
            _lookAroundAction = new LookAroundAction();
            _steeringRunToDestination = new SteeringRunToDestinationWithEvading();
            _aimToDestinationDirection = new AimToDestinationDirection();
            _steeringAimToDestinationDirection = new SteeringAimToDestinationDirection();
            _steeringShootToDestinationDirection = new SteeringShootToDestinationDirection();
            AddState("LookAround",_lookAroundAction,perception);
            AddState("SteeringRun",_steeringRunToDestination,perception);
            AddState("Aim",_aimToDestinationDirection,perception);
            AddState("SteeringAim",_steeringAimToDestinationDirection,perception);
            AddState("SteeringShoot",_steeringShootToDestinationDirection,perception);
            /*allStates.Add(_lookAroundAction);
            allStates.Add(_steeringRunToDestination);
            allStates.Add(_aimToDestinationDirection);
            allStates.Add(_steeringAimToDestinationDirection);
            allStates.Add(_steeringShootToDestinationDirection);*/
        }

        protected override Dictionary<int,EndAction> CalculateEndActions(Perception perception,DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();
            int unitId = perception.MyUnints[0].Id;///Убрать когда будешь работать над несколькими юнитами
            if (perception.EnemyUnints.Count == 0) //Проверка, вдруг вообще ничего нет
            {
                orderedEndActions[unitId] = GetAction(unitId,"LookAround");
                return orderedEndActions;
            }

            double bestPoints = double.MinValue;
            int bestEnemyIndex = -1;
            double point = 0;
            for (int i = 0; i < perception.EnemyUnints.Count; i++)
            {
                point = CalculateEnemyValue(perception, perception.EnemyUnints[i]);
                if (debugInterface != null)
                    debugInterface.AddPlacedText(perception.EnemyUnints[i].Position, (point).ToString(), new Vec2(0, 0), 0.5, new Color(0, 1, 0.5, 0.7));
                if (bestPoints < point)
                {
                    bestEnemyIndex = i;
                    bestPoints = point;
                }
            }

            var unit = perception.MyUnints[0];
            var enemy = perception.EnemyUnints[bestEnemyIndex];
            var safeDirection = CalculateDodge(perception, debugInterface);
            var distanceToEnemy = perception.MyUnints[id].Position.SqrDistance(perception.EnemyUnints[bestEnemyIndex].Position);
            var estimatedEnemyPosition = CalculateAimToTargetPrediction(ref enemy, perception.Constants.Weapons[perception.MyUnints[0].Weapon.Value].ProjectileSpeed, perception.MyUnints[0].Position);
            // Console.WriteLine("CurrentState " + (currentState == _steeringAimToDestinationDirection));


            if (debugInterface != null)
            {
                debugInterface.AddSegment(unit.Position, unit.Position.Add(unit.Direction.Multi(100)), 0.3, new Color(0, 1, 0, 0.5));
                debugInterface.AddRing(perception.MyUnints[id].Position, safeZone, 0.5, new Color(0, 1, 0.5, 1));
                debugInterface.AddRing(perception.MyUnints[id].Position, 30, 0.5, new Color(0, 1, 0.5, 1));
                debugInterface.AddCircle(estimatedEnemyPosition, 0.4, new Color(1, 0, 0, 1));
                debugInterface.AddPlacedText(enemy.Position.Add(new Vec2(0, 1)), enemy.Velocity.Length().ToString(), new Vec2(0.5, 0.5), 0.5, new Color(1, 0.4, 0.6, 0.5));
                debugInterface.AddSegment(enemy.Position, estimatedEnemyPosition, 0.1, new Color(1, 0.4, 0.6, 0.5));
            }
            
            if (((currentStates[unitId] != _steeringAimToDestinationDirection && currentStates[unitId] != _steeringShootToDestinationDirection) && distanceToEnemy > 30 * 30) ||
                ((currentStates[unitId] == _steeringAimToDestinationDirection || currentStates[unitId] == _steeringShootToDestinationDirection) && distanceToEnemy > 35 * 35)) //Приблежаемся, возможно нужно стрелять. Можно красивее через Active
            {
                ((SteeringRunToDestinationWithEvading)GetAction(unitId,"SteeringRun")).SetDestination(perception.EnemyUnints[bestEnemyIndex].Position);
                orderedEndActions[unitId] = GetAction(unitId,"SteeringRun");
                return orderedEndActions;
            }
            else if (safeZone * safeZone < distanceToEnemy) //Стреляем
            {
                int maxSafeIndex = perception.FindIndexMaxSafeDirection();

                if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, estimatedEnemyPosition,
                                                   perception.Constants.Obstacles, true) == null)
                {
                    ((SteeringShootToDestinationDirection)GetAction(unitId,"SteeringShoot")).SetDestination(perception.MyUnints[id].Position.Add(safeDirection));
                    ((SteeringShootToDestinationDirection)GetAction(unitId,"SteeringShoot")).SetDirection(estimatedEnemyPosition);
                    orderedEndActions[unitId] = GetAction(unitId,"SteeringShoot");
                    return orderedEndActions;
                }

                var stAim = (SteeringAimToDestinationDirection)GetAction(unitId,"SteeringAim");
                if (Tools.RaycastObstacle(perception.MyUnints[id].Position, estimatedEnemyPosition, perception.Constants.Obstacles, true) == null) //Если нет укрытия, просто прицеливаемся, уклоняясь
                    stAim.SetDestination(perception.MyUnints[id].Position.Add(safeDirection));
                if (Tools.RaycastObstacle(perception.MyUnints[id].Position, estimatedEnemyPosition, perception.Constants.Obstacles, true) != null) //Если есть укрытие то
                {
                    if (perception.EnemiesAimingYou.Contains(enemy.Id))
                        stAim.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position)); //Если Смотрит на нас, то отходим, отдалясь от укрытия
                    else                                                                                                                    //Если не смотрит, то приближаемся
                        stAim.SetDestination(enemy.Position);

                }
                stAim.SetDirection(estimatedEnemyPosition);
                orderedEndActions[unitId] = GetAction(unitId,"SteeringAim");
                return orderedEndActions;
            }
            else  //Отступаем
            {
                if (perception.MyUnints[id].Aim == 1 && Tools.RaycastObstacle(perception.MyUnints[id].Position, estimatedEnemyPosition,
                        perception.Constants.Obstacles, true) == null)
                {
                    _steeringShootToDestinationDirection.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position));
                    _steeringShootToDestinationDirection.SetDirection(estimatedEnemyPosition);
                    orderedEndActions[unitId] = _steeringShootToDestinationDirection;
                    return orderedEndActions;
                }
                _steeringAimToDestinationDirection.SetDestination(perception.MyUnints[id].Position.FindMirrorPoint(enemy.Position));
                _steeringAimToDestinationDirection.SetDirection(estimatedEnemyPosition);
                orderedEndActions[unitId] = GetAction(unitId,"SteeringAim");
                return orderedEndActions;
                
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

        Vec2 CalculateAimToTargetPrediction(ref Unit enemy, double bulletSpeed, Vec2 shotPosition) //Возможно неправильно просчитывает
        {
            double estimatedFlyTime = enemy.Position.Distance(shotPosition) / bulletSpeed;

            Vec2 estimatedEnemyPosition = enemy.Position.Add(enemy.Velocity.Multi(estimatedFlyTime));


            for (int i = 0; i < 100; i++) //Тут был раньше 5!
            {
                estimatedFlyTime = estimatedEnemyPosition.Distance(shotPosition) / bulletSpeed;
                estimatedEnemyPosition = enemy.Position.Add(enemy.Velocity.Multi(estimatedFlyTime));
            }
            return estimatedEnemyPosition.Add(estimatedEnemyPosition.Substract(enemy.Position).Multi(0.45));
        }

        Vec2 CalculateDodge(Perception perception, DebugInterface debugInterface)
        {
            if (perception.Game.Projectiles.Length == 0)
                return new Vec2(0, 0);
            int indexNearest = 0;
            for (int i = 0; i < perception.Game.Projectiles.Length; i++)
            {
                if (perception.Game.Projectiles[i].Id != perception.Game.MyId)
                    if (perception.Game.Projectiles[i].Position.SqrDistance(perception.MyUnints[0].Position) <
                        perception.Game.Projectiles[indexNearest].Position.SqrDistance(perception.MyUnints[0].Position))
                    {
                        indexNearest = i;
                    }
            }
            var bullet = perception.Game.Projectiles[indexNearest];
            var safeDirection1 = bullet.Position.FindPerpendicularWithX(perception.MyUnints[0].Position.X);
            var safeDirection2 = bullet.Position.FindPerpendicularWithX(perception.MyUnints[0].Position.X).Multi(-1);
            var lineBullet = new Straight(bullet.Velocity, bullet.Position);
            var lineDirection = new Straight(safeDirection1, perception.MyUnints[0].Position);
            var point = lineBullet.GetIntersection(lineDirection);
            //  System.Console.WriteLine($"SafeDirection1 {safeDirection1} SafeDirection{safeDirection2}");
            if (debugInterface != null)
                debugInterface.AddSegment(bullet.Position, bullet.Position.Add(bullet.Velocity), 0.1, new Color(0.7, 0.3, 0, 0.8));
            if (point.Value.SqrDistance(perception.MyUnints[0].Position.Add(safeDirection1)) > point.Value.SqrDistance(perception.MyUnints[0].Position.Add(safeDirection2)))
                return safeDirection1;
            else
                return safeDirection2;



        }
    }
}
