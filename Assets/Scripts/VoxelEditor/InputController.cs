using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Actions;
using UnityEngine.AI;

public class InputController : MonoBehaviour
{
    public Camera cam;
    public GridRenderer gridRenderer;
    // Vector3Int? startVoxel = null;
    // Vector3Int? depthVoxel = null;
    // Vector3Int? lastVoxel = null;

    // enum DragAxis { None, X, Y, Z }
    // DragAxis activeAxis = DragAxis.None;
    float holdTimer = 0f;
    public float holdThreshold = 0.5f; // tweak (0.1–0.2 feels good)
    public float heldRate = 0.2f;
    public float moveThreshold = 1f;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            //BeginDrag(mouse.position.ReadValue());
            if (!TryGetVoxel(mouse.position.ReadValue(), out var voxel)) return;
            ApplyTool(voxel);
        }
        else if (mouse.leftButton.isPressed)
        {
            bool shouldModify = false;
            //ContinueDrag(mouse.position.ReadValue());
            if (mouse.delta.magnitude > moveThreshold) // si te mueves que se haga inmediatamente la modificación
                shouldModify = true;

            holdTimer += Time.deltaTime;
            if (holdTimer >= holdThreshold)
                if (holdTimer > (heldRate + holdThreshold))
                {
                    shouldModify = true;
                    holdTimer = holdThreshold;
                }
                
            if (shouldModify)
            {
                if (!TryGetVoxel(mouse.position.ReadValue(), out var voxel)) return;
                ApplyTool(voxel);
            }
                
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            //EndDrag();
            holdTimer = 0;
        }
    }

    // void BeginDrag(Vector2 screenPos)
    // {
    //     if (TryGetVoxel(screenPos, out var voxel))
    //     {
    //         startVoxel = voxel;
    //         lastVoxel = voxel;
    //         activeAxis = DragAxis.None;

    //         ApplyTool(voxel.x, voxel.y, voxel.z);
    //     }
    // }

    // void ContinueDrag(Vector2 screenPos)
    // {
    //     if (startVoxel == null) return;
    //     if (!TryGetVoxel(screenPos, out var voxel)) return;
    //     if (depthVoxel == null)
    //     {
    //         holdTimer = 0;
    //         depthVoxel = voxel;
    //         // Debug.Log($"start Voxel = {startVoxel.Value}\t depthVoxel = {depthVoxel.Value}\t lastVoxel = {lastVoxel.Value}");
    //     }
    //     // Debug.Log($"Current Voxel = {voxel} Current Axis = {GetAxis(voxel, startVoxel.Value)}");

    //     // Step 1: Determine axis if not set
    //     if (activeAxis == DragAxis.None)
    //     {
    //         if (voxel == depthVoxel.Value)
    //         {
    //             holdTimer += Time.deltaTime;
    //         }
            
    //         if (holdTimer > holdThreshold || voxel != depthVoxel.Value)
    //         {
    //             if (voxel != depthVoxel.Value && GetAxis(voxel, startVoxel.Value) == GetAxis(depthVoxel.Value, startVoxel.Value))
    //             {
    //                 var dragVector = voxel - startVoxel.Value;
    //                 int startDepthCoord, depthVoxelCoord, dragDepthCoord;
    //                 switch (GetAxis(depthVoxel.Value, startVoxel.Value))
    //                 {
    //                     case DragAxis.X:
    //                         startDepthCoord = startVoxel.Value.x;
    //                         depthVoxelCoord = depthVoxel.Value.x;
    //                         dragDepthCoord = dragVector.x;
    //                         break;
    //                     case DragAxis.Y:
    //                         startDepthCoord = startVoxel.Value.y;
    //                         depthVoxelCoord = depthVoxel.Value.y;
    //                         dragDepthCoord = dragVector.y;
    //                         break;
    //                     case DragAxis.Z:
    //                         startDepthCoord = startVoxel.Value.z;
    //                         depthVoxelCoord = depthVoxel.Value.z;
    //                         dragDepthCoord = dragVector.z;
    //                         break;
    //                     default:
    //                         startDepthCoord = 0;
    //                         depthVoxelCoord = 0;
    //                         dragDepthCoord = 0;
    //                         break;
    //                 }
    //                 if (startDepthCoord < dragDepthCoord)
    //                 {
    //                     if (startDepthCoord < depthVoxelCoord && depthVoxelCoord < dragDepthCoord)
    //                         ApplyTool(depthVoxel.Value);
    //                 } else
    //                 {
    //                     if (dragDepthCoord < depthVoxelCoord && depthVoxelCoord < startDepthCoord)
    //                         ApplyTool(depthVoxel.Value);
    //                 }

    //             }
                
    //             var axis = GetAxis(voxel, startVoxel.Value);
    //             if (axis == DragAxis.None)
    //                 return; // ignore until valid axis chosen
    //             activeAxis = axis;
    //         }
    //     }

    //     // Step 2: Only allow voxels ON the axis
    //     if (!IsOnAxis(startVoxel.Value, voxel, activeAxis))
    //         return;

    //     ApplyTool(voxel);

    //     lastVoxel = voxel;
    // }

    // DragAxis GetAxis(Vector3Int voxelA, Vector3Int voxelB)
    // {
    //     Vector3Int delta = voxelA - voxelB;
    //     int nonZeroAxes =
    //     (delta.x != 0 ? 1 : 0) +
    //     (delta.y != 0 ? 1 : 0) +
    //     (delta.z != 0 ? 1 : 0);

    //     // Only accept perfectly aligned second voxel
    //     if (nonZeroAxes == 1)
    //     {
    //         if (delta.x != 0) return DragAxis.X;
    //         else if (delta.y != 0) return DragAxis.Y;
    //         else if (delta.z != 0) return DragAxis.Z;
    //     }

    //     return DragAxis.None;

    // }

    // bool IsOnAxis(Vector3Int start, Vector3Int v, DragAxis axis)
    // {
    //     return axis switch
    //     {
    //         DragAxis.X => v.y == start.y && v.z == start.z,
    //         DragAxis.Y => v.x == start.x && v.z == start.z,
    //         DragAxis.Z => v.x == start.x && v.y == start.y,
    //         _ => false
    //     };
    // }

    // void EndDrag()
    // {
    //     startVoxel = null;
    //     depthVoxel = null;
    //     lastVoxel = null;
    //     activeAxis = DragAxis.None;
    // }



    // void HandleTap(Vector2 screenPos)
    // {
    //     Ray ray = cam.ScreenPointToRay(screenPos);
    //     bool isDeleting = EditorState.Instance.activeTool == VoxelTool.Delete;

    //     var grid = FindAnyObjectByType<GridRenderer>();
    //     if (Physics.Raycast(ray, out RaycastHit hit))
    //     {
    //         Vector3 point = hit.point;
    //         Vector3 normal = hit.normal;

    //         Vector3 adjusted;

    //         if (isDeleting)
    //         {
    //             // Inside the voxel
    //             adjusted = point - normal * 0.5f;
    //         }
    //         else
    //         {
    //             adjusted = point + normal * 0.5f;
    //         }
    //         var (x, y, z) = grid.ToVoxelCoordinates(adjusted);
    //         ApplyTool(x, y, z);
    //     }
    //     else if (grid.gridBounds.IntersectRay(ray, out float enter))
    //     {
    //         Vector3 point = ray.GetPoint(enter);
    //         var (x, y, z) = grid.ToVoxelCoordinates(point);
    //         ApplyTool(x, y, z);
    //     }
    // }
    // Debug data
    Vector3 debug_rayOrigin;
    Vector3 debug_rayDir;

    public bool debug_hasHit;
    Vector3 debug_hitPoint;
    Vector3 debug_hitNormal;

    public bool debug_hasAdjusted;
    Vector3 debug_adjustedPoint;

    public bool debug_hasBoundsHit;
    Vector3 debug_boundsPoint;

    public bool debug_hasVoxel;
    Vector3 debug_voxelWorld;

    bool TryGetVoxel(Vector2 screenPos, out Vector3Int voxel)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        bool isDeleting = EditorState.Instance.activeMesh == VoxelMeshID.VVVVVV;

        var grid = gridRenderer;

        // Store ray
        debug_rayOrigin = ray.origin;
        debug_rayDir = ray.direction;

        debug_hasHit = false;
        debug_hasAdjusted = false;
        debug_hasBoundsHit = false;
        debug_hasVoxel = false;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            debug_hasHit = true;

            Vector3 point = hit.point;
            Vector3 normal = hit.normal;

            debug_hitPoint = point;
            debug_hitNormal = normal;

            Vector3 adjusted;

            if (isDeleting)
            {
                adjusted = point - normal * 0.5f;
            }
            else
            {
                adjusted = point + normal * 0.5f;
            }

            debug_hasAdjusted = true;
            debug_adjustedPoint = adjusted;

            if (!grid.ToVoxelCoordinates(adjusted, out voxel))
                return false;

            debug_hasVoxel = true;
            debug_voxelWorld = grid.originOffset.TransformPoint(new Vector3(voxel.x, voxel.y, voxel.z));

            return true;
        }
        else if (grid.gridBounds.IntersectRay(ray, out float enter))
        {
            debug_hasBoundsHit = true;

            Vector3 point = ray.GetPoint(enter);
            debug_boundsPoint = point;

            if (!grid.ToVoxelCoordinates(point, out voxel))
                return false;

            debug_hasVoxel = true;
            debug_voxelWorld = grid.originOffset.TransformPoint(new Vector3(voxel.x, voxel.y, voxel.z));

            return true;
        }
        voxel = default;
        return false;
    }


    void ApplyTool(Vector3Int voxel) { ApplyTool(voxel.x, voxel.y, voxel.z); }
    void ApplyTool(int x, int y, int z)
    {
        var grid = FindAnyObjectByType<VoxelGrid>();

        Voxel newVoxel = new();
        newVoxel.meshId = EditorState.Instance.activeMesh;
        newVoxel.orientationId = EditorState.Instance.activeOrientation;
        newVoxel.reflection = EditorState.Instance.activeReflection;
        grid.Set(x, y, z, newVoxel);
    }
    void OnDrawGizmos()
    {
        // Ray
        Gizmos.color = Color.white;
        Gizmos.DrawLine(debug_rayOrigin, debug_rayOrigin + debug_rayDir * 100f);

        // Hit point
        if (debug_hasHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(debug_hitPoint, 0.1f);

            // Normal
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(debug_hitPoint, debug_hitPoint + debug_hitNormal);
        }

        // Adjusted point
        if (debug_hasAdjusted)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(debug_adjustedPoint, 0.12f);
        }

        // Bounds intersection
        if (debug_hasBoundsHit)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(debug_boundsPoint, 0.15f);
        }

        // Final voxel
        if (debug_hasVoxel)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(debug_voxelWorld, Vector3.one);
        }
    }

}
