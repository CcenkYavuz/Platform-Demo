using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utilities;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        // [SerializeField] CharacterController characterController;
        [SerializeField] Animator animator;
        [SerializeField] CinemachineFreeLook freeLookVCam;
        [SerializeField] GroundChecker groundChecker;
        [SerializeField] InputReader input;
        [SerializeField] Rigidbody rb;
        [SerializeField] TargetManager targetManager;

        [Header("Movement Settings")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float rotationSpeed = 15f;
        [SerializeField] float smoothTime = 0.2f;

        [Header("Jump Settings")]
        [SerializeField] float jumpForce = 10f;
        [SerializeField] float jumpDuration = .5f;
        [SerializeField] float jumpCooldown = 0f;
        [SerializeField] float gravityMultiplier = 3f;

        [Header("Dash Settings")]
        [SerializeField] float dashForce = 10f;
        [SerializeField] float dashDuration = 1f;
        [SerializeField] float dashCooldown = 2f;

        [Header("Shield Settings")]
        [SerializeField] float shieldDuration = 2f;
        [SerializeField] float shieldCooldown = 3f;

        const float ZeroF = 0f;

        Transform mainCam;
        float currentSpeed;
        float velocity;
        float jumpVelocity;

        float dashVelocity = 1.0f;
        Vector3 movement;
        bool hasShield = false;

        List<Utilities.Timer> timers;
        CountdownTimer jumpTimer;
        CountdownTimer jumpCooldownTimer;
        CountdownTimer dashTimer;
        CountdownTimer dashCooldownTimer;
        CountdownTimer shieldTimer;
        CountdownTimer shieldCooldownTimer;
        StateMachine stateMachine;

        protected static readonly int ShieldHash = Animator.StringToHash("Shield");
        protected static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        protected const float crossFadeDuration = 0.1f;
        private void Awake()
        {
            targetManager.AddTarget(gameObject);
            mainCam = Camera.main.transform;
            freeLookVCam.Follow = transform;
            freeLookVCam.LookAt = transform;
            freeLookVCam.OnTargetObjectWarped(transform, transform.position - freeLookVCam.transform.position - Vector3.forward);
            rb.freezeRotation = true;

            jumpTimer = new CountdownTimer(jumpDuration);
            jumpCooldownTimer = new CountdownTimer(jumpCooldown);

            jumpTimer.OnTimerStart += () => jumpVelocity = jumpForce;
            jumpTimer.OnTimerStop += () => jumpCooldownTimer.Start();

            dashTimer = new CountdownTimer(dashDuration);
            dashCooldownTimer = new CountdownTimer(dashCooldown);

            dashTimer.OnTimerStart += () => dashVelocity = dashForce;
            dashTimer.OnTimerStop += () =>
            {
                dashVelocity = 1.0f;
                dashCooldownTimer.Start();
            };

            shieldTimer = new CountdownTimer(shieldDuration);
            shieldCooldownTimer = new CountdownTimer(shieldCooldown);

            shieldTimer.OnTimerStart += () => hasShield = true;
            shieldTimer.OnTimerStop += () =>
            {
                hasShield = false;
                shieldCooldownTimer.Start(); animator.CrossFade(LocomotionHash, crossFadeDuration);
            };

            timers = new List<Utilities.Timer>(capacity: 6) { jumpTimer, jumpCooldownTimer, dashTimer, dashCooldownTimer, shieldTimer, shieldCooldownTimer };
            stateMachine = new StateMachine();

            var locomotionState = new LocomotionState(this, animator);
            var jumpState = new JumpState(this, animator);
            var dashState = new DashState(this, animator);

            At(locomotionState, jumpState, new FuncPredicate(() => jumpTimer.IsRunning));
            At(locomotionState, dashState, new FuncPredicate(() => dashTimer.IsRunning));

            Any(locomotionState, new FuncPredicate(() => groundChecker.IsGrounded && !jumpTimer.IsRunning && !dashTimer.IsRunning));
            stateMachine.SetState(locomotionState);
        }

        void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

        private void Start()
        {

            input.EnablePlayerActions();

        }
        void OnTriggerEnter(Collider other)
        {
            if (targetManager.LastTarget != gameObject) return;
            if (other.gameObject.tag != "Ball") return;
            if (!hasShield)
            {
                targetManager.RemoveTarget(gameObject);
                Destroy(gameObject);
            }
            targetManager.ChooseTarget();
        }
        private void OnEnable()
        {
            input.Jump += OnJump;
            input.Dash += OnDash;
            input.Shield += OnShield;
        }
        private void OnDisable()
        {
            input.Jump -= OnJump;
            input.Dash -= OnDash;
            input.Shield -= OnShield;
        }
        private void OnShield()
        {
            if (!shieldTimer.IsRunning && !shieldCooldownTimer.IsRunning)
            {
                animator.CrossFade(ShieldHash, crossFadeDuration);
                shieldTimer.Start();
            }
        }
        private void OnJump(bool performed)
        {
            if (performed && !jumpTimer.IsRunning && !jumpCooldownTimer.IsRunning && groundChecker.IsGrounded)
            {
                jumpTimer.Start();
            }
            else if (!performed && jumpTimer.IsRunning)
            {
                jumpTimer.Stop();
            }
        }
        private void OnDash(bool performed)
        {
            if (performed && !dashTimer.IsRunning && !dashCooldownTimer.IsRunning)
            {
                dashTimer.Start();
            }
            else if (!performed && jumpTimer.IsRunning)
            {
                dashTimer.Stop();
            }
        }
        void Update()
        {
            movement = new Vector3(input.Direction.x, 0f, input.Direction.y);
            stateMachine.Update();
            HandleTimer();
            UpdateAnimator();
        }
        void FixedUpdate()
        {
            stateMachine.FixUpdate();
        }
        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
        }
        private void HandleTimer()
        {
            foreach (var timer in timers)
            {
                timer.Tick(Time.deltaTime);
            }
        }
        public void HandleJump()
        {
            if (!jumpTimer.IsRunning && groundChecker.IsGrounded)
            {
                jumpVelocity = ZeroF;
                jumpTimer.Stop();
                return;
            }
            if (!jumpTimer.IsRunning)
            {
                jumpVelocity += Physics.gravity.y * gravityMultiplier * Time.fixedDeltaTime;
            }
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
        }
        public void HandleMovement()
        {
            Vector3 adjustedDirection = Quaternion.AngleAxis(mainCam.eulerAngles.y, Vector3.up) * movement;
            if (adjustedDirection.magnitude > ZeroF)
            {
                HandleRotation(adjustedDirection);
                HandleHorizantalMovement(adjustedDirection);
                SmoothSpeed(adjustedDirection.magnitude);
            }
            else
            {
                SmoothSpeed(ZeroF);
                rb.velocity = new Vector3(ZeroF, rb.velocity.y, ZeroF);
            }
        }
        private void SmoothSpeed(float value)
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, value, ref velocity, smoothTime);
        }
        private void HandleHorizantalMovement(Vector3 adjustedDirection)
        {
            Vector3 velocity = adjustedDirection * (moveSpeed * dashVelocity * Time.fixedDeltaTime);
            rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

        }
        private void HandleRotation(Vector3 adjustedDirection)
        {
            Quaternion targetRotation = Quaternion.LookRotation(adjustedDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            transform.LookAt(transform.position + adjustedDirection);
        }
    }
}

