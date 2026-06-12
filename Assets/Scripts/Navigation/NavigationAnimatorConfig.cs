using UnityEngine;


[CreateAssetMenu(
    fileName = "NavigationAnimatorSettings",
    menuName = "Navigation/Navigation Animator Settings")]
public class NavigationAnimatorSettings : ScriptableObject
{
    public NavSurfaceMode NavSurfMode;

    public LayerMask SurfaceMask;

    public float SurfaceRayDistance = 100f;

    [NavMeshArea] public string ClimbArea;

    [NavMeshArea] public string PlanetSeamArea;
}