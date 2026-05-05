using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DirectionalWalker : MonoBehaviour
{
    public float stepDistance = 3f;        // how far ahead we set the next target
    public float directionChangeRate = 0.2f; // how quickly direction drifts
    public float sampleRadius = 2f;

    private NavMeshAgent agent;
    private Vector3 currentDirection;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // initial random direction on tangent plane
        currentDirection = Random.onUnitSphere;
        currentDirection = Vector3.ProjectOnPlane(currentDirection, transform.position.normalized).normalized;
    }

    void Update()
    {
        // Slightly rotate direction over time (smooth wandering)
        Vector3 randomOffset = Random.insideUnitSphere * directionChangeRate;
        currentDirection = (currentDirection + randomOffset).normalized;

        // Keep direction tangent to sphere
        Vector3 normal = (transform.position - Vector3.zero).normalized;
        currentDirection = Vector3.ProjectOnPlane(currentDirection, normal).normalized;

        // Pick a point ahead
        Vector3 target = transform.position + currentDirection * stepDistance;

        // Snap to NavMesh
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}