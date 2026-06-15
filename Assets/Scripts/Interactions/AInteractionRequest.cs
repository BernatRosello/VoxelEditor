using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Immutable description of an interaction creation request.
///
/// Requests are consumed by <see cref="InteractionManager"/> and are responsible
/// for carrying both the desired participant identities and any interaction
/// specific configuration required to instantiate the interaction.</br></br>
///
/// Requests may optionally be retried multiple times if their creation
/// preconditions are not currently satisfied.
///
/// Derived classes should typically only supply parameters and a factory
/// function rather than overriding behavior.
/// </summary>
public abstract class AInteractionRequest
{
    /// <summary>
    /// Creature identities that should participate in the interaction.</br></br>
    ///
    /// The <see cref="InteractionManager"/> will resolve these into
    /// <see cref="CreatureData"/> instances before attempting creation.
    /// </summary>
    public IReadOnlyList<CreatureIdentity> Targets { get; }

    /// <summary>
    /// Remaining creation attempts for this request.</br></br>
    ///
    /// Failed requests may be requeued until this value reaches zero.
    /// </summary>
    public int RequestAttemptsLeft { get => requestAttemptsLeft; set => requestAttemptsLeft = value; }
    private int requestAttemptsLeft;

    /// <summary>
    /// Creates a new interaction request.
    /// </summary>
    /// <param name="targets">
    /// Identities of the creatures that should participate.
    /// </param>
    /// <param name="requestAttempts">
    /// Maximum number of creation attempts before the request is discarded.
    /// </param>
    protected AInteractionRequest(IEnumerable<CreatureIdentity> targets, int requestAttempts = 1)
    {
        Targets = targets.ToList();
        RequestAttemptsLeft = requestAttempts;
    }

    /// <summary>
    /// Creates the interaction instance using the supplied participants.</br></br>
    ///
    /// This method is called internally by <see cref="InteractionManager"/>
    /// after participant resolution and availability checks have been performed.
    /// </summary>
    /// <param name="participants">
    /// Participants selected for the interaction.
    /// </param>
    public abstract ACreatureInteraction CreateInteraction(IEnumerable<CreatureData> participants);
}

/// <summary>
/// Generic request implementation used by most interactions.</br></br>
///
/// This class stores a parameter object and a factory function that
/// automatically instantiate the interaction type.</br></br>
///
/// Typical derived request implementations only need to forward their
/// constructor arguments to the base constructor.
///
/// Example:
///
///     public class ConversationRequest
///         : AInteractionRequest&lt;ConversationInteraction, ConversationParams&gt;
/// </summary>
/// <typeparam name="TInteractionType">
/// Concrete interaction type to instantiate.
/// </typeparam>
/// <typeparam name="TParams">
/// Parameter type consumed by the interaction.
/// </typeparam>
public abstract class AInteractionRequest<TInteractionType, TParams> : AInteractionRequest
    where TInteractionType : ACreatureInteraction
    where TParams : AInteractionParams
{
    /// <summary>
    /// Immutable parameter object passed to the interaction constructor.
    /// </summary>
    protected readonly TParams parameters;
    private readonly System.Func<TParams, List<CreatureData>, TInteractionType> factory;

    /// <summary>
    /// Creates a generic interaction request.
    /// </summary>
    /// <param name="parameters">
    /// Interaction specific parameters.
    /// </param>
    /// <param name="targets">
    /// Requested participants.
    /// </param>
    /// <param name="factory">
    /// Function responsible for constructing the interaction instance.
    /// </param>
    /// <param name="requestAttempts">
    /// Maximum amount of creation attempts.
    /// </param>
    protected AInteractionRequest(TParams parameters, IEnumerable<CreatureIdentity> targets,
        Func<TParams, List<CreatureData>, TInteractionType> factory, int requestAttempts = 1)
        : base(targets, requestAttempts)
    {
        this.parameters = parameters;
        this.factory = factory;
    }

    /// <inheritdoc/>
    public override ACreatureInteraction CreateInteraction(IEnumerable<CreatureData> participants)
    {
        return factory(parameters, participants.ToList());
    }

}