using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Microsoft.VisualBasic;
using UnityEngine.UIElements;
using UnityEngine;

public enum InteractionPriority
{
    Background,
    Normal,
    Important,
    Critical
}

public enum InteractionPhase
{
    CtorPendingJoin = default,
    Join,
    Update,
    Leave
}

public sealed class CreatureInteractionState
{
    public InteractionPhase Phase;
    public int ActionIndex;
    public bool ActionComplete;
}


public abstract class AInteractionParams { }
public abstract class ACreatureInteraction<TParams> : ACreatureInteraction where TParams : AInteractionParams
{
    protected readonly TParams InteractionParameters;
    protected ACreatureInteraction(TParams parameters, List<CreatureData> participants) : base(participants)
    {
        InteractionParameters = parameters;
    }
}

public abstract class ACreatureInteraction
{
    #region COMPILE TIME
    public abstract string InteractionName { get; }
    public abstract string Description { get; }
    public abstract int MinParticipants { get; }
    public abstract int MaxParticipants { get; }
    public abstract bool AllowLateJoining { get; }
    public abstract bool AllowEarlyLeaving { get; }
    public abstract InteractionPriority Priority { get; }
    public abstract bool InterruptLowerPriorityInteractions { get; }
    public IReadOnlyList<CreatureData> Participants => participants;

    #endregion
    #region RUN TIME
    private Dictionary<CreatureData, CreatureInteractionState> participantStates = new();
    private readonly List<CreatureData> participants = new();

    #endregion

    public ACreatureInteraction(List<CreatureData> participantList)
    {
        foreach (var p in participantList)
        {
            participants.Add(p);
            CreatureInteractionState s = new()
            {
                Phase = InteractionPhase.CtorPendingJoin,
                ActionComplete = true,
                ActionIndex = 0
            };
            participantStates[p] = s;
        }
    }

    #region INTERACTION CONTROL FLOW CHECKS

    /// <summary>
    /// Determines conditions for when a creature is allowed to join (be it during initialization or late join)
    ///  <br/>
    /// Base implementation: <br/>
    ///     participants.Count < MaxParticipants
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
    protected virtual bool CheckJoin(CreatureData participant) { return participants.Count < MaxParticipants; }
    protected virtual bool JoinFinished(CreatureData participant) { return true; }
    /// <summary>
    /// Determines conditions for when a creature is allowed to join (be it during initialization or late join)
    /// 
    /// Base implementation:
    ///     participants.Count < MinParticipants
    /// </summary>
    /// <param name="participantData"></param>
    /// <returns></returns>
    protected virtual bool CheckLeave(CreatureData participantData) { return participants.Count < MinParticipants; }
    protected virtual bool LeaveFinished(CreatureData participant) { return true; }

    /// <summary>
    /// Defines synchronization barriers within the interaction flow. <br/>
    /// <br/>
    /// When <see langword="true"/> is returned for an action index,
    /// participants reaching that action will wait until every other
    /// participant has reached the same action before continuing. <br/>
    /// <br/>
    /// Override this to coordinate multi-creature interactions. <br/>
    /// <br/>
    /// Example: <br/>
    ///     Action 0 -> Everyone moves <br/>
    ///     Action 1 -> Synchronization barrier <br/>
    ///     Action 2 -> Everyone starts dancing simultaneously <br/>
    /// </summary>
    /// <param name="actionIndex">
    /// Action index being evaluated.
    /// </param>
    protected virtual bool IsSynchronizedAction(int actionIndex) { return false; }

    #endregion

    #region INTERACTION FUNCTIONS

    protected virtual void JoinInteraction(CreatureData participant) { }
    protected virtual void OnParticipantJoined(CreatureData joined)
    {
        Debug.Log($"Creature[{joined.Identity}] JOINED the interaction[{this.InteractionName}]");
    }

