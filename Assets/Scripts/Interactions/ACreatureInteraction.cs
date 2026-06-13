using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Microsoft.VisualBasic;
using UnityEngine.UIElements;

public enum InteractionPriority
{
    Background,
    Normal,
    Important,
    Critical
}

public enum InteractionPhase
{
    UnInitialized = default,
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
                Phase = InteractionPhase.UnInitialized,
                ActionComplete = true,
                ActionIndex = 0
            };
            participantStates[p] = s;
            OnParticipantJoined(p);
        }
    }

    #region INTERACTION CONTROL FLOW CHECKS
    /// <summary>
    /// Determines conditions for when a creature is allowed to join (be it during initialization or late join)
    /// 
    /// Base implementation:
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
    protected virtual bool IsSynchronizedAction(int actionIndex) { return false; }

    #endregion

    #region INTERACTION FUNCTIONS

    protected virtual void JoinInteraction(CreatureData participant) { }
    protected virtual void OnParticipantJoined(CreatureData joined) { }
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
    protected virtual void OnParticipantLeft(CreatureData left) { InteractionManager.NotifyParticipantLeft(this, left); }

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

        participantStates[participant] = new CreatureInteractionState
        {
            Phase = InteractionPhase.Join,
            ActionIndex = 0,
            ActionComplete = true
        };

        participants.Add(participant);

        OnParticipantJoined(participant);

        return true;
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

    public void Tick()
    {
        for (int i = participants.Count - 1; i >= 0; i--)
        {
            CreatureData p = participants[i];
            CreatureInteractionState pState = participantStates[p];

            switch (pState.Phase)
            {
                case InteractionPhase.UnInitialized:
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
                    if (pState.ActionComplete)
                    {
                        UpdateInteraction(p);
                        break;
                    }
                    if (CheckLeave(p))
                    {
                        pState.Phase = InteractionPhase.Leave;
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

            CreatureInteractionState otherState = participantStates[other];

            if (otherState.ActionIndex < currentAction)
            {
                return true;
            }
        }

        return false;
    }

    protected void DispatchAction(CreatureData participant, DriverActionDefinition action)
    {
        CreatureInteractionState state = participantStates[participant];
        state.ActionComplete = false;
        participant.Driver.Execute(
            action.Action, () => state.ActionComplete = true,
            action.CompletionCondition == null ? null : () => action.CompletionCondition(participant.Driver));
    }

    #endregion
}