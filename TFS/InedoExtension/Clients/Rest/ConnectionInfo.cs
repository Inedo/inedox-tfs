namespace Inedo.Extensions.TFS.Clients.Rest
{
    internal class ConnectionInfo : IVsoConnectionInfo
    {
        public ConnectionInfo(string username, string password, string projectCollectionUrl, string domain)
        {

            this.UserName = username;
            this.PasswordOrToken = password;
            this.Domain = domain;
            this.TeamProjectCollectionUrl = projectCollectionUrl;
        }

        public string UserName { get; }
        public string PasswordOrToken { get; }
        public string Domain { get; }
        public string TeamProjectCollectionUrl { get; }
    }
}
