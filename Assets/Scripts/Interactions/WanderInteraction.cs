using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;

public class WanderingParams : AInteractionParams
{
    public float minDistance;
    public float maxDistance;
    public float frequency;
    public float frequencyVariance;
    public float turnChance;
}

public class WanderingRequest : AInteractionRequest<WanderingInteraction, WanderingParams>
{
    public WanderingRequest(WanderingParams parameters, IEnumerable<CreatureIdentity> targets)
        : base(parameters, targets,
            // Request Factory
            (p, participants) => new WanderingInteraction(p, participants))
    { }
}

public class WanderingInteraction : ACreatureInteraction<WanderingParams>
{
    public WanderingInteraction(WanderingParams parameters, List<CreatureData> participants) : base(parameters, participants)
    { }

    public override string Name => "UserMove";
    public override string Description => "Initiator will move creature to target position";
    public override int MinParticipants => 1;
    public override int MaxParticipants => 1;
    public override bool AllowLateJoining => false;
    public override bool AllowEarlyLeaving => true;
    public override InteractionPriority Priority => 0;
    public override bool InterruptLowerPriorityInteractions => false;

    protected override bool CheckLeave(CreatureData participantData)
    {
        bool result = Random.Range(0, 100) == 0;

        // Debug.Log(
        //     $"{participantData.Identity} leave check = {result} " +
        //     $"index={StateOf(participantData).ActionIndex} " +
        //     $"complete={StateOf(participantData).ActionComplete}");

        return result;
    }

    protected override void UpdateInteraction(CreatureData p)
    {
        switch (StateOf(p).ActionIndex)
        {
            // TODO
        }
    }
}