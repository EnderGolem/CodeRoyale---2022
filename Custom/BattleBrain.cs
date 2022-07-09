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
        public override UnitOrder Process(Perception perception,DebugInterface debugInterface)
        {
            _aimingToPoint.SetTarget(perception.MyUnints[id].Direction);
            return _aimingToPoint.Process(perception, id);
        }
    }
}
