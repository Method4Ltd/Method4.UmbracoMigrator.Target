namespace Method4.UmbracoMigrator.Target.Core.Models.Exceptions
{
    [Serializable]
    public class MigrationFailedException : Exception
    {
        public MigrationFailedException(int phaseNumber, Exception innerException)
            : base(String.Format("Migration failed on Phase {0}. {1}", phaseNumber, innerException.Message), innerException) { }
    }
}