    protected virtual void UpdateInteraction(CreatureData participant)
    {
        var pState = participantStates[participant];
        while (pState.ActionComplete)
        {
            if (IsBlockedBySynchronization(participant))
            {
                break;
            }

            if (!pState.ActionComplete)
            {
                pState.ActionComplete = false;
                break;
            }

            pState.ActionIndex++;
        }
    }
    protected virtual void LeaveInteraction(CreatureData participant) { }

    // Base must be called if overriden to ensure that interaction manager is correctly notified of internal participant abandoment of interaction
    protected virtual void OnParticipantLeft(CreatureData left)
    {
        Debug.Log($"Creature[{left.Identity}] LEFT the interaction[{this.InteractionName}]");
        InteractionManager.NotifyParticipantLeft(this, left);
    }

    protected virtual void RemoveParticipantData(CreatureData participant)
    {
        participants.Remove(participant);
        participantStates.Remove(participant);
    }

    #endregion

    #region PUBLIC METHOD INTERFACE
    public virtual bool ValidateInteraction()
    {
        return participants.Count >= MinParticipants;
    }

    public virtual bool IsInteractionEmpty()
    {
        return participants.Count == 0;
    }
    public virtual bool TryJoin(CreatureData participant)
    {
        if (!AllowLateJoining ||
            participants.Contains(participant) ||
            participants.Count >= MaxParticipants)
        {
            return false;
        }
        Join(participant);
        return true;
    }

    protected void Join(CreatureData p)
    {
        if (!participantStates.ContainsKey(p))
        {
            participantStates[p] = new CreatureInteractionState
            {
                Phase = InteractionPhase.Join,
                ActionIndex = 0,
                ActionComplete = true
            };

#if UNITY_EDITOR
            if (participants.Contains(p))
                Debug.LogError("The participant list should not already contain (Late) joining participant");
#endif
            participants.Add(p);
        }
        else
        {
            participantStates[p].Phase = InteractionPhase.Join;
            participantStates[p].ActionIndex = 0;
            participantStates[p].ActionComplete = true;
#if UNITY_EDITOR
            if (!participants.Contains(p))
                Debug.LogError("The participan list should already contain UnInitialized joining participant!");
#endif
        }

        OnParticipantJoined(p);
    }

    public virtual bool TryLeave(CreatureData participant)
    {
        if (!AllowEarlyLeaving ||
            !participants.Contains(participant))
        {
            return false;
        }

        participantStates[participant].Phase = InteractionPhase.Leave;

        return true;
    }

    public virtual void ForceLeave(CreatureData participant)
    {
        if (participants.Contains(participant))
        {
            participantStates[participant].Phase = InteractionPhase.Leave;
        }
    }

    /// <summary>
    /// Advances the interaction by one frame.
    ///
    /// Each participant progresses independently through:
    ///
    /// <list type="number">
    /// <item><see cref="InteractionPhase.UnInitialized"/></item>
    /// <item><see cref="InteractionPhase.Join"/></item>
    /// <item><see cref="InteractionPhase.Update"/></item>
    /// <item><see cref="InteractionPhase.Leave"/></item>
    /// </list>
    ///
    /// Action execution only occurs while
    /// <see cref="CreatureInteractionState.ActionComplete"/>
    /// is <see langword="true"/>.
    /// </summary>
    public void Tick()
    {
        for (int i = participants.Count - 1; i >= 0; i--)
        {
            CreatureData p = participants[i];
            CreatureInteractionState pState = participantStates[p];

            switch (pState.Phase)
            {
                case InteractionPhase.CtorPendingJoin:
                    Join(p);
                    break;
                case InteractionPhase.Join:

                    if (!pState.ActionComplete)
                    {
                        break;
                    }

                    JoinInteraction(p);

                    if (JoinFinished(p))
                    {
                        pState.Phase = InteractionPhase.Update;
                    }

                    break;

                case InteractionPhase.Update:

                    if (CheckLeave(p))
                    {
                        pState.Phase = InteractionPhase.Leave;
                        break;
                    }

                    if (pState.ActionComplete)
                    {
                        UpdateInteraction(p);
                    }

                    break;

                case InteractionPhase.Leave:

                    if (!pState.ActionComplete)
                    {
                        break;
                    }

                    LeaveInteraction(p);

                    if (LeaveFinished(p))
                    {
                        OnParticipantLeft(p);
                        RemoveParticipantData(p);
                    }

                    break;
            }
        }
    }

