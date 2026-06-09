using System;
using System.Numerics;
using System.Runtime.Serialization;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;


public enum NavSurfaceMode
{
    FlatTransform,      // Uses a transform's up vector
    SphereTransform,    // Uses a transform as the sphere center
    RaycastSurface      // Uses physics geometry
}

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

    public static class NavAreas
    {
        public static readonly int Walkable =
            NavMesh.GetAreaFromName("Walkable");

        public static readonly int PlanetSeam =
            NavMesh.GetAreaFromName("PlanetSeam");

        public static readonly int ClimbLink =
            NavMesh.GetAreaFromName("ClimbLink");

        public static readonly int LadderLink =
            NavMesh.GetAreaFromName("LadderLink");
    }

    private NavMeshAgent agent;
    private Animator animator;
    private Transform animatedTransform;
    [SerializeField] private FloatThresholds movementThreshold = new(0.5f, 0.3f);
    [SerializeField] private FloatThresholds turningThreshold = new(30f, 5f);
    [Space(10)]
    [SerializeField] private float turnWhileMovingThreshold = 91f;
    [SerializeField] private float rotationSmoothDegrees = 90f;
    [Space(10)]
    [SerializeField] private float minimumSpeed = 0.25f;
    [SerializeField] private float maximumSpeed = 5.0f;
    [InspectorWide]
    [SerializeField] private float shortPathLength = 2f;
    [SerializeField] private AnimationCurve shortPathSpeedCurve;
    [SerializeField] private float mediumPathLength = 8f;
    [SerializeField] private AnimationCurve mediumPathSpeedCurve;
    [SerializeField] private float longPathLength = 20f;
    [SerializeField] private AnimationCurve longPathSpeedCurve;
    [Space(10)]

    [Header("Navigation Surface Configuration")]
    [SerializeField] private static NavSurfaceMode navSurfMode; // Common 
    [SerializeField] private static Transform navSurfTransform; // For Flat & for Sphere modes
    [SerializeField] private static LayerMask surfaceMask = ~0; // For Raycast Mode
    [SerializeField] private static float surfaceRayDistance = 5f; // For Raycast Mode
    [Header("Off Mesh Links")]
    [SerializeField, NavMeshArea] private string seamArea = "SurfaceSeams";
    [SerializeField, NavMeshArea] private string climbArea = "Climb";

    private Vector3 surfaceUp = Vector3.up;

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
        agent.autoTraverseOffMeshLink = false;

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
        SetCurrentUp();

        if (HandleOffMeshLink())
            return;

        ProcessMovement();
    }

    public void SetCurrentUp()
    {
        switch (navSurfMode)
        {
            case RaycastSurface:
                // NOTE: NavMesh API sucks and there is currently NO WAY to get the normal of the NavMeshSurface at any given position.
                //      To work around this we can use a raycast, but it should be filtered only to include the collection of meshes that
                //      were used to bake the NavMesh in the first place (but even this is not a perfect solution since baking can severely
                //      change the Surface when comparing it to the base mesh).
                if (Physics.Raycast(animatedTransform.position, -animatedTransform.up, out RaycastHit hit, surfaceRayDistance, surfaceMask))
                {
                    surfaceUp = hit.normal;
                }
                break;
            case SphereTransform:
                surfaceUp = (animatedTransform.position - navSurfTransform.position).normalized;
                break;
            case FlatTransform:
                surfaceUp = navSurfTransform.up;
                break;
        }
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
        Vector3 toTarget = Vector3.ProjectOnPlane(steeringTarget - transform.position, surfaceUp);

        if (toTarget.sqrMagnitude < 0.001f)
            return;

        Vector3 desiredForward = toTarget.normalized;

        float angleToTarget = Vector3.SignedAngle(animatedTransform.forward, desiredForward, surfaceUp);

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
                animator.SetFloat("LocomotionSpeedParam", desiredSpeed / 5);
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

    private bool HandleOffMeshLink()
    {
        if (!agent.isOnOffMeshLink)
            return false;

        OffMeshLinkData linkData = agent.currentOffMeshLinkData;

        int seamAreaId = NavMesh.GetAreaFromName(seamArea);
        int climbAreaId = NavMesh.GetAreaFromName(climbArea);

        switch (linkData.offMeshLink.area)
        {
            case var area when area == seamAreaId:

                Debug.Log($"Reached seam OffMeshLink '{linkData.offMeshLink.name}'.");
                break;

            case var area when area == climbAreaId:

                Debug.Log($"Reached climb OffMeshLink '{linkData.offMeshLink.name}'.");
                // TODO:
                // Play climb traversal animation.
                animator.SetBool("IsMoving", false);
                animator.SetTrigger("ClimbTrigger");
                break;

            default:

                Debug.LogWarning($"Reached OffMeshLink '{linkData.offMeshLink.name}' " + $"with unhandled area {linkData.offMeshLink.area}.");
                break;
        }

        // Perhaps consider checking finalization condition for off mesh link traversal and then calling 
        //          agent.CompleteOffMeshLink();

        return true;
    }

    private void OnAnimatorMove()
    {
        animatedTransform.position = animator.rootPosition;
        agent.nextPosition = animator.rootPosition;

        if (animator.GetBool("IsTurning"))
        {   // Root Motion Rotation
            animatedTransform.rotation = Quaternion.FromToRotation(animatedTransform.up, surfaceUp) * animatedTransform.rotation * animator.deltaRotation;
        }
        else if (animator.GetBool("IsMoving"))
        {   // Procedurally controlled Rotation
            var steeringDelta = smoothedSteeringTarget - animatedTransform.position;
            // NOTE: we might have to project the steering target down to the tangent surfaceUp plane ( Vector3.ProjectOnPlane(..., surfaceUp); )
            animatedTransform.rotation = Quaternion.RotateTowards(
                animatedTransform.rotation,
                Quaternion.LookRotation(steeringDelta.normalized, surfaceUp),
                steeringDelta.sqrMagnitude * rotationSmoothDegrees * Time.deltaTime);
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
