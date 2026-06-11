using System;
using System.Collections.Generic;
using System.Linq;


public enum InteractionPriority
{
    Background,
    Normal,
    Important,
    Critical
}

public abstract class AInteractionParams
{

}

public abstract class ACreatureInteraction<TParams> : ACreatureInteraction where TParams : AInteractionParams
{
    protected readonly TParams Params;

    protected ACreatureInteraction(TParams parameters, List<ParticipantData> participants) : base(participants)
    {
        Params = parameters;
    }
}

public enum InteractionState
{
    Starting,
    Running,
    Ending,
    Finished
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

    #endregion
    #region RUN TIME
    private InteractionState state;
    private readonly List<ParticipantData> participants = new();


    public IReadOnlyList<ParticipantData> Participants => participants;

    #endregion

    public ACreatureInteraction(List<ParticipantData> participantList)
    {
        participants.AddRange(participantList);
    }


    #region INTERACTION FUNCTIONS
    // action beginning preconditions
    public virtual bool CheckStart()
    {
        return true;
    }

    // action beginning preconditions
    public virtual void StartInteraction()
    {
    }

    // update phase
    public abstract void UpdateInteraction();

    // check if update phase is done
    public abstract bool CheckEnd();

    // regular finish interaction path orderly liberate participants etc.
    public virtual void EndInteraction()
    {
    }

    public bool IsFinished => state == InteractionState.Finished;

    public virtual bool TryJoin(ParticipantData participant)
    {
        return AllowJoining;
    }
    protected virtual void OnParticipantJoined(ParticipantData joined)
    {
    }

    public void JoinInteraction(ParticipantData participant)
    {
        participants.Add(participant);
        OnParticipantJoined(participant);
    }

    public virtual bool TryLeave(ParticipantData participant)
    {
        return AllowLeaving;
    }

    public virtual void LeaveInteraction(ParticipantData participant)
    {
        participants.Remove(participant);
        OnParticipantLeft(participant);
    }

    protected virtual void OnParticipantLeft(ParticipantData left)
    {
    }

    public virtual void AbortParticipant(ParticipantData participant)
    {
    }


    public void Tick()
    {

    }

    #endregion
}