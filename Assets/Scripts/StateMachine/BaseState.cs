using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Platformer
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerController player;
        protected readonly Animator animator;
        protected static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        protected static readonly int JumpHash = Animator.StringToHash("Jump");
        protected static readonly int DashHash = Animator.StringToHash("Dash");

        protected const float crossFadeDuration = 0.1f;
        protected BaseState(PlayerController player, Animator animator)
        {
            this.player = player;
            this.animator = animator;
        }

        public virtual void OnEnter()
        {
            //throw new System.NotImplementedException();
        }
        public virtual void Update()
        {
            //throw new System.NotImplementedException();
        }
        public virtual void FixedUpdate()
        {
            //throw new System.NotImplementedException();
        }
        public virtual void OnExit()
        {
            Debug.Log("BaseStateExit");
            //throw new System.NotImplementedException();
        }


    }

}
