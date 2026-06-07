using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;

public class NavigationAnimator : MonoBehaviour
{
    [System.Serializable]
    public struct FloatThresholds
    {
        [SerializeField] private float startThreshold;
        [SerializeField] private float stopThreshold;

        public FloatThresholds(float start, float stop)
        {
            startThreshold = start;
            stopThreshold = stop;
        }

        public float Start => startThreshold;
        public float Stop => stopThreshold;
    }
    private NavMeshAgent agent;
    private Animator animator;
    private Transform animatedTransform;
    [SerializeField] private float rotationSmoothDegrees = 90f;
    [SerializeField] private FloatThresholds movementThreshold = new(0.5f, 0.3f);
    [SerializeField] private FloatThresholds turningThreshold = new(30f, 5f);
    [SerializeField] private float turnWhileMovingThreshold = 91f;
    [SerializeField] private float minimumSpeed = 0.25f;
    [SerializeField] private float maximumSpeed = 5.0f;
    [InspectorWide]
    [SerializeField] private float shortPathLength = 2f;
    [SerializeField] private AnimationCurve shortPathSpeedCurve;
    [SerializeField] private float mediumPathLength = 8f;
    [SerializeField] private AnimationCurve mediumPathSpeedCurve;
    [SerializeField] private float longPathLength = 20f;
    [SerializeField] private AnimationCurve longPathSpeedCurve;
    private float cachedPathLength;
    private bool pathCachedFlag = true;
    private Vector3 smoothedSteeringTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        agent = GetComponentInParent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animatedTransform = animator.transform;

        animator.applyRootMotion = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        animator.SetBool("IsMoving", false);
        animator.SetBool("IsTurning", false);
        animator.SetFloat("vel_x", 0);
        animator.SetFloat("vel_y", 0);
        animator.SetFloat("vel_ang", 0);
        animator.SetFloat("LocomotionSpeedParam", 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        ProcessMovement();
    }

    // To handle unexpected infinite values given out by the NavMesh plugin. As referenced by https://stackoverflow.com/a/67561314
    public static float GetRemainingDistance(NavMeshAgent agent)
    {
        if (!Single.IsInfinity(agent.remainingDistance))
        {
            return agent.remainingDistance;
        }

        float distance = 0;
        Vector3[] corners = agent.path.corners;

        if (corners.Length > 2)
        {
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 previous = new Vector2(corners[i - 1].x, corners[i - 1].z);
                Vector2 current = new Vector2(corners[i].x, corners[i].z);

                distance += Vector2.Distance(previous, current);
            }
        }
        else
        {
            distance = 0;
            Debug.LogError("CAN'T FIND THE LENGTH OF THE REMAINING PATH OF THE AGENT!");
        }

