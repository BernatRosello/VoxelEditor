using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


public enum NavSurfaceMode
{
    FlatTransform,      // Uses a transform's up vector
    SphereTransform,    // Uses a transform as the sphere center
    RaycastSurface,      // Uses physics geometry
    WorldUp
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

    private NavMeshAgent agent;
    private Animator animator;
    private Transform animatedTransform;
    [SerializeField] private FloatThresholds movementThreshold = new(0.5f, 0.3f);
    public float MoveStartThreshold => movementThreshold.Start;
    public float MoveStopThreshold => movementThreshold.Stop;
    [SerializeField] private FloatThresholds turningThreshold = new(30f, 5f);
    public float TurnStartThreshold => turningThreshold.Start;
    public float TurnStopThreshold => turningThreshold.Stop;
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
    public NavigationAnimatorSettings navSettings;
    [SerializeField] public Transform navSurfaceTransform;

    private float currentMaxSpeed;


    private Vector3 surfaceUp = Vector3.up;

    private float cachedPathLength;
    private bool pathCachedFlag = true;
    private Vector3 smoothedSteeringTarget;
    private Vector3 previousPosition;
    private Vector3 actualVelocity;
    private bool navigationActive;
    private bool turnRequestActive;
    private Vector3 requestedFacingDirection;

    public bool NavigationActive { get => navigationActive; set => navigationActive = value; }


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
        navigationActive = true;
        currentMaxSpeed = maximumSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        SetCurrentUp();

        if (HandleOffMeshLink())
            return;

