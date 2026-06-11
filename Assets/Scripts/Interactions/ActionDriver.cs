using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ActionDriver : MonoBehaviour
{
    [Header("Optional References")]

    [SerializeField]
    private MonoBehaviour emotionProvider;

    private NavMeshAgent agent;
    private Animator animator;

    public NavMeshAgent Agent => agent;

    public Animator Animator => animator;

    public Transform CachedTransform
    {
        get;
        private set;
    }

    private void Awake()
    {
        CachedTransform = transform;

        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();
    }

    #region Navigation

    public void MoveTo(
        Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    public bool HasReachedDestination(
        float tolerance = 0.25f)
    {
        if (agent.pathPending)
            return false;

        return agent.remainingDistance <= tolerance;
    }

    public void StopMoving()
    {
        agent.SetDestination(
            CachedTransform.position);
    }

    #endregion

    #region Rotation

    public void FacePosition(
        Vector3 position,
        float rotationSpeed = 360f)
    {
        Vector3 direction =
            position - CachedTransform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized);

        CachedTransform.rotation =
            Quaternion.RotateTowards(
                CachedTransform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime);
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

    public void SetBool(
        string parameter,
        bool value)
    {
        animator.SetBool(
            parameter,
            value);
    }

    public void SetFloat(
        string parameter,
        float value)
    {
        animator.SetFloat(
            parameter,
            value);
    }

    public void SetTrigger(
        string parameter)
    {
        animator.SetTrigger(
            parameter);
    }

    public AnimatorStateInfo
        GetCurrentAnimatorState()
    {
        return animator
            .GetCurrentAnimatorStateInfo(0);
    }

    #endregion

    #region Emotions

    public bool TrySetEmotion(
        string emotionName,
        float value)
    {
        if (emotionProvider == null)
            return false;

        if (emotionProvider
            is IEmotionProvider provider)
        {
            provider.SetEmotion(
                emotionName,
                value);

            return true;
        }

        return false;
    }

    public bool TryGetEmotion(
        string emotionName,
        out float value)
    {
        value = 0f;

        if (emotionProvider == null)
            return false;

        if (emotionProvider
            is IEmotionProvider provider)
        {
            value =
                provider.GetEmotion(
                    emotionName);

            return true;
        }

        return false;
    }

    internal System.Numerics.Vector3 GetPosition()
    {
        throw new NotImplementedException();
    }

    #endregion
}