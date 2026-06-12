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

public abstract class AInteractionParams { }
public abstract class ACreatureInteraction<TParams> : ACreatureInteraction where TParams : AInteractionParams
{
    protected readonly TParams Params;
    protected ACreatureInteraction(TParams parameters, List<ParticipantData> participants) : base(participants)
    {
        Params = parameters;
    }
}

public abstract class ACreatureInteraction
{
    #region COMPILE TIME
    public abstract string InteractionName { get; }
    public abstract string Description { get; }
    public abstract int MinParticipants { get; }
    public abstract int MaxParticipants { get; }
    public abstract bool AllowJoining { get; }
    public abstract bool AllowLeaving { get; }
    public abstract InteractionPriority Priority { get; }
    public abstract bool InterruptLowerPriorityInteractions { get; }
    public virtual bool IsInteractionCompleted() { return participants.Count < MinParticipants; }

    #endregion
    #region RUN TIME
    private readonly List<ParticipantData> participants = new();
    public IReadOnlyList<ParticipantData> Participants => participants;

    #endregion

    public ACreatureInteraction(List<ParticipantData> participantList)
    {
        participants.AddRange(participantList);
        foreach (var p in participants)
        {
            p.State = InteractionState.Starting;
        }
    }

    #region INTERACTION CONTROL FLOW CHECKS

    // action beginning preconditions
    protected virtual bool CheckStartAllParticipants() { return true; }
    protected virtual bool CheckStart(ParticipantData participant) { return true; }
    protected virtual bool StartFinished(ParticipantData participant) { return true; }
    protected virtual bool UpdateFinished(ParticipantData participant) { return true; }
    protected virtual bool EndFinished(ParticipantData participant) { return true; }

    protected virtual bool CheckJoin(ParticipantData participant) { return true; }
    protected virtual bool JoinFinished(ParticipantData participant) { return true; }
    protected virtual bool CheckLeave(ParticipantData participantData) { return true; }
    protected virtual bool LeaveFinished(ParticipantData participant) { return true; }

    #endregion

    #region INTERACTION FUNCTIONS
    
    protected virtual void StartInteraction(ParticipantData participant)
    {
        // PerformAction(ActionDriver.MoveToPoint(posA),..)
        // PerformAction(ActionDriver.TurnTowards(posB),..., (syncPoint =) true)
        // PerformAction(ActionDriver.WaveEmote(),.., )
    }
    protected void JoinInteraction(ParticipantData participant) { }
    protected virtual void OnParticipantJoined(ParticipantData joined) { }
    protected abstract void UpdateInteraction(ParticipantData participant);

    protected virtual void LeaveInteraction(ParticipantData participant) { }

    // regular finish interaction path orderly liberate participants etc.
    protected virtual void EndInteraction(ParticipantData participant) { }



    protected virtual void OnParticipantLeft(ParticipantData left) { }

    protected virtual void AbortParticipant(ParticipantData participant)
    {
        OnParticipantLeft(participant);
        participants.Remove(participant);
    }

    #endregion

    #region PUBLIC METHOD INTERFACE

    public virtual bool TryJoin(ParticipantData participant)
    {
        if (AllowJoining && !participants.Contains(participant))
        {
            participant.State = InteractionState.Joining;
            participants.Add(participant);
            OnParticipantJoined(participant);
            return true;
        }
        return false;
    }

    public virtual bool TryLeave(ParticipantData participant)
    {
        if (AllowLeaving && participants.Contains(participant))
        {
            participant.State = InteractionState.Leaving;
            // participants.Remove(participant);
            // OnParticipantLeft(participant);
            return true;
        }
        return false;
    }

    public virtual void ForceStop(ParticipantData participant)
    {
        if (participants != null && participants.Contains(participant))
        {
            participant.State = InteractionState.Abort;
        }
    }

    public void Tick()
    {
        // perhaps we should implement the initial start point in here for the interaction.
        for (int i = participants.Count - 1; i > 0; i--)
        {
            var p = participants[i];

            if (p.State == InteractionState.Abort)
            {
                AbortParticipant(p);
                continue;
            }

            if (!p.ActionComplete)
                continue;

            switch (p.State)
            {
                case InteractionState.Starting:
                    StartInteraction(p);
                    if (StartFinished(p))
                    {
                        p.State = InteractionState.Update;
                    }
                        break;

                case InteractionState.Joining:
                    JoinInteraction(p);
                    if (JoinFinished(p))
                    {
                        p.State = InteractionState.Update;
                    }
                    break;

                case InteractionState.Update:
                    UpdateInteraction(p);
                    if (UpdateFinished(p))
                    {
                        p.State = InteractionState.Ending;
                    }
                    break;

                case InteractionState.Leaving:
                    LeaveInteraction(p);
                    if (LeaveFinished(p))
                    {
                        p.State = InteractionState.Abort;
                    }
                    break;

                case InteractionState.Ending:
                    if (EndFinished(p))
                    {
                        p.State = InteractionState.Abort;
                    }
                    break;
            }
        }
    }

    #endregion
}