        ProcessMovement();
    }

    void LateUpdate()
    {
        actualVelocity = (animatedTransform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        previousPosition = animatedTransform.position;
    }

    public void SetCurrentUp()
    {
        switch (navSettings.NavSurfMode)
        {
            case NavSurfaceMode.RaycastSurface:
                // NOTE: NavMesh API sucks and there is currently NO WAY to get the normal of the NavMeshSurface at any given position.
                //      To work around this we can use a raycast, but it should be filtered only to include the collection of meshes that
                //      were used to bake the NavMesh in the first place (but even this is not a perfect solution since baking can severely
                //      change the Surface when comparing it to the base mesh).
                if (Physics.Raycast(animatedTransform.position, -animatedTransform.up, out RaycastHit hit,
                    navSettings.SurfaceRayDistance,
                    navSettings.SurfaceMask))
                {
                    surfaceUp = hit.normal;
                }
                break;
            case NavSurfaceMode.SphereTransform:
                surfaceUp = (animatedTransform.position - navSurfaceTransform.position).normalized;
                break;
            case NavSurfaceMode.FlatTransform:
                surfaceUp = navSurfaceTransform.up;
                break;
            case NavSurfaceMode.WorldUp:
                surfaceUp = Vector3.up;
                break;
        }
    }

    public float GetRemainingDistance()
    {
        return GetRemainingDistance(agent);
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

    public void FaceDirection(Vector3 direction)
    {
        direction = Vector3.ProjectOnPlane(direction, surfaceUp);

        if (direction.sqrMagnitude < 0.0001f)
            return;

        turnRequestActive = true;
        requestedFacingDirection = direction.normalized;

        // Stop any locomotion cleanly.
        agent.ResetPath();

        animator.SetBool("IsMoving", false);
        animator.SetFloat("vel_x", 0f);
        animator.SetFloat("vel_y", 0f);

        pathCachedFlag = false;
    }

    public bool IsFacingDirection(Vector3 direction, float? tolerance = null)
    {
        direction = Vector3.ProjectOnPlane(direction, surfaceUp);

        if (direction.sqrMagnitude < 0.0001f)
            return true;

        float angle = Vector3.Angle(
            animatedTransform.forward,
            direction.normalized);

        Debug.Log(angle);

        tolerance ??= turningThreshold.Stop;

        return angle <= tolerance.Value;
    }

    private bool ShouldStartTurning(Vector3 direction)
    {
        direction = Vector3.ProjectOnPlane(direction, surfaceUp);

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        return Vector3.Angle(animatedTransform.forward, direction.normalized) > turningThreshold.Start;
    }

    private void ProcessMovement()
    {
        if (!navigationActive) return;

        if (turnRequestActive)
        {
            ProcessTurnRequest();
            return;
        }

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
        Vector3 toTarget = Vector3.ProjectOnPlane(smoothedSteeringTarget - animatedTransform.position, surfaceUp);

        if (toTarget.sqrMagnitude < 0.001f)
            return;

        Vector3 desiredForward = toTarget.normalized;

        float angleToTarget = Vector3.SignedAngle(animatedTransform.forward, desiredForward, surfaceUp);
        bool facingTarget = IsFacingDirection(desiredForward);

        bool isMoving = animator.GetBool("IsMoving");
        bool isTurning = animator.GetBool("IsTurning");

        //
        // PRE-MOVEMENT PLANNING
        //
        if (!isMoving && !isTurning)
        {
            // Debug.Log($"Remaining Distance: {remainingDistance}/{movementThreshold.Start}");
            if (ShouldStartTurning(desiredForward))
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
            // Debug.Log(
            //     $"Angle={angleToTarget:F1}  " +
            //     $"Desired={desiredForward}  " +
            //     $"Forward={animatedTransform.forward}");

            float velAng = Mathf.Clamp(angleToTarget / 180f, -1f, 1f);
            animator.SetFloat("vel_ang", velAng, 0.1f, Time.deltaTime);
            if (facingTarget)
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

            //
            // ORIENTATION
            //
            // Keep orientation driven exclusively by steering target.
            //
            Vector3 steeringLocal = animatedTransform.InverseTransformDirection(desiredForward).normalized;
            //
            // MOVEMENT
            //
            // Use desiredVelocity so avoidance influences locomotion.
            //
            Vector3 desiredVelocity = Vector3.ProjectOnPlane(agent.desiredVelocity, surfaceUp);

            Vector3 localVelocity;

            if (desiredVelocity.sqrMagnitude > 0.001f)
            {
                localVelocity = animatedTransform.InverseTransformDirection(desiredVelocity.normalized);
            }
            else
            {
                // Fallback when the agent reports almost no velocity.
                localVelocity = steeringLocal;
            }

            float desiredSpeed = GetSpeedAlongPath(remainingDistance, cachedPathLength);
            desiredSpeed = Mathf.Clamp(desiredSpeed, -maximumSpeed, maximumSpeed);

            animator.SetFloat("vel_x", localVelocity.x * desiredSpeed, 0.1f, Time.deltaTime);

            animator.SetFloat("vel_y", localVelocity.z * desiredSpeed, 0.1f, Time.deltaTime);

            if (desiredSpeed < 1f)
            {
                animator.SetFloat("LocomotionSpeedParam", Mathf.Lerp(0.65f, 1.0f, desiredSpeed));
            }
            else if (desiredSpeed > 5f)
            {
                animator.SetFloat("LocomotionSpeedParam", desiredSpeed / 5f);
            }

            //
            // TURN-IN-PLACE DECISION
            //
            // IMPORTANT:
            // Still use steering target angle here,
            // NOT desiredVelocity.
            //
            if (remainingDistance > 0.15f &&
                Mathf.Abs(angleToTarget) > turnWhileMovingThreshold)
            {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsTurning", true);

                animator.SetFloat("vel_x", 0f, 0.25f, Time.deltaTime);

                animator.SetFloat("vel_y", 0f, 0.25f, Time.deltaTime);
                cachedPathLength = remainingDistance;
            }
        }
    }

    private void ProcessTurnRequest()
    {
        float angle =
            Vector3.SignedAngle(
                animatedTransform.forward,
                requestedFacingDirection,
                surfaceUp);

        if (!animator.GetBool("IsTurning"))
        {
            animator.SetBool("IsTurning", true);
        }

        animator.SetFloat(
            "vel_ang",
            Mathf.Clamp(angle / 180f, -1f, 1f),
            0.1f,
            Time.deltaTime);

        if (IsFacingDirection(requestedFacingDirection))
        {
            animator.SetBool("IsTurning", false);
            animator.SetFloat("vel_ang", 0f);

            turnRequestActive = false;
        }
    }

    private bool HandleOffMeshLink()
    {
        if (!agent.isOnOffMeshLink)
            return false;

        OffMeshLinkData linkData = agent.currentOffMeshLinkData;

        int seamAreaId = NavMesh.GetAreaFromName(navSettings.PlanetSeamArea);
        int climbAreaId = NavMesh.GetAreaFromName(navSettings.ClimbArea);

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
        agent.velocity = actualVelocity;

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

            Debug.DrawRay(animatedTransform.position, requestedFacingDirection, Color.green);
            Debug.DrawRay(animatedTransform.position, animatedTransform.forward, Color.blue);
        }
    }

    internal void SetMaxSpeed(float speed) { currentMaxSpeed = speed; }
    internal void ResetMaxSpeed() { currentMaxSpeed = maximumSpeed; }

    internal void SetDestination(Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    internal Vector3 GetPosition()
    {
        return agent.nextPosition;
    }

    internal bool HasDestination()
    {
        return agent.hasPath;
    }
}