    #endregion

    #region HELPER METHODS
    protected int IndexOf(CreatureData participant)
    {
        return participants.IndexOf(participant);
    }
    protected CreatureInteractionState StateOf(CreatureData creature)
    {
        return participantStates[creature];
    }

    protected bool AllParticipantsPastAction(int actionIndex)
    {
        foreach (var participant in participants)
        {
            CreatureInteractionState state = participantStates[participant];

            if (state.ActionIndex <= actionIndex)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlockedBySynchronization(CreatureData participant)
    {
        CreatureInteractionState state = participantStates[participant];

        int currentAction = state.ActionIndex;

        if (!IsSynchronizedAction(currentAction))
        {
            return false;
        }

        foreach (var other in participants)
        {
            if (other == participant)
            {
                continue;
            }
            if (participantStates[other].ActionIndex < currentAction)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a completion condition that succeeds after
    /// <paramref name="seconds"/> seconds have elapsed. </br>
    /// </br>
    /// Useful for adding durations to actions. </br>
    /// </br>
    /// Example: </br>
    ///     DispatchAction(participant, DriverActions.SetBool("IsDancing", true), After(5f));
    /// </summary>
    protected Func<bool> After(float seconds)
    {
        float endTime = Time.time + seconds;

        return () => Time.time >= endTime;
    }

    /// <summary>
    /// Creates a completion condition that succeeds when
    /// any supplied condition succeeds. </br>
    /// </br>
    /// i.e logical OR combination of all conditions
    /// </summary>
    protected Func<bool> AnyOf(params Func<bool>[] conditions)
    {
        return () => conditions.Any(c => c());
    }

    /// <summary>
    /// Creates a completion condition that succeeds when
    /// any supplied condition succeeds. </br>
    /// </br>
    /// i.e logical AND combination of all conditions
    /// </summary>
    protected Func<bool> AllOf(params Func<bool>[] conditions)
    {
        return () => conditions.All(c => c());
    }

    /// <summary>
    /// Dispatches a <see cref="DriverActionDefinition"/> to a participant. </br>
    /// </br>
    /// The participant is marked as busy until the completion condition
    /// evaluates to <see langword="true"/>. Once completed: </br>
    /// </br>
    /// - <see cref="CreatureInteractionState.ActionComplete"/> is set. </br>
    /// - <see cref="CreatureInteractionState.ActionIndex"/> is incremented. </br>
    /// </br>
    /// By default, the action's own completion condition is used, but
    /// it may be overridden for this specific dispatch. </br>
    /// </br>
    /// This method is intended to be called from
    /// <see cref="UpdateInteraction(CreatureData)"/> implementations.
    /// </summary>
    /// <param name="participant">
    /// Participant that will execute the action.
    /// </param>
    /// <param name="action">
    /// Action definition to execute.
    /// </param>
    /// <param name="completionConditionOverride">
    /// Optional completion condition used instead of
    /// <see cref="DriverActionDefinition.CompletionCondition"/>.
    /// </param>
    protected void DispatchAction(CreatureData participant, DriverActionDefinition action, Func<bool> completionConditionOverride = null)
    {
        Debug.Log($"Dispatching action[{action}] for participant[{participant.Identity}] with completionConditionOverride[{completionConditionOverride}]");
        CreatureInteractionState state = participantStates[participant];
        state.ActionComplete = false;
        Func<bool> completeExpr = completionConditionOverride ??
            (action.CompletionCondition == null
                ? null
                : () => action.CompletionCondition(participant.Driver));
        participant.Driver.Execute(
            action.Action, () =>
            {
                Debug.Log($"COMPLETED ACTION [{state.ActionIndex}]");
                state.ActionComplete = true;
                state.ActionIndex++;
            },
            completeExpr

        );
    }

    #endregion
}