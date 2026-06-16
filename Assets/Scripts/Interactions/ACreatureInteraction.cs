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
    User,
    Critical,
}

public enum InteractionPhase
{
    Join,
    Update,
    Leave,
}

public sealed class CreatureInteractionState
{
    public InteractionPhase Phase;
    public int ActionIndex;
    public bool ActionComplete;
}


public abstract class AInteractionParams { }
public abstract class ACreatureInteraction<TParams> : ACreatureInteraction where TParams : AInteractionParams, new()
{
    protected readonly TParams Parameters;
    protected ACreatureInteraction(TParams parameters, List<CreatureData> participants) : base(participants)
    {
        if (parameters == null)
            Parameters = new();
        else
            Parameters = parameters;
    }
}

public abstract class ACreatureInteraction
{
    #region COMPILE TIME
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract int MinParticipants { get; }
    public abstract int MaxParticipants { get; }
    public abstract bool AllowLateJoining { get; }
    public abstract bool AllowEarlyLeaving { get; }
    public abstract InteractionPriority Priority { get; }
    public abstract bool InterruptLowerPriorityInteractions { get; }

    #endregion
    #region RUN TIME
    private Dictionary<CreatureData, CreatureInteractionState> participantStates = new();
    private readonly List<CreatureData> participants = new();
    private readonly Queue<CreatureData> pendingJoin = new();
    private readonly Queue<CreatureData> pendingLeave = new();
    protected bool continueUpdateTick;
    #endregion

    public ACreatureInteraction(List<CreatureData> participantList)
    {
        if (participantList == null) return;
        participantList.ForEach(p => pendingJoin.Enqueue(p));
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
        Debug.Log($"C[{joined.Identity}] JOINED the interaction[{this.Name}]");
    }

    /// <summary>
    /// <para> UpdateInteraction() must guarantee one of these outcomes: </para>
    /// <para> Dispatch an asynchronous action → participant.ActionComplete = false *This is applied automatically when dispatching actions (as long as completionCondition is evaluating to false)</para>
    /// <para> Advance synchronously → continueUpdateTick = true *This is applied automatically when dispatching immediate actions (completionCondition == null)</para>
    /// <para> Transition phase/end interaction → continueUpdateTick = false *This is applied automatically at the start of every updateTick</para>
    /// </summary>
    /// <param name="participant"></param>
    protected abstract void UpdateInteraction(CreatureData participant);
    protected virtual void LeaveInteraction(CreatureData participant) { Debug.Log($"C[{participant.Identity}] Leaving..."); }

    // Base must be called if overriden to ensure that interaction manager is correctly notified of internal participant abandoment of interaction
    protected virtual void OnParticipantLeft(CreatureData left)
    {
        Debug.Log($"C[{left.Identity}] LEFT the interaction[{this.Name}]");
        InteractionManager.NotifyParticipantLeft(this, left);
    }

    #endregion

    #region PUBLIC METHOD INTERFACE
    // Includes only active participants (discounting pendingJoin participants)
    public IReadOnlyList<CreatureData> ActiveParticipants => participants;
    /// <summary>
    /// Includes participants that are still pending to join
    /// </summary>
    public IReadOnlyList<CreatureData> AllParticipants => participants.Union(pendingJoin).ToList();
    public virtual bool ValidateInteraction()
    {
        return (participants.Count + pendingJoin.Count - pendingLeave.Count) >= MinParticipants;
    }

    public virtual bool IsInteractionEmpty()
    {
        return participants.Count == 0;
    }

    public virtual bool CanJoin(CreatureData c) => true;
    public bool TryJoin(CreatureData participant)
    {
        if (!AllowLateJoining ||
            pendingJoin.Contains(participant) ||
            participants.Contains(participant) ||
            participantStates[participant].Phase == InteractionPhase.Join ||
            participants.Count >= MaxParticipants)
        {
            return false;
        }
        if (!CanJoin(participant)) return false;

        pendingJoin.Enqueue(participant);
        return true;
    }

