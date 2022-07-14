using System;
using System.Collections.Generic;
using AiCup22.Custom;
using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Custom
{
    public interface Processable
    {
        public int LastActivationTick { get; set; }

        public int LastDeactivationTick { get; set; }
        public bool IsActive { get; set; }
        public void Activate(int currentGameTick);

        public void Deactivate(int currentGameTick);

        public UnitOrder Process(Perception perception, DebugInterface debugInterface);
    }

    public class Brain : Processable
    {
        protected int id = 0;// Чтобы не забыли об этом
        protected Processable previousState;
        protected Processable currentState;

        protected List<Processable> allStates;
        protected long[] timeStates;

        public long[] TimeStates => timeStates;

        /// <summary>
        /// Здесь дожен быть какой - то общий список всех конечных действий на выбор данного мозга
        /// </summary>
        /// <param name="perception"></param>
        public Brain()
        {
            previousState = null;
            currentState = null;
            allStates = new List<Processable>();
            LastActivationTick = -1000;
            LastDeactivationTick = -1000;
        }

        public int LastActivationTick { get; set; }

        public int LastDeactivationTick { get; set; }

        public bool IsActive { get; set; }
        
        protected virtual Processable ChooseNewState(Perception perception, DebugInterface debugInterface)
        {
            return null;
        }

        public UnitOrder Process(Perception perception,DebugInterface debugInterface)
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

}
