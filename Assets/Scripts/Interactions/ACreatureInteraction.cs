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
    protected static bool DebugBaseClass = false;
    #region COMPILE TIME
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual string DebugInfo { get => ""; }
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
    private float ellapsed;
    #endregion

    public ACreatureInteraction(List<CreatureData> participantList)
    {
        if (participantList == null) return;
        participantList.ForEach(p => pendingJoin.Enqueue(p));
    }

    #region INTERACTION CONTROL FLOW CHECKS

    /// <summary>
    /// Determines conditions for when a creature is allowed to join (be it during initialization or late join)
    /// 
    /// <para> Base implementation: </para>
    ///     ParticipantCount &lt; MaxParticipants
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
    protected virtual bool CheckJoin(CreatureData participant) { return TotalParticipantCount < MaxParticipants; }
    protected virtual bool JoinFinished(CreatureData participant) { return true; }

    /// <summary>
    /// Determines conditions for when a creature must leave (internal "wants to leave" condition, different than CanLeave() which would check if it could)
    /// 
    /// <para> Base implementation: </para>
    ///     ParticipantCount &gt; MinParticipants
    /// </summary>
    /// <param name="participantData"></param>
    /// <returns></returns>
    protected virtual bool CheckLeave(CreatureData participantData) { return TotalParticipantCount < MinParticipants; }

    /// <summary>
    /// Determines the completion condition used when calling Tick on the Leave Phase.
    /// 
    /// <para>Whenever it's true for a given participant it will finalize it's leaving 
    /// and remove it from the interaction (after all participants have processed that tick).</para>
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
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
        if (DebugBaseClass) Debug.Log($"C[{joined.Identity}] JOINED the interaction[{this.Name}]");
    }

    /// <summary>
    /// <para> UpdateInteraction() must guarantee one of these outcomes: </para>
    /// <para> Dispatch an asynchronous action → participant.ActionComplete = false
    ///     *This is applied automatically when dispatching actions (as long as completionCondition is evaluating to false)</para>
    /// <para> Advance synchronously → continueUpdateTick = true
    ///     *This is applied automatically when dispatching immediate actions (completionCondition == null)</para>
    /// <para> Transition phase/end interaction → continueUpdateTick = false
    ///     *This is applied automatically at the start of every updateTick</para>
    /// <para> Manual increment/decrement of ActionIndex → participant.ActionIndex = ...
    ///     </para>
    /// </summary>
    /// <param name="participant"></param>
    protected abstract void UpdateInteraction(CreatureData participant);
    protected virtual void LeaveInteraction(CreatureData participant) { if (DebugBaseClass) Debug.Log($"C[{participant.Identity}] Leaving..."); }

    // Base must be called if overriden to ensure that interaction manager is correctly notified of internal participant abandoment of interaction
    protected virtual void OnParticipantLeft(CreatureData left)
    {
        if (DebugBaseClass) Debug.Log($"C[{left.Identity}] LEFT the interaction[{this.Name}]");
        InteractionManager.NotifyParticipantLeft(this, left);
    }

    protected virtual void PostTick(float deltaTime) { }

    #endregion

    #region PUBLIC METHOD INTERFACE
    // Includes only active participants (discounting pendingJoin participants)
    public IReadOnlyList<CreatureData> ActiveParticipants => participants;
    /// <summary>
    /// Includes participants that are still pending to join
    /// </summary>
    public IReadOnlyList<CreatureData> AllParticipants => participants.Union(pendingJoin).ToList();
    public int TotalParticipantCount => participants.Count + pendingJoin.Count + pendingLeave.Count;
    public int OccupiedParticipantSlotCount => participants.Count + pendingJoin.Count;
    /// <summary>
    /// Time the interaction has gone on for
    /// <para> *May actually be lower than real ellapsed time since the start of the interaction,
    ///  depending on the deltaTime and call frequency to the Tick() function. </para>
    /// </summary>
    public float TotalEllapsedTime => ellapsed;

    public CreatureInteractionState TryReadState(CreatureData creature)
    {
        participantStates.TryGetValue(creature, out var state);
        return state;
    }

    /// <summary>
    /// Condition for determining if an interaction meets the minimum requirements 
    /// to be created and Ticked or not.
    /// </summary>
    /// <returns></returns>
    public virtual bool ValidateInteraction()
    {
        return TotalParticipantCount >= MinParticipants;
    }

    /// <summary>
    /// User configurable preconditions for a creature to be allowed to join into an interaction.
    /// </summary>
    /// <param name="c"></param>
    /// <returns> true when allowed to join, false when otherwise</returns>
    public virtual bool CanJoin(CreatureData c) => true;
    public bool TryJoin(CreatureData participant)
    {
        if (!AllowLateJoining ||
            pendingJoin.Contains(participant) ||
            participants.Contains(participant) ||
            pendingLeave.Contains(participant) ||
            TotalParticipantCount >= MaxParticipants)
        {
            return false;
        }
        if (!CanJoin(participant)) return false;

        pendingJoin.Enqueue(participant);
        return true;
    }

    /// <summary>
    /// User configurable preconditions for a creature to be allowed to leave an interaction.
    /// </summary>
    /// <param name="c"></param>
    /// <returns> true when allowed to join, false when otherwise</returns>
    public virtual bool CanLeave(CreatureData c) => true;
    public bool TryLeave(CreatureData participant)
    {
        if (!AllowEarlyLeaving ||
            pendingLeave.Contains(participant) ||
            pendingJoin.Contains(participant) ||
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
    public void Tick(float deltaTime)
    {
        while (pendingJoin.Count > 0)
        {
            BaseAddParticipant(pendingJoin.Dequeue());
        }

        foreach (var p in participants)
        {
            // if (DebugBaseClass) Debug.Log(
            //     $"[{p.Identity}] " +
            //     $"Phase={participantStates[p].Phase} " +
            //     $"Action={participantStates[p].ActionIndex} " +
            //     $"Complete={participantStates[p].ActionComplete}"
            // );
            switch (participantStates[p].Phase)
            {
                case InteractionPhase.Join:

                    if (!participantStates[p].ActionComplete)
                    {
                        break;
                    }

                    JoinInteraction(p);

                    if (participantStates[p].ActionComplete && JoinFinished(p))
                    {
                        OnParticipantJoined(p);
                        participantStates[p].Phase = InteractionPhase.Update;
                    }

                    break;

                case InteractionPhase.Update:

                    if (CheckLeave(p))
                    {
                        if (DebugBaseClass) Debug.Log($"C[{p.Identity}] met leaving conditions, abandoning Update Phase...");
                        participantStates[p].Phase = InteractionPhase.Leave;
                        break;
                    }

                    if (participantStates[p].ActionComplete)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        int lockedOutCounter = 0;
#endif
                        continueUpdateTick = false;
                        do
                        {
                            if (IsBlockedBySynchronization(p))
                            {
                                break;
                            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            var state = participantStates[p];
                            int previousActionIndex = state.ActionIndex;
                            bool previousActionComplete = state.ActionComplete;
                            bool previousContinueUpdateTick = continueUpdateTick;
#endif

                            UpdateInteraction(p);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            state = participantStates[p];

                            bool progressed =
                                state.ActionIndex != previousActionIndex ||          // Action index changed.
                                state.ActionComplete != previousActionComplete ||    // Async action dispatched.
                                continueUpdateTick == previousContinueUpdateTick && continueUpdateTick == true;    // Synchronous advance or phase transition.

                            if (!progressed)
                            {
                                lockedOutCounter++;
                                if (lockedOutCounter > 20)
                                {
                                    Debug.Break();
                                    throw new InvalidOperationException(
                                        $"{GetType().Name}.{nameof(UpdateInteraction)}() violated the interaction contract.\n\n" +
                                        $"The method returned without advancing the interaction state. This would cause the interaction to stall indefinitely.\n\n" +
                                        $"Every call to {nameof(UpdateInteraction)}() must guarantee one of the following:\n" +
                                        $" • DispatchAction(...) for an asynchronous action.\n" +
                                        $" • DispatchAction(...) for an immediate action.\n" +
                                        $" • Set continueUpdateTick appropriately to transition/end the update phase.\n" +
                                        $" • Manually modify participant.ActionIndex.\n\n" +
                                        $"Check that every execution path in your implementation performs one of these actions.");
                                }
                            }
#endif
                        } while (!CheckLeave(p) && continueUpdateTick);

                    }
                    break;

                case InteractionPhase.Leave:

                    if (!participantStates[p].ActionComplete)
                    {
                        if (DebugBaseClass) Debug.Log(
                            $"[{p.Identity}] Waiting to enter Leave. " +
                            $"DriverBusy={p.Driver.IsBusy}");
                        break;
                    }

                    LeaveInteraction(p);

                    if (participantStates[p].ActionComplete && LeaveFinished(p))
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

        ellapsed += deltaTime;
        PostTick(deltaTime);
    }

    #endregion

    #region HELPER METHODS
    public override string ToString()
    {
        // return guid;
        return $"{Name} participants({TotalParticipantCount}/{MaxParticipants})";
    }
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

    protected bool ParticipantFinishedAction(CreatureData participant, int actionIndex)
    {
        CreatureInteractionState state = participantStates[participant];
        return (state.ActionIndex > actionIndex) || (state.ActionIndex == actionIndex && state.ActionComplete);
    }

    protected bool AllParticipantsPastAction(int actionIndex)
    {
        foreach (var participant in participants)
        {
            if (!ParticipantFinishedAction(participant, actionIndex)) return false;
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
                if (DebugBaseClass) Debug.Log($"C[{participant.Identity}] -> Action[{StateOf(participant).ActionIndex}] is Un-Synchronized");
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
                    if (DebugBaseClass) Debug.Log($"C[{participant.Identity}] -> Action[{StateOf(participant).ActionIndex}] is Synchronized. Waiting for C[{other.Identity}] to reach Action[{currentAction}]");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a completion condition that succeeds after
    /// <paramref name="seconds"/> seconds have elapsed.
    /// <para> Useful for adding durations to actions.
    /// Can be used to make immedate actions asnychronous
    /// by deferring their completion. </para>
    /// <para> Example: </para>
    ///     DispatchAction(participant, DriverActions.SetBool("IsDancing", true), After(5f));
    /// </summary>
    protected Func<bool> After(float seconds)
    {
        float endTime = Time.time + seconds;

        return () => Time.time >= endTime;
    }

    /// <summary>
    /// Creates a completion condition that succeeds when
    /// any supplied condition succeeds.
    /// <para> i.e logical OR combination of all conditions </para>
    /// </summary>
    protected Func<bool> AnyOf(params Func<bool>[] conditions)
    {
        return () => conditions.Any(c => c());
    }

    /// <summary>
    /// Creates a completion condition that succeeds when
    /// any supplied condition succeeds.
    /// <para> i.e logical AND combination of all conditions </para>
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
        if (DebugBaseClass) Debug.Log($"C[{participant.Identity}]: Action[{participantStates[participant].ActionIndex}] Dispatched");
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
                if (DebugBaseClass) Debug.Log($"C[{participant.Identity}]: Action[{participantStates[participant].ActionIndex}] COMPLETE");
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