        return distance;
    }

    private float GetSpeedAlongPath(float remainingDistance, float totalPathDistance)
    {
        if (totalPathDistance <= 0.01f)
            return minimumSpeed;

        float progress = 1f - Mathf.Clamp01(remainingDistance / totalPathDistance);

        float normalizedSpeed;

        string profileInfo;

        if (totalPathDistance <= mediumPathLength)
        {
            float t = Mathf.InverseLerp(shortPathLength, mediumPathLength, totalPathDistance);

            float shortValue = shortPathSpeedCurve.Evaluate(progress);

            float mediumValue =
                mediumPathSpeedCurve.Evaluate(progress);

            normalizedSpeed =
                Mathf.Lerp(
                    shortValue,
                    mediumValue,
                    t);

            profileInfo =
                $"SHORT→MEDIUM blend={t:F2} short={shortValue:F2} medium={mediumValue:F2}";
        }
        else
        {
            float t =
                Mathf.InverseLerp(
                    mediumPathLength,
                    longPathLength,
                    totalPathDistance);

            float mediumValue =
                mediumPathSpeedCurve.Evaluate(progress);

            float longValue =
                longPathSpeedCurve.Evaluate(progress);

            normalizedSpeed = Mathf.Lerp(mediumValue, longValue, t);

            profileInfo =
                $"MEDIUM→LONG blend={t:F2} medium={mediumValue:F2} long={longValue:F2}";
        }

        float finalSpeed =
            Mathf.Lerp(
                minimumSpeed,
                maximumSpeed,
                normalizedSpeed);

        // Debug.Log(
        //     $"[PathSpeed] " +
        //     $"remaining={remainingDistance:F2}m " +
        //     $"total={totalPathDistance:F2}m " +
        //     $"progress={progress:P0} " +
        //     $"{profileInfo} " +
        //     $"normalized={normalizedSpeed:F2} " +
        //     $"speed={finalSpeed:F2}");

        return finalSpeed;
    }

    private void ProcessMovement()
    {
        if (!agent.hasPath)
        {
            pathCachedFlag = false;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsTurning", false);
            return;
        }

        if (!pathCachedFlag &&
            !agent.pathPending)
        {
            cachedPathLength = GetRemainingDistance(agent);
            pathCachedFlag = true;
        }

        float remainingDistance = GetRemainingDistance(agent);
        smoothedSteeringTarget = agent.steeringTarget;
        Vector3 toTarget = smoothedSteeringTarget - animatedTransform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
            return;

        Vector3 desiredForward = toTarget.normalized;

        float angleToTarget = Vector3.SignedAngle(animatedTransform.forward, desiredForward, Vector3.up);

        float absAngle = Mathf.Abs(angleToTarget);

        bool isMoving = animator.GetBool("IsMoving");
        bool isTurning = animator.GetBool("IsTurning");

        //
        // PRE-MOVEMENT PLANNING
        //
        if (!isMoving && !isTurning)
        {
            if (absAngle > turningThreshold.Start)
            {
                animator.SetBool("IsTurning", true);
                animator.SetFloat("vel_ang", Mathf.Clamp(angleToTarget / 180f, -1f, 1f));
            }
            else if (remainingDistance > movementThreshold.Start)
            {
                animator.SetBool("IsMoving", true);
            }
            else
            {
                animator.SetFloat("vel_x", 0f, 0.2f, Time.deltaTime);
                animator.SetFloat("vel_y", 0f, 0.2f, Time.deltaTime);
                agent.ResetPath();
            }

            return;
        }

        //
        // TURNING
        //
        if (isTurning)
        {
            float velAng = Mathf.Clamp(angleToTarget / 180f, -1f, 1f);
            animator.SetFloat("vel_ang", velAng, 0.1f, Time.deltaTime);

            if (absAngle < turningThreshold.Stop)
            {
                animator.SetBool("IsTurning", false);
                animator.SetFloat("vel_ang", 0f);
            }

            return;
        }

        //
        // MOVING
        //
        if (isMoving)
        {
            if (remainingDistance < (movementThreshold.Stop * animator.GetFloat("LocomotionSpeedParam")))
            {
                //animator.SetBool("IsMoving", false);
                animator.SetFloat("vel_x", 0f, 0.1f, Time.deltaTime);
                animator.SetFloat("vel_y", 0f, 0.1f, Time.deltaTime);
                agent.ResetPath();
                return;
            }

            // Compensate for on-the-fly extended paths
            if (remainingDistance > cachedPathLength)
            {
                cachedPathLength = remainingDistance;
            }

            // Debug.Log(
            //     $"hasPath={agent.hasPath} " +
            //     $"pending={agent.pathPending} " +
            //     $"NavMeshAgent remaining={agent.remainingDistance} " +
            //     $"Patched remaining={remainingDistance} " +
            //     $"status={agent.pathStatus}");

            Vector3 localTarget = animatedTransform.InverseTransformDirection(desiredForward).normalized;
            float desiredSpeed = GetSpeedAlongPath(remainingDistance, cachedPathLength);
            // float movementSpeed = desiredSpeed * animator.GetFloat("CalmEnergetic");

            animator.SetFloat("vel_x", localTarget.x * desiredSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat("vel_y", localTarget.z * desiredSpeed, 0.1f, Time.deltaTime);
            if (desiredSpeed < 1)
            {
                animator.SetFloat("LocomotionSpeedParam", Mathf.Lerp(0.65f, 1.0f, desiredSpeed));
            }
            else if (desiredSpeed > 5)
            {
                animator.SetFloat("LocomotionSpeedParam", desiredSpeed/5);
            }

            if (remainingDistance > 0.15f && absAngle > turnWhileMovingThreshold)
            {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsTurning", true);

                animator.SetFloat("vel_x", 0f, 0.25f, Time.deltaTime);
                animator.SetFloat("vel_y", 0f, 0.25f, Time.deltaTime);

                // refresh path length to ensure smooth velocity transitions
                cachedPathLength = remainingDistance;
            }
        }
    }

    private void OnAnimatorMove()
    {
        animatedTransform.position = animator.rootPosition;
        agent.nextPosition = animator.rootPosition;
        // agent.Warp(transform.position);

        if (animator.GetBool("IsTurning"))
        {
            animatedTransform.rotation = animator.rootRotation;
        }
        else if (animator.GetBool("IsMoving"))
        {
            animatedTransform.rotation = Quaternion.RotateTowards(
                animatedTransform.rotation,
                Quaternion.LookRotation((smoothedSteeringTarget - animatedTransform.position).normalized),
                rotationSmoothDegrees * Time.deltaTime);
        }


    }

    private void OnDrawGizmos()
    {
        if (animator)
        {
            Gizmos.color = Color.blue;
            Vector3 toTarget = smoothedSteeringTarget - animator.rootPosition;
            Gizmos.DrawLine(animator.rootPosition, animator.rootPosition + toTarget);
        }
    }
}
