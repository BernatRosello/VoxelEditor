using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

public delegate void DriverAction(ActionDriver driver, Action completionCallback);

public sealed class DriverActionDefinition
{
    public DriverAction Action { get; init; }
    public Func<ActionDriver, bool> CompletionCondition { get; init; }
}

public static class DriverActions
{
    public static DriverActionDefinition MoveTo(Vector3 destination)
    {
        return new()
        {
            Action = (driver, completed) => { driver.MoveTo(destination); },
            CompletionCondition = driver => driver.HasReachedDestination()
        };
    }

    public static DriverActionDefinition StopMoving()
    {
        return new()
        {
            Action = (driver, completed) => { driver.StopMoving(); completed(); }
        };
    }

    public static DriverActionDefinition FaceDirection(Vector3 dir)
    {
        return new()
        {
            Action = (driver, completed) => { driver.FaceDirection(dir); },
            CompletionCondition = driver => driver.HasReachedDestination()
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
            Action = (driver, completed) => { action(driver); completed(); }
        };
    }

    public static DriverActionDefinition SetTrigger(string trigger)
    {
        return new()
        {
            Action = (driver, completed) => { driver.SetTrigger(trigger); completed(); }
        };
    }

    public static DriverActionDefinition SetBool(string trigger, bool value)
    {
        return new()
        {
            Action = (driver, completed) => { driver.SetBool(trigger, value); completed(); }
        };
    }
}

[RequireComponent(typeof(NavigationAnimator))]
[RequireComponent(typeof(Animator))]
public class ActionDriver : MonoBehaviour
{
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
    private bool actionFinished;
    internal void Execute(DriverAction driverAction, Action completionCallback, Func<bool> completionCondition = null)
    {
        if (IsBusy)
        {
            throw new InvalidOperationException($"{name} is already executing an action.");
        }

        actionFinished = false;

        void Complete()
        {
            if (actionFinished)
                return;
            actionFinished = true;
            this.completionCondition = null;
            this.completionCallback = null;
            completionCallback?.Invoke();
        }

        driverAction(this, Complete);

        if (actionFinished)
            return;
        if (completionCondition == null)
        {
            Complete();
            return;
        }

        this.completionCondition = completionCondition;
        this.completionCallback = completionCallback;
    }
    #endregion

    #region Navigation

    internal void MoveTo(Vector3 destination)
    {
        nav.SetDestination(destination);
    }

    internal bool HasReachedDestination(
        float tolerance = 0.1f)
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
        nav.SetDestination(CachedTransform.position);
    }

    #endregion

    #region Rotation

    public void FaceDirection(Vector3 dir)
    {
        nav.MoveTo(GetPosition() + dir * 0.1f);
    }

    internal void FacePosition(Vector3 position)
    {
        return FaceDirection(position - GetPosition());
    }

    public bool IsFacingDirection(Vector3 dir, float toleranceDegrees = 5f, bool verticalCheck = false)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return true;

        if (!verticalCheck)
            dir.y = 0;

        float angle = Vector3.Angle(CachedTransform.forward, direction.normalized);

        return angle <= toleranceDegrees;
    }

    public bool IsFacingPosition(Vector3 position, float toleranceDegrees = 5f)
    {
        return IsFacingDirection(position - GetPosition(), toleranceDegrees);
    }

    #endregion

    #region Animation

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
}