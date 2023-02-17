using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Inedo.TFS.TfsTiny;
using Inedo.TFS.VisualStudioOnline.Model;
using Newtonsoft.Json;

namespace Inedo.TFS.Clients.Rest
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

        public async Task<GetChangesetResponse[]> GetChangesetsAsync(string project, string path)
        {
            var query = new QueryString { Top = 1, SearchCriteriaItemPath = path };

            var response = await this.InvokeAsync<GetChangesetsResponse>("GET", project, "tfvc/changesets", query).ConfigureAwait(false);
            return response.value;
        }


        private async Task<T> InvokeAsync<T>(string method, string project, string relativeUrl, QueryString query, object data = null, string contentType = "application/json")
        {
            string apiBaseUrl;
            if (string.IsNullOrEmpty(project))
                apiBaseUrl = $"{this.connectionInfo.TeamProjectCollectionUrl}/_apis/";
            else
                apiBaseUrl = $"{this.connectionInfo.TeamProjectCollectionUrl}/{Uri.EscapeUriString(project)}/_apis/";

            string url = apiBaseUrl + relativeUrl + query.ToString();

            var request = WebRequest.Create(url);
            if (request is HttpWebRequest httpRequest)
                httpRequest.UserAgent = "BuildMasterTFSExtension/" + typeof(TfsRestApi).Assembly.GetName().Version.ToString();
            request.ContentType = contentType;
            request.Method = method;

            this.log?.LogDebug($"Invoking TFS REST API {method} request ({contentType}) to URL: {url}");

            if (data != null)
            {
                using var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false);
#warning Was InedoLib.Utf8Encoding.  Reference InedoLib if becomes an issue
                using var writer = new StreamWriter(requestStream, Encoding.UTF8);
                JsonSerializer.CreateDefault().Serialize(writer, data);
            }

            this.SetCredentials(request);

            try
            {
                using var response = await request.GetResponseAsync().ConfigureAwait(false);
                return DeserializeJson<T>(response);
            }
            catch (WebException ex) when (ex.Response != null)
            {
                throw TfsRestException.Wrap(ex, url);
            }
        }

        public static T DeserializeJson<T>(WebResponse response)
        {
            using var responseStream = response.GetResponseStream();
            using var reader = new JsonTextReader(new StreamReader(responseStream));
            return JsonSerializer.CreateDefault().Deserialize<T>(reader);
        }

        private void SetCredentials(WebRequest request)
        {
            if (!string.IsNullOrEmpty(this.connectionInfo.UserName))
            {
                string fullName = string.IsNullOrEmpty(this.connectionInfo.Domain) ? this.connectionInfo.UserName : $"{this.connectionInfo.Domain}\\{this.connectionInfo.UserName}";
                this.log?.LogDebug($"Authenticating as '{fullName}'...");
                request.Credentials = new NetworkCredential(fullName, this.connectionInfo.PasswordOrToken);

                // local instances of TFS 2015 can return file:/// URLs which result in FileWebRequest instances that do not allow headers
                if (request is HttpWebRequest)
                {
                    request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(fullName + ":" + this.connectionInfo.PasswordOrToken));
                }
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

        public static TfsRestException Wrap(WebException ex, string url)
        {
            var response = (HttpWebResponse)ex.Response;
            try
            {
                var error = TfsRestApi.DeserializeJson<Error>(response);
                return new TfsRestException((int)response.StatusCode, error.message, ex);
            }
            catch
            {
                using var responseStream = ex.Response.GetResponseStream();
                try
                {
                    string errorText = new StreamReader(responseStream).ReadToEnd();
                    return new TfsRestException((int)response.StatusCode, errorText, ex);
                }
                catch
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                        return new TfsRestException((int)response.StatusCode, "Verify that the credentials used to connect are correct.", ex);
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return new TfsRestException(404, $"Verify that the URL in the operation or credentials is correct (resolved to '{url}').", ex);

                    throw ex;
                }
            }
        }
    }
}
