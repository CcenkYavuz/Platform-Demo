using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Platformer
{
    public class JumpState : BaseState
    {
        public JumpState(PlayerController player, Animator animator) : base(player, animator) { }
        public override void OnEnter()
        {
            Debug.Log("JumpState Entered");
            animator.CrossFade(JumpHash, crossFadeDuration);

        }
        public override void FixedUpdate()
        {
            player.HandleJump();
            player.HandleMovement();
        }

    }
}

