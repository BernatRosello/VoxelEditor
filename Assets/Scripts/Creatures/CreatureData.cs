using System.Runtime.ConstrainedExecution;
public sealed class CreatureData
{
    public CreatureIdentity Identity;
    public CreatureBehaviourStats Stats;
    public ActionDriver Driver;
    public CreatureData(ActionDriver driver, CreatureBehaviourStats stats, CreatureIdentity identity)
    {
        Driver = driver;
        Stats = stats;
        Identity = identity;
    }
}