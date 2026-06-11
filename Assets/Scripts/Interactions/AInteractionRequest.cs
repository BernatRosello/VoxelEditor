using System.Collections.Generic;
using System.Linq;

public abstract class AInteractionRequest
{
    public IReadOnlyList<CreatureIdentity> Targets { get; }

    protected AInteractionRequest(IEnumerable<CreatureIdentity> targets)
    {
        Targets = targets.ToList();
    }

    public abstract ACreatureInteraction GetInteraction();

    public abstract ACreatureInteraction CreateInteraction(
        IEnumerable<ParticipantData> participants);
}

public abstract class AInteractionRequest<TInteractionType, TParams> : AInteractionRequest
    where TInteractionType : ACreatureInteraction, new()
    where TParams : AInteractionParams
{
    public TParams parameters;

    protected AInteractionRequest(TParams parameters, IEnumerable<CreatureIdentity> targets) : base(targets)
    {
        this.parameters = parameters;
    }

    public override ACreatureInteraction GetInteraction() { return new TInteractionType(); }
}