    public virtual bool CanLeave(CreatureData c) => true;
    public bool TryLeave(CreatureData participant)
    {
        if (!AllowEarlyLeaving ||
            pendingLeave.Contains(participant) ||
            !participants.Contains(participant) ||
            participantStates[participant].Phase == InteractionPhase.Leave)
        {
            return false;
        }
        if (!CanLeave(participant)) return false;

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
        while (pendingJoin.Count > 0)
        {
            BaseAddParticipant(pendingJoin.Dequeue());
        }

        foreach (var p in participants)
        {
            Debug.Log(
                $"[{p.Identity}] " +
                $"Phase={participantStates[p].Phase} " +
                $"Action={participantStates[p].ActionIndex} " +
                $"Complete={participantStates[p].ActionComplete}"
            );
            switch (participantStates[p].Phase)
            {
                case InteractionPhase.Join:

                    if (!participantStates[p].ActionComplete)
                    {
                        break;
                    }

                    JoinInteraction(p);

                    if (JoinFinished(p))
                    {
                        OnParticipantJoined(p);
                        participantStates[p].Phase = InteractionPhase.Update;
                    }

                    break;

                case InteractionPhase.Update:

                    if (CheckLeave(p))
                    {
                        Debug.Log($"C[{p.Identity}] met leaving conditions, abandoning Update Phase...");
                        participantStates[p].Phase = InteractionPhase.Leave;
                        break;
                    }

                    if (participantStates[p].ActionComplete)
                    {
                        continueUpdateTick = false;
                        do
                        {
                            if (IsBlockedBySynchronization(p))
                            {
                                break;
                            }
                            UpdateInteraction(p);

                        } while (!CheckLeave(p) && continueUpdateTick);

                    }
                    break;

                case InteractionPhase.Leave:

                    if (!participantStates[p].ActionComplete)
                    {
                        Debug.Log(
                            $"[{p.Identity}] Waiting to enter Leave. " +
                            $"DriverBusy={p.Driver.IsBusy}");
                        break;
                    }

                    LeaveInteraction(p);

                    if (LeaveFinished(p))
                    {
                        OnParticipantLeft(p);
                        pendingLeave.Enqueue(p);
                    }

                    break;
            }
        }

        while (pendingLeave.Count > 0)
        {
            BaseRemoveParticipant(pendingLeave.Dequeue());
        }

    }

    #endregion

    #region HELPER METHODS

    private void BaseAddParticipant(CreatureData p)
    {
        participants.Add(p);
        participantStates[p] = new CreatureInteractionState
        {
            Phase = InteractionPhase.Join,
            ActionIndex = 0,
            ActionComplete = true
        };

        AddParticipantData(p);
    }

    /// <summary>
    /// <para> Will be called automatically whenever a particular participant's data must be added. </para>
    /// Override when extra data must be initialized whenever a new participant joins the interaction.
    /// </summary>
    /// <param name="p"></param>
    protected virtual void AddParticipantData(CreatureData p) { }

    private void BaseRemoveParticipant(CreatureData participant)
    {
        participants.Remove(participant);
        participantStates.Remove(participant);
        RemoveParticipantData(participant);
    }

    /// <summary>
    /// <para> Will be called automatically whenever a particular participant's data must be removed. </para>
    /// Override when extra data must be cleaned up whenever a participant leaves the interaction.
    /// </summary>
    /// <param name="p"></param>
    protected virtual void RemoveParticipantData(CreatureData p) { }

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

