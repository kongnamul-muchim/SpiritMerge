using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 간단한 FSM (Finite State Machine)
    /// </summary>
    public class StateMachine
    {
        private IState _currentState;

        public IState CurrentState => _currentState;

        public void ChangeState(IState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void Update()
        {
            _currentState?.Update();
        }

        public void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }
    }

    public interface IState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }

    public abstract class BaseState : IState
    {
        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }
}
