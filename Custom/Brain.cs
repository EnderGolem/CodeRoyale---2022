using System;
using System.Collections.Generic;
using System.Reflection;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public interface Processable<T>
    {
        public int LastActivationTick { get; set; }

        public int LastDeactivationTick { get; set; }
        public bool IsActive { get; set; }
        public void Activate(int currentGameTick);

        public void Deactivate(int currentGameTick);

        public T Process(Perception perception, DebugInterface debugInterface);
    }

    public class Brain : Processable<Dictionary<int,UnitOrder>>
    {
        protected int id = 0;// Чтобы не забыли об этом
        

        /// <summary>
        /// Здесь дожен быть какой - то общий список всех конечных действий на выбор данного мозга
        /// </summary>
        /// <param name="perception"></param>
        public Brain()
        {
            LastActivationTick = -1000;
            LastDeactivationTick = -1000;
        }

        public int LastActivationTick { get; set; }

        public int LastDeactivationTick { get; set; }

        public bool IsActive { get; set; }

        public virtual Dictionary<int,UnitOrder> Process(Perception perception,DebugInterface debugInterface)
        {

            // return currentState.Process(perception, debugInterface);
           return null;
        }

        public void Activate(int currentGameTick)
        {
            if (!IsActive)
            {
                IsActive = true;
                LastActivationTick = currentGameTick;
            }
        }

        public void Deactivate(int currentGameTick)
        {
            if (IsActive)
            {
                IsActive = false;
                LastDeactivationTick = currentGameTick;
            }
        }
    }

    public class SuperBrain : Brain
    {
        protected Brain previousState;
        protected Brain currentState;

        protected List<Brain> allStates;
        protected long[] timeStates;

        public long[] TimeStates => timeStates;

        public SuperBrain(Perception perception)
        {
            previousState = null;
            currentState = null;
            allStates = new List<Brain>();
        }
        

        protected virtual Brain ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            return null;
        }
        
        public override Dictionary<int,UnitOrder> Process(Perception perception,DebugInterface debugInterface)
        {
            Activate(perception.Game.CurrentTick);
            var newState = ChooseNewState(perception,debugInterface);
            if (currentState != newState)
            {
                currentState?.Deactivate(perception.Game.CurrentTick);
                previousState = currentState;
            }
            
            currentState = newState;
            
            return currentState.Process(perception, debugInterface);
        }
    }

    public class EndBrain : Brain
    {
        protected Dictionary<int, EndAction> previousStates;
        protected Dictionary<int, EndAction> currentStates;
        
        private Dictionary<int,Dictionary<string,EndAction>> allStates;
       
        public EndBrain(Perception perception)
        {
            previousStates = new Dictionary<int, EndAction>();
            currentStates = new Dictionary<int, EndAction>();
            allStates=new Dictionary<int, Dictionary<string, EndAction>>();
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                allStates[perception.MyUnints[i].Id] = new Dictionary<string, EndAction>();
                previousStates[perception.MyUnints[i].Id] = null;
                currentStates[perception.MyUnints[i].Id] = null;
            }
        }

        protected void AddState(string actionName,EndAction action,Perception perception)
        {
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                allStates[perception.MyUnints[i].Id][actionName] = action.Copy();
            }
        }

        protected EndAction GetAction(int unitId, string actionName)
        {
            return allStates[unitId][actionName];
        }

        protected virtual Dictionary<int,EndAction> CalculateEndActions(Perception perception, DebugInterface debugInterface)
        {
            return null;
        }

        public override Dictionary<int, UnitOrder> Process(Perception perception, DebugInterface debugInterface)
        {
            Activate(perception.Game.CurrentTick);
            Dictionary<int, UnitOrder> orders = new Dictionary<int, UnitOrder>();
            var actions = CalculateEndActions(perception,debugInterface);
            for (int i = 0; i < perception.MyUnints.Count; i++)
            {
                int id = perception.MyUnints[i].Id;
                if (!actions.ContainsKey(id))
                {
                    currentStates[id]?.Deactivate(perception.Game.CurrentTick);
                    continue;
                }
                var newState = actions[id];
                if (currentStates.ContainsKey(id) && currentStates[id] != newState)
                {
                    currentStates[id]?.Deactivate(perception.Game.CurrentTick);
                    previousStates[id] = currentStates[id];
                }
            
                currentStates[id] = newState;
                newState.SetActingUnit(perception.MyUnints[i]);
                orders[id] = newState.Process(perception, debugInterface);
            }

            return orders;
        }
    }

}
