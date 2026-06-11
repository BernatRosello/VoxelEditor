using System.Runtime.ConstrainedExecution;

public sealed class ParticipantData
{
    public CreatureIdentity Identity;
    public ActionDriver Driver;
    public ACreatureInteraction CurrentInteraction { get; set; }
    // public int Phase;

    public int ActionIndex;
    public bool ActionComplete;

    public ParticipantData(
        ActionDriver driver, CreatureIdentity identity)
    {
        Driver = driver;
        Identity = identity;
    }
}