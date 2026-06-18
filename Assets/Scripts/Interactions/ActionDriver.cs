using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.AI;

public delegate void DriverAction(ActionDriver driver);

public sealed class DriverActionDefinition
{
    public DriverAction Action { get; internal set; }
    public Func<ActionDriver, bool> CompletionCondition { get; internal set; }
}

public static class DriverActions
{
    public static DriverActionDefinition MoveTo(Vector3 destination)
    {
        return new()
        {
            Action = (driver) => { driver.MoveTo(destination); },
            CompletionCondition = driver => driver.HasReachedDestination()
        };
    }

    public static DriverActionDefinition StopMoving()
    {
        return new()
        {
            Action = (driver) => { driver.StopMoving(); }
        };
    }

    public static DriverActionDefinition FaceDirection(Vector3 dir)
    {
        return new()
        {
            Action = (driver) => { driver.FaceDirection(dir); },
            CompletionCondition = driver => driver.IsFacingDirection(dir)
        };
    }
    public static DriverActionDefinition FacePosition(Vector3 position)
    {
        return new()
        {
            Action = (driver) => { driver.FacePosition(position); },
            CompletionCondition = driver => driver.IsFacingPosition(position)
        };
    }

    public static DriverActionDefinition ResetAnimator()
    {
        return new()
        {
            Action = (driver) => { driver.ResetToIdle(); }
        };
    }

    /// <summary>
    /// Warning! Should only be used with lambdas with immediate animator setter functions AntionDriver actions!
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public static DriverActionDefinition Animator(Action<ActionDriver> action)
    {
        return new()
        {
            Action = (driver) => { action(driver); }
        };
    }

    public static DriverActionDefinition SetTrigger(string trigger)
    {
        return new()
        {
            Action = (driver) => { driver.SetTrigger(trigger); }
        };
    }

    public static DriverActionDefinition SetBool(string trigger, bool value)
    {
        return new()
        {
            Action = (driver) => { driver.SetBool(trigger, value); }
        };
    }
}

[RequireComponent(typeof(NavigationAnimator))]
[RequireComponent(typeof(Animator))]
public class ActionDriver : MonoBehaviour
{
    private IReadOnlyList<GameObject> meshObjects;
    private readonly Dictionary<GameObject, int> originalLayers = new();

    private Animator animator;
    private NavigationAnimator nav;
    public Animator Animator => animator;

    public Transform CachedTransform
    {
        get;
        private set;
    }

    private Func<bool> completionCondition;
    private Action completionCallback;

    public bool IsBusy => completionCondition != null;

    private void Awake()
    {
        CachedTransform = transform;

        nav = GetComponent<NavigationAnimator>();

        animator = GetComponent<Animator>();

        meshObjects = GetComponentsInChildren<SkinnedMeshRenderer>().Select(m => m.gameObject).ToList();
    }

    private void Update()
    {
        if (!IsBusy)
        {
            IdleUpdate();
            return;
        }

        if (!completionCondition())
        {
            return;
        }

        Action callback = completionCallback;

        completionCondition = null;
        completionCallback = null;

        callback?.Invoke();
    }

    private float idleEmoteTimer;
    private float idleTime;
    private void IdleUpdate()
    {
        if (idleTime > idleEmoteTimer)
        {
            idleEmoteTimer = 10 + UnityEngine.Random.Range(-5,10);
            animator.SetTrigger("IdleEmote");
        }
    }

    #region Internals
    /// <summary>
    /// If no completion condition is provided or can be resolved by the DriverActionDefinition:
    /// the action is performed in Immediate mode and the completion callback is fired
    /// synchronously after driverAction dispatch.
    /// </summary>
    /// <param name="driverAction"></param>
    /// <param name="completionCallback"></param>
    /// <param name="completionCondition"></param>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Execute(DriverAction driverAction, Action completionCallback, Func<bool> completionCondition = null)
    {
        if (IsBusy)
        {
            throw new InvalidOperationException($"{name} is already executing an action.");
        }

        driverAction(this);

        this.completionCondition = completionCondition;
        this.completionCallback = completionCallback;

        // No condition => immediate action
        if (completionCondition == null)
        {
            completionCallback?.Invoke();
            return;
        }
    }
    #endregion

    #region Navigation

    internal void MoveTo(Vector3 destination)
    {
        nav.SetDestination(destination);
    }

    internal bool HasReachedDestination(
        float tolerance = 0.15f)
    {
        if (!nav.HasDestination())
            return true;

        return nav.GetRemainingDistance() < tolerance;
    }

    internal Vector3 GetPosition()
    {
        return CachedTransform.position;
        // return animator.transform.position;
    }

    internal void StopMoving()
    {
        nav.SetDestination(nav.GetPosition());
    }

    #endregion

    #region Rotation

    public void FaceDirection(Vector3 dir)
    {
        nav.SetDestination(GetPosition() + dir * 0.1f);
    }

    internal void FacePosition(Vector3 position)
    {
        FaceDirection(position - GetPosition());
    }

    public bool IsFacingDirection(Vector3 dir, float toleranceDegrees = 5f, bool verticalCheck = false)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return true;

        if (!verticalCheck)
            dir.y = 0;

        float angle = Vector3.Angle(CachedTransform.forward, dir.normalized);

        return angle <= toleranceDegrees;
    }

    public bool IsFacingPosition(Vector3 position, float toleranceDegrees = 5f)
    {
        return IsFacingDirection(position - GetPosition(), toleranceDegrees);
    }

    #endregion

    #region Animation
    internal void ResetToIdle()
    {
        animator.SetBool("IsDancing", false);
        StopMoving();
    }

    internal void SetBool(string parameter, bool value)
    {
        animator.SetBool(parameter, value);
    }

    internal void SetFloat(string parameter, float value)
    {
        animator.SetFloat(parameter, value);
    }

    internal void SetTrigger(string parameter)
    {
        animator.SetTrigger(parameter);
    }

    internal AnimatorStateInfo
        GetCurrentAnimatorState()
    {
        return animator.GetCurrentAnimatorStateInfo(0);
    }

    #endregion

    #region Emotions

    internal bool TrySetEmotion(string emotionName, float value)
    {
        // TODO
        return false;
    }

    internal bool TryGetEmotion(string emotionName, out float value)
    {
        // TODO
        value = 0;
        return false;
    }

    #endregion

    #region VFX
    private void CacheMeshLayers()
    {
        if (originalLayers.ContainsKey(meshObjects[0]))
            return;

        foreach(var go in meshObjects)
        {
            originalLayers[go] = go.layer;
        }
    }
    private void ClearMeshLayers()
    {
        originalLayers.Clear();
    }

    private void SetMeshLayers(string layerName)
    {
        int v = LayerMask.NameToLayer(layerName);
        foreach(var go in meshObjects)
        {
            go.layer = v;
        }
    }

    public void SetOutlineGreen()
    {
        CacheMeshLayers();
        SetMeshLayers("OutlineGreen");
    }


    public void SetOutlineBlue()
    {
        CacheMeshLayers();
        SetMeshLayers("OutlineBlue");
    }

    public void SetOutlineRed()
    {
        CacheMeshLayers();
        SetMeshLayers("OutlineRed");
    }

    public void SetOutlineNONE()
    {
        foreach(var kvp in originalLayers)
        {
            kvp.Key.layer = kvp.Value;
        }
        ClearMeshLayers();
    }
    #endregion
}