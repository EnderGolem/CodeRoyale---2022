using System;
using System.Collections.Generic;
using AiCup22.Custom;
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
        protected Processable currentState;

        protected List<Processable> allStates;


        /// <summary>
        /// Здесь дожен быть какой - то общий список всех конечных действий на выбор данного мозга
        /// </summary>
        /// <param name="perception"></param>
        public Brain()
        {
            allStates = new List<Processable>();
        }

        public int LastActivationTick { get; set; }

        public int LastDeactivationTick { get; set; }

        public bool IsActive { get; set; }

        public virtual UnitOrder Process(Perception perception, DebugInterface debugInterface)
        {
            throw new System.NotImplementedException();
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
