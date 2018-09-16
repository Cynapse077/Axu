using UnityEngine;

namespace AxuAI
{
    [System.Serializable]
    public class Transition
    {
        public Decision decision;
        public State trueState;
        public State falseState;

        public bool Decide(StateController controller)
        {
            return decision.Decide(controller);
        }
    }

    [System.Serializable]
    public class Decision : ScriptableObject
    {
        public virtual bool Decide(StateController controller)
        {
            return true;
        }
    }

    [CreateAssetMenu(menuName = "AI/Decisions/IsHostile")]
    public class IsHostile : Decision
    {
        public new string name = "IsHostile";

        public override bool Decide(StateController controller)
        {
            return controller.ai.isHostile;
        }
    }

    [CreateAssetMenu(menuName = "AI/Decisions/IsFollower")]
    public class IsFollower : Decision
    {
        public new string name = "IsFollower";

        public override bool Decide(StateController controller)
        {
            return controller.ai.isFollower();
        }
    }

    public class CanUseAbility : Decision
    {
        public new string name = "CanUseAbility";

        public override bool Decide(StateController controller)
        {
            return AIManager.TryUseSkills(controller.ai, controller.ai.target);
        }
    }
}

