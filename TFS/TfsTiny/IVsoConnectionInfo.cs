namespace Inedo.TFS
{
    public interface IVsoConnectionInfo
    {
        string UserName { get; }
        string PasswordOrToken { get; }
        string Domain { get; }
        string TeamProjectCollectionUrl { get; }
    }
}
