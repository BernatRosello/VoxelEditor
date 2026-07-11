using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.Search;
using UnityEngine;

public delegate void DriverAction(ActionDriver driver);

public sealed class DriverActionDefinition
{
    public DriverAction Action { get; internal set; }
    public Func<ActionDriver, bool> CompletionCondition { get; internal set; }
}

public static class DriverActions
{
    public static DriverActionDefinition MoveTo(Vector3 destination, float? maxSpeed = null)
    {
        return new()
        {
            Action = driver => driver.MoveTo(destination, maxSpeed),
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

    public static DriverActionDefinition SetTrigger(string trigger, string waitOnState = "")
    {
        return new()
        {
            Action = (driver) => { driver.SetTrigger(trigger); },
            CompletionCondition = waitOnState != "" ? driver =>
            {
                var res = driver.QueryAnimatorStateCompletion(waitOnState);
                Debug.Log($"[{driver.GetComponent<Creature>().Identity}] completion of waitOnState \"{waitOnState}\" triggered by \"{trigger}\": {res}");
                return res;
            }
            : null
        };
    }

    public static DriverActionDefinition SetBool(string boolean, bool value)
    {
        return new()
        {
            Action = (driver) => { driver.SetBool(boolean, value); }
        };
    }

    public static DriverActionDefinition SetFloat(string floating, float value)
    {
        return new()
        {
            Action = (driver) => { driver.SetFloat(floating, value); }
        };
    }

    public static DriverActionDefinition EmitParticle(CreatureParticle particle, int count = 1)
    {
        return new()
        {
            Action = (driver) => { driver.EmitParticle(particle, count); }
        };
    }
}

[RequireComponent(typeof(NavigationAnimator))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ParticleController))]
public class ActionDriver : MonoBehaviour
{
    private IReadOnlyList<GameObject> meshObjects;
    private readonly Dictionary<GameObject, int> originalLayers = new();

    private Animator animator;
    private NavigationAnimator nav;
    private ParticleController particleController;

    public Transform CachedTransform
    {
        get;
        private set;
    }

    private Func<bool> completionCondition;
    private Action completionCallback;
    private AnimatorStateQueryStatus queriedAnimatorStateFulfillStatus = AnimatorStateQueryStatus.Uninitialized;

    public bool IsBusy => completionCondition != null;

    private string queriedAnimatorStateName = null;

    public string GetQueriedAnimatorStateName()
    {
        return queriedAnimatorStateName;
    }

    private void SetQueriedAnimatorStateName(string value)
    {
        queriedAnimatorStateFulfillStatus = AnimatorStateQueryStatus.Uninitialized;
        queriedAnimatorStateName = value;
    }

    internal bool QueryAnimatorStateCompletion(string stateName)
    {
        if (GetQueriedAnimatorStateName() != stateName)
        {
            Debug.Log($"Changed active animator state completion query from({GetQueriedAnimatorStateName()})over to state: {stateName}");
            SetQueriedAnimatorStateName(stateName);
            return false;
        }
        else
        {
            Debug.Log($"Current fulfill status for stateName \"{stateName}\": {queriedAnimatorStateFulfillStatus}");
            bool res = queriedAnimatorStateFulfillStatus == AnimatorStateQueryStatus.Completed;
            if (res)
            {
                // De-init the query to free up the next possible one.
                SetQueriedAnimatorStateName(null);
            }
            return res;
        }
    }
    public void NotifyStateCompletion(AnimatorStateQueryStatus completion) { queriedAnimatorStateFulfillStatus = completion; }


#if UNITY_EDITOR
    private void OnValidate()
    {
        bool dirty = false;

        if (particleController == null)
        {
            particleController = GetComponent<ParticleController>();
            dirty = true;
        }

        if (nav == null)
        {
            nav = GetComponent<NavigationAnimator>();
            dirty = true;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            dirty = true;
        }

        if (dirty)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Awake()
    {
        CachedTransform = transform;

        if (particleController == null) particleController = GetComponent<ParticleController>();

        if (nav == null) nav = GetComponent<NavigationAnimator>();

        if (animator == null) animator = GetComponent<Animator>();

        meshObjects = GetComponentsInChildren<SkinnedMeshRenderer>().Select(m => m.gameObject).ToList();
    }

    private void Update()
    {
        if (!IsBusy)
        {
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

    internal void MoveTo(Vector3 destination, float? maxSpeed = null)
    {
        if (maxSpeed.HasValue) nav.SetMaxSpeed(maxSpeed.Value);
        nav.SetDestination(destination);
    }

    internal bool HasReachedDestination(
        float tolerance = -1.0f)
    {
        if (!nav.HasDestination())
            return true;

        if (tolerance == -1.0f)
            tolerance = Mathf.Min(nav.MoveStartThreshold, nav.MoveStopThreshold);

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

    public bool IsFacingDirection(Vector3 dir, float toleranceDegrees = -1.0f, bool verticalCheck = false)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return true;

        if (!verticalCheck)
            dir.y = 0;

        float angle = Vector3.Angle(CachedTransform.forward, dir.normalized);

        if (toleranceDegrees == -1.0f)
            toleranceDegrees = Mathf.Min(nav.TurnStartThreshold, nav.TurnStopThreshold);

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

    internal bool GetBool(string parameter)
    {
        return animator.GetBool(parameter);
    }

    internal AnimatorStateInfo GetCurrentAnimatorState()
    {
        return animator.GetCurrentAnimatorStateInfo(0);
    }
    public bool AnimatorIsPlaying()
    {
        return animator.GetCurrentAnimatorStateInfo(0).length >
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
    public bool AnimatorIsPlaying(string stateName)
    {
        return AnimatorIsPlaying() && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    public void StartTurnRight()
    {
        nav.NavigationActive = false;
        animator.SetBool("IsTurning", true);
        animator.SetFloat("vel_ang", 1);
    }

    public void StartTurnLeft()
    {
        nav.NavigationActive = false;
        animator.SetBool("IsTurning", true);
        animator.SetFloat("vel_ang", -1);
    }

    public void StopTurn()
    {
        nav.NavigationActive = true;
        animator.SetFloat("vel_ang", 0);
    }

    #endregion

    #region VFX
    private void CacheMeshLayers()
    {
        if (originalLayers.ContainsKey(meshObjects[0]))
            return;

        foreach (var go in meshObjects)
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
        foreach (var go in meshObjects)
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
        foreach (var kvp in originalLayers)
        {
            kvp.Key.layer = kvp.Value;
        }
        ClearMeshLayers();
    }

    internal void EmitParticle(CreatureParticle particle, int count = 1)
    {
        particleController.EmitParticles(particle, count);
    }
    #endregion
}

public enum AnimatorStateQueryStatus
{
    Uninitialized,
    Entered,
    Completed
}