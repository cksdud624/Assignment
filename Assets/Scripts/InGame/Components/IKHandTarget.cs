using UnityEngine;

namespace InGame.Components
{
    public class IKHandTarget : MonoBehaviour
    {
        public Transform LeftHandTarget { get; set; }
        public Transform RightHandTarget { get; set; }

        private void OnAnimatorIK(int layerIndex)
        {
            var animator = GetComponent<Animator>();
            if (animator == null) return;

            ApplyIK(animator, AvatarIKGoal.LeftHand, LeftHandTarget);
            ApplyIK(animator, AvatarIKGoal.RightHand, RightHandTarget);
        }

        private static void ApplyIK(Animator animator, AvatarIKGoal goal, Transform target)
        {
            var weight = target != null ? 1f : 0f;
            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, weight);
            if (target == null) return;
            animator.SetIKPosition(goal, target.position);
            animator.SetIKRotation(goal, target.rotation);
        }

        public void Clear()
        {
            LeftHandTarget = null;
            RightHandTarget = null;
        }
    }
}
