using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DemoCharacterSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] Transform navSurfaceTransform;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float navMeshEdgeClearance = 8f;

    [Header("Spawn Animation")]
    [SerializeField] private float spawnDuration = 0.4f;
    [SerializeField]    private AnimationCurve spawnCurve =        AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private int startupSpawnAmount = 20;

    // [Header("Randomization")]
    // [SerializeField] private Gradient possibleBodyColors;
    // [SerializeField] private Vector2 sizeRange = new(0.85f, 1.15f);

    private void Awake()
    {
        for (int i = 0; i < startupSpawnAmount; i++)
        {
            SpawnCreature();
        }
    }

    [ContextMenu("Spawn Creature")]
    public void SpawnCreature(bool runtime = false)
    {
        if (!NavMeshUtility.TryGetRandomPosition(
                Vector3.zero,
                spawnRadius,
                navMeshEdgeClearance,
                out Vector3 position))
        {
            Debug.LogWarning("Couldn't find a valid spawn location.");
            return;
        }

        GameObject creatureRoot = Instantiate(creaturePrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        Creature creature = creatureRoot.GetComponentInChildren<Creature>(true);
        creature.GetComponent<NavigationAnimator>().navSurfaceTransform = navSurfaceTransform;

        if (creature == null)
        {
            Debug.LogError($"Prefab '{creaturePrefab.name}' does not contain a Creature component.");
            Destroy(creatureRoot);
            return;
        }

        RandomizeCreature(creature);

        InteractionManager.Instance.RegisterParticipant(creature.Identity, creature.Stats, creature.GetComponent<ActionDriver>());

        if (runtime) StartCoroutine(SpawnAnimation(creature.transform));
    }

    private void RandomizeCreature(Creature creature)
    {
        creature.Stats = ObjectRandomizer.RandomizeObject(creature.Stats);
        creature.Visuals = ObjectRandomizer.RandomizeObject(creature.Visuals);
    }

    private IEnumerator SpawnAnimation(Transform t)
    {
        Vector3 finalScale = t.localScale;

        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;

            float alpha = spawnCurve.Evaluate(elapsed / spawnDuration);

            t.localScale = finalScale * alpha;

            yield return null;
        }

        t.localScale = finalScale;
    }
}