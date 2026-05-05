using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SphereOrientationAndLinkAdapter : MonoBehaviour
{
    public float rotationSharpness = 12f;

    private NavMeshAgent agent;
    private Transform navSphere;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Prevent teleporting across links
        agent.autoTraverseOffMeshLink = false;

        // Find sphere
        navSphere = GameObject.Find("NavSphere").transform;
    }

    void Start()
    {
        StartCoroutine(HandleLinks());
    }

    void Update()
    {
        AlignToSphere();
    }

    void AlignToSphere()
    {
        if (navSphere == null) return;

        Vector3 normal = (transform.position - navSphere.position).normalized;

        Vector3 velocity = agent.velocity;
        Vector3 forward;

        if (velocity.sqrMagnitude > 0.001f)
        {
            // Use movement direction projected onto surface
            forward = Vector3.ProjectOnPlane(velocity, normal).normalized;
        }
        else
        {
            // Keep current forward but reproject it
            forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        }

        transform.rotation.SetLookRotation(forward, normal);
        // Quaternion targetRot = Quaternion.LookRotation(forward, normal);
        // transform.rotation = Quaternion.Slerp(
        //     transform.rotation,
        //     targetRot,
        //     rotationSharpness * Time.deltaTime
        // );
    }

    IEnumerator HandleLinks()
    {
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {
                yield return TraverseLink(agent.currentOffMeshLinkData);
                agent.CompleteOffMeshLink();
            }

            yield return null;
        }
    }

    IEnumerator TraverseLink(OffMeshLinkData data)
    {
        Vector3 start = transform.position;
        Vector3 end = data.endPos;

        float length = Vector3.Distance(start, end);
        float t = 0f;

        while (t < 1f)
        {
            float dt = Time.deltaTime;

            // Advance using agent speed
            t += (agent.speed / length) * dt;
            t = Mathf.Clamp01(t);

            // Let navmesh keep control of position → we just gently guide it
            Vector3 pos = Vector3.Lerp(start, end, t);
            transform.position = pos;

            yield return null;
        }

        transform.position = end;
    }
}