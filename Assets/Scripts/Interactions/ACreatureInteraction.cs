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

    public void Tick()
    {
        for (int i = participants.Count - 1; i >= 0; i--)
        {
            CreatureData p = participants[i];
            CreatureInteractionState pState = participantStates[p];

            switch (pState.Phase)
            {
                case InteractionPhase.UnInitialized:
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
        Debug.Log($"Dispatching action for participant[{participant.Identity}]");
        CreatureInteractionState state = participantStates[participant];
        state.ActionComplete = false;
        participant.Driver.Execute(
            action.Action, () =>
            {
                Debug.Log($"COMPLETED ACTION [{state.ActionIndex}]");
                state.ActionComplete = true;
                state.ActionIndex++;
            },
            action.CompletionCondition == null ? null : () => action.CompletionCondition(participant.Driver));
    }

    #endregion
}