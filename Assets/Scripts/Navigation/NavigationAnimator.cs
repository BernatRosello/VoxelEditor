using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavigationAnimator : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Transform animatedTransform;
    private Vector2 velocity;
    private Vector2 smoothDeltaPosition;
    [SerializeField] private float rotationSmoothTime = 10f;
    [SerializeField] private float movementThreshold = 0.3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        agent = GetComponentInParent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animatedTransform = animator.transform;

        animator.applyRootMotion = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    // Update is called once per frame
    void Update()
    {
        SyncAnimatorAndAgent();
    }

    private void SyncAnimatorAndAgent()
    {
        Vector3 worldDeltaPosition = agent.nextPosition - animatedTransform.position;
        worldDeltaPosition.y = 0;

        float dx = Vector3.Dot(animatedTransform.right, worldDeltaPosition);
        float dy = Vector3.Dot(animatedTransform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        float smooth = Mathf.Min(1, Time.deltaTime / 0.1f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        velocity = smoothDeltaPosition / Time.deltaTime;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            velocity = Vector2.Lerp(
                Vector2.zero,
                velocity,
                agent.remainingDistance / agent.stoppingDistance
            );
        }

        bool shouldMove =
            velocity.magnitude >= movementThreshold &&
            agent.remainingDistance > agent.stoppingDistance;

        animator.SetBool("IsMoving", shouldMove);
        animator.SetFloat("locomotion", velocity.magnitude);

        float deltaMagnitude = worldDeltaPosition.magnitude;

        if (deltaMagnitude > agent.radius / 2f)
        {
            animatedTransform.position =
                Vector3.Lerp(animator.rootPosition, agent.nextPosition, smooth);
        }

        // Smooth rotation
        Vector3 desiredDirection = agent.desiredVelocity;

        desiredDirection.y = 0;

        if (desiredDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(desiredDirection);

            animatedTransform.rotation = Quaternion.Slerp(
                animatedTransform.rotation,
                targetRotation,
                rotationSmoothTime * Time.deltaTime
            );
        }
    }

    private void OnAnimatorMove()
    {
        Vector3 rootPosition = animator.rootPosition;
        rootPosition.y = agent.nextPosition.y;

        animatedTransform.position = rootPosition;
        agent.nextPosition = rootPosition;
    }

    private void OnDrawGizmos()
    {
        if (animator)
            Gizmos.DrawLine(animator.rootPosition, animator.rootPosition + new Vector3(velocity.x, velocity.y, 0));
    }
}
