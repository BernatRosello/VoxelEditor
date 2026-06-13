using System.Collections.Generic;
using System.Linq;

public abstract class AInteractionRequest
{
    public IReadOnlyList<CreatureIdentity> Targets { get; }
    public int RequestAttemptsLeft { get => requestAttemptsLeft; set => requestAttemptsLeft = value; }

    private int requestAttemptsLeft;

    protected AInteractionRequest(IEnumerable<CreatureIdentity> targets, int requestAttempts = 1)
    {
        Targets = targets.ToList();
        RequestAttemptsLeft = requestAttempts;
    }

    public abstract ACreatureInteraction CreateInteraction(IEnumerable<CreatureData> participants);
}

public abstract class AInteractionRequest<TInteractionType, TParams> : AInteractionRequest
    where TInteractionType : ACreatureInteraction
    where TParams : AInteractionParams
{
    protected readonly TParams parameters;
    private readonly System.Func<TParams, List<CreatureData>, TInteractionType> factory;

    protected AInteractionRequest(TParams parameters, IEnumerable<CreatureIdentity> targets,
        System.Func<TParams, List<CreatureData>, TInteractionType> factory, int requestAttempts = 1)
        : base(targets, requestAttempts)
    {
        this.parameters = parameters;
        this.factory = factory;
    }

    public override ACreatureInteraction CreateInteraction(IEnumerable<CreatureData> participants)
    {
        return factory(parameters, participants.ToList());
    }

}