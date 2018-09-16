using UnityEngine;

namespace AxuAI
{
    [System.Serializable]
    public class State : ScriptableObject
    {
        public Transition[] transitions;

        public virtual void Act(StateController controller)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                controller.ChangeState(transitions[i].Decide(controller) ? transitions[i].trueState : transitions[i].falseState);
            }
        }

        public virtual void EnterState(StateController controller) {}
        public virtual void ExitState(StateController controller) {}
    }

    [CreateAssetMenu(menuName = "AI/States/Remain")]
    public class RemainState : State
    {
        public override void Act(StateController controller)
        {
            base.Act(controller);
        }

        public override void EnterState(StateController controller)
        {
            base.EnterState(controller);
        }

        public override void ExitState(StateController controller)
        {
            base.ExitState(controller);
        }
    }

    [CreateAssetMenu(menuName = "AI/States/End")]
    public class EndState : State
    {
        public override void Act(StateController controller)
        {
            base.Act(controller);
        }

        public override void EnterState(StateController controller)
        {
            base.EnterState(controller);
        }

        public override void ExitState(StateController controller)
        {
            base.ExitState(controller);
        }
    }

    public class StateController : MonoBehaviour
    {
        public BaseAI ai;
        public State remainState;
        public State CurrentState { get; protected set; }
        public State PreviousState { get; protected set; }

        public void Act()
        {
            CurrentState.Act(this);
        }

        public void ChangeState(State newState)
        {
            if (newState == remainState)
                return;

            CurrentState.ExitState(this);
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.EnterState(this);
        }
    }
}

