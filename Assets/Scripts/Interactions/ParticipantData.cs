using System.Runtime.ConstrainedExecution;

public enum InteractionState
{
    Joining,
    Starting,
    Update,
    Leaving,
    Abort
}

public sealed class ParticipantData
{
    public CreatureIdentity Identity;
    public ActionDriver Driver;
    public ACreatureInteraction CurrentInteraction { get; set; }
    public InteractionState State;

    public int ActionIndex;
    public bool ActionComplete;

    public ParticipantData(
        ActionDriver driver, CreatureIdentity identity)
    {
        Driver = driver;
        Identity = identity;
    }
}