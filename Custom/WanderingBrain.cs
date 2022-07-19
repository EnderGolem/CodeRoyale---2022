using System;
using System.Collections.Generic;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public class WanderingBrain : EndBrain
    {
        protected const double wanderUnitDistance=10;
        protected const double wanderingAngle = 4;
        protected const double zoneIndent = 10;
        protected const double gapDistance = 8;

        private List<Vec2> wanderPositions;
        public WanderingBrain(Perception perception) : base(perception)
        {
            AddState("Run",new SteeringRunToDestinationWithEvading(), perception);
            AddState("LookAround", new LookAroundWithEvading(), perception);
        }

        protected override Dictionary<int, EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            Dictionary<int, EndAction> orderedEndActions = new Dictionary<int, EndAction>();
            wanderPositions = CalculateWanderPositions(perception, debugInterface);
            /*for (int i = 0; i < wanderPositions.Count; i++)
            {
                debugInterface.AddCircle(wanderPositions[i],0.5,new Color(0,0,1,1));
            }*/
            
            bool[] gapping = new bool[perception.MyUnints.Count];
            if (gapping.Length > 1)
            {
                double[] distances = new double[gapping.Length];
                for (int i = 0; i < perception.MyUnints.Count; i++)
                {
                    distances[i] = perception.MyUnints[i].Position.Distance(wanderPositions[i]);
                }

                for (int i = 0; i < perception.MyUnints.Count; i++)
                {
                    double maxGap = 1000000;
                    for (int j = 0; j < perception.MyUnints.Count; j++)
                    {
                        var gap = distances[i] - distances[j];
                        if (gap < maxGap)
                        {
                            maxGap = gap;
                        }
                    }

                    if (maxGap<0 && -maxGap>gapDistance)
                    {
                        gapping[i] = true;
                    }
                }
            }

            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                var curUnit = perception.MyUnints[i];
                var run = (SteeringRunToDestinationWithEvading) GetAction(curUnit.Id, "Run");
                var lookAround = (LookAroundWithEvading) GetAction(curUnit.Id,"LookAround");
                if (!gapping[i])
                {
                    run.SetDestination(wanderPositions[i]);
                    orderedEndActions[curUnit.Id] = run;
                }
                else
                {
                    //debugInterface.AddRing(curUnit.Position,5,1,new Color(0,1,0,0.5));
                    orderedEndActions[curUnit.Id] = lookAround;
                }
            }
            return orderedEndActions;
        }

        protected List<Vec2> CalculateWanderPositions(Perception perception,DebugInterface debugInterface)
        {
            List<Vec2> res = new List<Vec2>();
            Vec2 vecToZoneEdgeNor = perception.AverageUnitPosition.Substract(perception.Game.Zone.CurrentCenter)
                .Normalize();
            Vec2 vecToZoneEdge = vecToZoneEdgeNor.Multi(perception.Game.Zone.CurrentRadius);
            double averageZoneDistance = Tools.CurrentZoneDistance(perception.Game.Zone,perception.AverageUnitPosition);
            if (averageZoneDistance>0 && averageZoneDistance<zoneIndent+perception.MyUnints.Count*wanderUnitDistance)
            {
                vecToZoneEdge = vecToZoneEdge.Rotate(wanderingAngle);
                vecToZoneEdgeNor = vecToZoneEdge.Normalize();
            }

            //debugInterface.AddSegment(perception.Game.Zone.CurrentCenter,perception.Game.Zone.CurrentCenter.Add(vecToZoneEdge),1,new Color(1,0,0,1));
            res.Add(perception.Game.Zone.CurrentCenter.Add(vecToZoneEdge.Substract(vecToZoneEdgeNor.Multi(zoneIndent))));
            for (int i = 1; i < perception.MyUnints.Count; i++)
            {
                res.Add(res[i-1].Substract(vecToZoneEdgeNor.Multi(wanderUnitDistance)));
            }

            return res;
        }
    }
}