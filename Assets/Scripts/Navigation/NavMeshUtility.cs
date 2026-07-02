
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshUtility
{
    /// <summary>
    /// Attempts to find a random position on the NavMesh.
    /// </summary>
    /// <param name="center">Center of the search area.</param>
    /// <param name="radius">Search radius.</param>
    /// <param name="edgeClearance">Minimum distance to the nearest NavMesh edge.</param>
    /// <param name="result">The resulting position.</param>
    /// <param name="areaMask">NavMesh areas to sample.</param>
    /// <param name="maxAttempts">Maximum number of random samples.</param>
    /// <returns>True if a suitable position was found.</returns>
    public static bool TryGetRandomPosition(
        Vector3 center,
        float radius,
        float edgeClearance,
        out Vector3 result,
        int areaMask = NavMesh.AllAreas,
        int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(random2D.x, 0f, random2D.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, areaMask))
                continue;

            if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, areaMask))
                continue;

            if (edgeHit.distance < edgeClearance)
                continue;

            result = hit.position;
            return true;
        }

        result = default;
        return false;
    }
}