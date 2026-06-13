using System;
using UnityEngine;
using UnityEngine.AI;

public delegate void DriverAction(ActionDriver driver, Action completionCallback);

public sealed class DriverActionDefinition
{
    public DriverAction Action;
    public Func<ActionDriver, bool> CompletionCondition;
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

    public static DriverActionDefinition SetTrigger(
        string trigger)
    {
        return new()
        {
            Action = (driver, completed) => { driver.SetTrigger(trigger); completed(); }
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
        if (completionCondition == null)
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
    internal void Execute(DriverAction driverAction, Action completionCallback, Func<bool> completionCondition = null)
    {
        if (IsBusy)
        {
            throw new InvalidOperationException($"{name} is already executing an action.");
        }

        driverAction(this, completionCallback);

        if (completionCondition == null)
        {
            return;
        }

        this.completionCondition = completionCondition;
        this.completionCallback = completionCallback;
    }
    #endregion

    #region Navigation

    internal void MoveTo(
        Vector3 destination)
    {
        nav.SetDestination(destination);
    }

    internal bool HasReachedDestination(
        float tolerance = 0.25f)
    {
        if (nav.HasDestination())
            return false;

        return nav.DistanceToDestination() > tolerance;
    }

    internal Vector3 GetPosition()
    {
        return animator.transform.position;
    }

    internal void StopMoving()
    {
        nav.SetDestination(
            CachedTransform.position);
    }

    #endregion

    #region Rotation

    internal void FacePosition(
        Vector3 position)
    {

    }

    public bool IsFacingPosition(
        Vector3 position,
        float toleranceDegrees = 5f)
    {
        Vector3 direction =
            position - CachedTransform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return true;

        float angle =
            Vector3.Angle(
                CachedTransform.forward,
                direction.normalized);

        return angle <= toleranceDegrees;
    }

    #endregion

    #region Animation

    internal void SetBool(
        string parameter,
        bool value)
    {
        animator.SetBool(
            parameter,
            value);
    }

    internal void SetFloat(
        string parameter,
        float value)
    {
        animator.SetFloat(
            parameter,
            value);
    }

    internal void SetTrigger(
        string parameter)
    {
        animator.SetTrigger(
            parameter);
    }

    internal AnimatorStateInfo
        GetCurrentAnimatorState()
    {
        return animator
            .GetCurrentAnimatorStateInfo(0);
    }

    #endregion

    #region Emotions

    internal bool TrySetEmotion(
        string emotionName,
        float value)
    {
        // TODO
        return false;
    }

    internal bool TryGetEmotion(
        string emotionName,
        out float value)
    {
        // TODO
        value = 0;
        return false;
    }

    #endregion
}