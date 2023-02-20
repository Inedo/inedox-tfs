using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Extensions.TFS.VisualStudioOnline.Model;
using Newtonsoft.Json;

namespace Inedo.Extensions.TFS.Clients.Rest
{
    public sealed class QueryString
    {
        public static QueryString Default = new();

        public string ApiVersion { get; set; } = "2.0";
        public string Expand { get; set; }

        public string BuildNumber { get; set; }
        public int Definition { get; set; }
        public int? Top { get; set; }
        public string ResultFilter { get; set; }
        public string StatusFilter { get; set; }

        public string SearchCriteriaItemPath { get; set; }

        public IEnumerable<int> Ids { get; set; }
        public string Timeframe { get; set; }

        public override string ToString()
        {
            var buffer = new StringBuilder(1024);
            buffer.Append('?');
            if (this.ApiVersion != null)
                buffer.AppendFormat("api-version={0}&", this.ApiVersion);
            if (this.BuildNumber != null)
                buffer.AppendFormat("buildNumber={0}&", this.BuildNumber);
            if (this.Definition != 0)
                buffer.AppendFormat("definitions={0}&", this.Definition);
            if (this.Top != null)
                buffer.AppendFormat("$top={0}&", this.Top);
            if (this.ResultFilter != null)
                buffer.AppendFormat("resultFilter={0}&", this.ResultFilter);
            if (this.StatusFilter != null)
                buffer.AppendFormat("statusFilter={0}&", this.StatusFilter);
            if (this.Ids != null)
                buffer.AppendFormat("ids={0}&", string.Join(",", this.Ids));
            if (this.Timeframe != null)
                buffer.AppendFormat("$timeframe={0}&", this.Timeframe);
            if (this.Expand != null)
                buffer.AppendFormat("$expand={0}&", this.Expand);
            if (this.SearchCriteriaItemPath != null)
                buffer.AppendFormat("searchCriteria.itemPath={0}&", Uri.EscapeDataString(this.SearchCriteriaItemPath));

            return buffer.ToString().TrimEnd('?', '&');
        }
    }

    public sealed class TfsRestApi
    {
        private readonly IVsoConnectionInfo connectionInfo;
        private readonly ILogSink log;

        public TfsRestApi(IVsoConnectionInfo connectionInfo, ILogSink log)
        {
            this.connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            this.log = log;
        }

        public async Task<GetChangesetResponse[]> GetChangesetsAsync(string project, string path, CancellationToken cancellationToken)
        {
            var query = new QueryString { Top = 1, SearchCriteriaItemPath = path };
            var response = await InvokeAsync<GetChangesetsResponse>(HttpMethod.Get, project, "tfvc/changesets", query, cancellationToken).ConfigureAwait(false);
            return response.value;
        }


        private async Task<T> InvokeAsync<T>(HttpMethod method, string project, string relativeUrl, QueryString query, CancellationToken cancellationToken)
        {
            string apiBaseUrl;
            if (string.IsNullOrEmpty(project))
                apiBaseUrl = $"{this.connectionInfo.TeamProjectCollectionUrl}/_apis/";
            else
                apiBaseUrl = $"{this.connectionInfo.TeamProjectCollectionUrl}/{Uri.EscapeDataString(project)}/_apis/";

            string url = apiBaseUrl + relativeUrl + query.ToString();
            var http = SDK.CreateHttpClient();
            using var request = new HttpRequestMessage(method, url);

            request.Headers.UserAgent.ParseAdd("BuildMasterTFSExtension/" + typeof(TfsRestApi).Assembly.GetName().Version.ToString());

            this.log?.LogDebug($"Invoking TFS REST API {method} request to URL: {url}");

            SetCredentials(request);

            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var resp = await response.Content.ReadAsStringAsync();
                log.LogError($"Error status code ({response.StatusCode}) received while checking path {resp}");
                throw new TfsRestException((int)response.StatusCode, resp, null);
            }
            return await DeserializeJsonAsync<T>(response);


        }

        public static async Task<T> DeserializeJsonAsync<T>(HttpResponseMessage response)
        {
            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new JsonTextReader(new StreamReader(responseStream));
            return JsonSerializer.CreateDefault().Deserialize<T>(reader);
        }

        private void SetCredentials(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(this.connectionInfo.UserName))
            {
                string fullName = string.IsNullOrEmpty(this.connectionInfo.Domain) ? this.connectionInfo.UserName : $"{this.connectionInfo.Domain}\\{this.connectionInfo.UserName}";
                this.log?.LogDebug($"Authenticating as '{fullName}'...");
                request.Headers.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(fullName + ":" + this.connectionInfo.PasswordOrToken)));
            }
            else
            {
                this.log?.LogDebug("No username specified, no authorization header will be sent.");
            }
        }
    }

    public sealed class TfsRestException : Exception
    {
        public TfsRestException(int statusCode, string message, Exception inner)
            : base(message, inner)
        {
            this.StatusCode = statusCode;
        }

        public int StatusCode { get; }

        public string FullMessage => $"The server returned an error ({this.StatusCode}): {this.Message}";

    }
}
