using System.Runtime.ConstrainedExecution;
public sealed class CreatureData
{
    public CreatureIdentity Identity;
    public ActionDriver Driver;
    public CreatureData(
        ActionDriver driver, CreatureIdentity identity)
    {
        Driver = driver;
        Identity = identity;
    }
}