            if (state.ActionIndex < actionIndex || (state.ActionIndex == actionIndex && !state.ActionComplete))
            {
                return false;
            }
        }

        return true;
    }

    // DEBUG PURPOSES ONLY FOR LOGGING
    static Dictionary<(CreatureData, int), bool> flags = new();

    private bool IsBlockedBySynchronization(CreatureData participant)
    {
        CreatureInteractionState state = participantStates[participant];
        int currentAction = state.ActionIndex;
        if (!flags.ContainsKey((participant, currentAction))) flags[(participant, currentAction)] = false;

        if (!IsSynchronizedAction(currentAction))
        {
            if (flags[(participant, currentAction)] != true)
            {
                flags[(participant, currentAction)] = true;
                Debug.Log($"C[{participant.Identity}] -> Action[{StateOf(participant).ActionIndex}] is Un-Synchronized");
            }
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
                if (flags[(participant, currentAction)] != true)
                {
                    flags[(participant, currentAction)] = true;
                    Debug.Log($"C[{participant.Identity}] -> Action[{StateOf(participant).ActionIndex}] is Synchronized. Waiting for C[{other.Identity}] to reach Action[{currentAction}]");
                }
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
    /// Dispatches a <see cref="DriverActionDefinition"/> to a participant.
    /// <para>
    /// The participant is marked as busy until the completion condition
    /// evaluates to <see langword="true"/>.
    /// </para>
    /// <para>
    /// Once completed:
    /// </para>
    /// <para>
    /// - <see cref="CreatureInteractionState.ActionComplete"/> is set.
    /// - <see cref="CreatureInteractionState.ActionIndex"/> is incremented.
    /// </para>
    /// <para>
    /// By default, the action's own completion condition is used, but
    /// it may be overridden for this specific dispatch.
    /// </para>
    /// <para>
    /// This method is intended to be called from
    /// <see cref="UpdateInteraction(CreatureData)"/> implementations.
    /// </para>
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
        Debug.Log($"C[{participant.Identity}]: Action[{participantStates[participant].ActionIndex}] Dispatched");
        participantStates[participant].ActionComplete = false;
        Func<bool> completeExpr = completionConditionOverride ??
            (action.CompletionCondition == null
                ? null
                : () => action.CompletionCondition(participant.Driver));

        // If no complete expression can be resolved the interaction is treated as immediate and it should keep the update function ticking
        continueUpdateTick = completeExpr == null;


        participant.Driver.Execute(
            action.Action,
            () =>
            {
                Debug.Log($"C[{participant.Identity}]: Action[{participantStates[participant].ActionIndex}] COMPLETE");
                participantStates[participant].ActionComplete = true;
                participantStates[participant].ActionIndex++;
            },
            completeExpr

        );
    }

    /// <summary>
    /// <summary>
    /// Dispatches a <see cref="DriverActionDefinition"/> to a participant,
    /// while also enforcing a maximum execution duration.
    /// <para>
    /// The participant is marked as busy until either:
    /// </para>
    /// <para>
    /// - The action's own completion condition succeeds.
    /// - <paramref name="timerSeconds"/> seconds have elapsed.
    /// </para>
    /// <para>
    /// Internally, both conditions are combined using
    /// <see cref="AnyOf(Func{bool}[])"/> so whichever completes first
    /// will finish the action.
    /// </para>
    /// <para>
    /// Once completed:
    /// </para>
    /// <para>
    /// - <see cref="CreatureInteractionState.ActionComplete"/> is set.
    /// - <see cref="CreatureInteractionState.ActionIndex"/> is incremented.
    /// </para>
    /// <para>
    /// This method is intended to be called from
    /// <see cref="UpdateInteraction(CreatureData)"/> implementations.
    /// </para>
    /// </summary>
    /// <param name="participant">
    /// Participant that will execute the action.
    /// </param>
    /// <param name="action">
    /// Action definition to execute.
    /// </param>
    /// <param name="timerSeconds">
    /// Maximum number of seconds the action may run before it is
    /// forcibly considered complete.
    /// </param>
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="action"></param>
    /// <param name="timerSeconds"></param>
    protected void DispatchAction(CreatureData participant, DriverActionDefinition action, float timerSeconds)
    {
        Func<bool> completionConditionOverride =
            action.CompletionCondition == null
                ? After(timerSeconds)
                : AnyOf(() => action.CompletionCondition(participant.Driver), After(timerSeconds));
        DispatchAction(participant, action, completionConditionOverride);
    }

    #endregion
}