using Platformer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Platformer
{
    public class NPCController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Animator animator;
        [SerializeField] Rigidbody rb;
        [SerializeField] Transform PatrolPath;
        [SerializeField] TargetManager targetManager;

        [Header("Movement Settings")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float rotationSpeed = 15f;
        [SerializeField] float smoothTime = 0.2f;
        [SerializeField] float distanceOffset = 3f;
        [SerializeField] float rotationSmoothness = 0.2f;
        float currentSpeed;
        float velocity;
        int currentIndex = 0;
        Transform currentWaypoint;
        List<Transform> waypoints = new List<Transform>();

        private void Awake()
        {
            targetManager.AddTarget(gameObject);
        }
        private void Start()
        {

            foreach (Transform t in PatrolPath)
            {
                waypoints.Add(t);
            }
            currentWaypoint = waypoints[currentIndex];


        }
        void OnTriggerEnter(Collider other)
        {
            if (targetManager.LastTarget != gameObject) return;
            if (other.gameObject.tag != "Ball") return;

            if (UnityEngine.Random.Range(0f, 1f) < .5)
            {
                targetManager.RemoveTarget(gameObject);
                Destroy(gameObject);
            }
            targetManager.ChooseTarget();

        }
        private void FixedUpdate()
        {
            UpdateAnimator();
            if (IsReachedTarget())
            {
                ChangeWayPoint();

            }
            HandleRotation();
            HandleHorizantalMovement();
        }

        private void HandleRotation()
        {
            var adjustedDirection = new Vector3(currentWaypoint.position.x, transform.position.y, currentWaypoint.position.z);

            var targetRotation = Quaternion.LookRotation((adjustedDirection - transform.position));
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSmoothness);

        }

        private void SmoothSpeed(float value)
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, value, ref velocity, smoothTime);
        }

        private void HandleHorizantalMovement()
        {
            Vector3 targetPos = new Vector3(currentWaypoint.position.x, transform.position.y, currentWaypoint.position.z);
            Vector3 directionToTarget = (targetPos - transform.position).normalized;

            rb.MovePosition(transform.position + (directionToTarget * moveSpeed * Time.deltaTime));

        }
        private void ChangeWayPoint()
        {
            currentIndex++;
            currentIndex %= waypoints.Count;
            currentWaypoint = waypoints[currentIndex];

        }

        private bool IsReachedTarget()
        {
            if (Vector3.Magnitude(transform.position - currentWaypoint.position) < distanceOffset)
            {
                return true;
            }
            return false;
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", moveSpeed);
        }
    }
}

