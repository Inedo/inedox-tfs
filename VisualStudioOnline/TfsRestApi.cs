using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline.Model;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    internal sealed class QueryString
    {
        public static QueryString Default = new QueryString();

        public string ApiVersion { get; set; } = "2.0";
        public string BuildNumber { get; set; }
        public int Definition { get; set; }
        public int? Top { get; set; }
        public string ResultFilter { get; set; }
        public string StatusFilter { get; set; }

        public override string ToString()
        {
            var buffer = new StringBuilder(100);
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

            return buffer.ToString().TrimEnd('?', '&');
        }
    }

    internal sealed class TfsRestApi
    {
        private string host;
        private string apiBaseUrl;

        public TfsRestApi(string host, string project)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrEmpty(project))
                throw new ArgumentException("A team project is required for the TFS Rest API.", nameof(project));

            this.host = host.TrimEnd('/');
            this.apiBaseUrl = $"{this.host}/{Uri.EscapeUriString(project)}/_apis/";
        }
        
        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task<GetBuildResponse> GetBuildAsync(int buildId)
        {
            return await this.InvokeAsync<GetBuildResponse>("GET", $"build/builds/{buildId}", QueryString.Default).ConfigureAwait(false);
        }

        public async Task<GetBuildResponse[]> GetBuildsAsync(string buildNumber = null, string resultFilter = null, string statusFilter = null, int? top = null)
        {
            var query = new QueryString() { BuildNumber = buildNumber, ResultFilter = resultFilter, StatusFilter = statusFilter, Top = top };

            var response = await this.InvokeAsync<GetBuildsResponse>("GET", "build/builds", query).ConfigureAwait(false);
            return response.value;
        }

        public async Task<GetBuildResponse[]> GetBuildsAsync(int buildDefinition, string buildNumber = null, string resultFilter = null, string statusFilter = null, int? top = null)
        {
            var query = new QueryString() { Definition = buildDefinition, BuildNumber = buildNumber, ResultFilter = resultFilter, StatusFilter = statusFilter, Top = top };

            var response = await this.InvokeAsync<GetBuildsResponse>("GET", "build/builds", query).ConfigureAwait(false);
            return response.value;
        }

        public async Task<GetBuildDefinitionResponse[]> GetBuildDefinitionsAsync()
        {
            var response = await this.InvokeAsync<GetBuildDefinitionsResponse>("GET", "build/definitions", QueryString.Default).ConfigureAwait(false);
            return response.value;
        }

        public async Task<GetBuildResponse> QueueBuildAsync(int definitionId)
        {
            return await this.InvokeAsync<GetBuildResponse>(
                "POST", 
                "build/builds", 
                QueryString.Default, 
                new
                {
                    definition = new { id = definitionId }
                }
            ).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task DownloadArtifactAsync(int buildId, string artifactName, string filePath)
        {
            var response = await this.InvokeAsync<GetBuildArtifactsResponse>("GET", $"build/builds/{buildId}/artifacts", QueryString.Default).ConfigureAwait(false);
            var artifact = response.value.FirstOrDefault(a => string.Equals(a.name, artifactName, StringComparison.OrdinalIgnoreCase));
            if (artifact == null)
                throw new InvalidOperationException($"Artifact \"{artifactName}\" could not be found for build ID # {buildId}.");

            string url = artifact.resource.downloadUrl;
            await this.DownloadFileAsync(url, filePath).ConfigureAwait(false);
        }

        private object Invoke(string method, string relativeUrl, QueryString query, object data = null)
        {
            return this.InvokeAsync<object>(method, relativeUrl, query, data);
        }

        private async System.Threading.Tasks.Task DownloadFileAsync(string url, string filePath)
        {
            var request = WebRequest.Create(url);
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
                httpRequest.UserAgent = "BuildMasterTFSExtension/" + typeof(TfsRestApi).Assembly.GetName().Version.ToString();
            request.Method = "GET";
            
            this.SetCredentials(request);

            try
            {
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                using (var responseStream = response.GetResponseStream())
                using (var fileStream = FileEx.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await responseStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    throw;

                using (var responseStream = ex.Response.GetResponseStream())
                {
                    string message;
                    try
                    {
                        message = new StreamReader(responseStream).ReadToEnd();
                    }
                    catch
                    {
                        throw ex;
                    }

                    throw new Exception(message, ex);
                }
            }
        }

        private async Task<T> InvokeAsync<T>(string method, string relativeUrl, QueryString query, object data = null)
        {
            string url = this.apiBaseUrl + relativeUrl + query.ToString();

            var request = WebRequest.Create(url);
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
                httpRequest.UserAgent = "BuildMasterTFSExtension/" + typeof(TfsRestApi).Assembly.GetName().Version.ToString();
            request.ContentType = "application/json";
            request.Method = method;
            if (data != null)
            {
                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                using (var writer = new StreamWriter(requestStream, InedoLib.UTF8Encoding))
                {
                    InedoLib.Util.JavaScript.WriteJson(writer, data);
                }
            }

            this.SetCredentials(request);

            try
            {
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var js = new JavaScriptSerializer();
                    string s = reader.ReadToEnd();
                    return js.Deserialize<T>(s);
                }
            }
            catch (WebException ex) when (ex.Response != null)
            {
                var httpResponse = ex.Response as HttpWebResponse;
                if (httpResponse != null)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                        throw new InvalidOperationException("TFS server returned 401 Unauthorized - verify that the credentials used to connect are correct.");
                    if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
                        throw new InvalidOperationException("TFS server returned 403 Forbidden - verify that the credentials used to connect are correct.");
                }

                using (var responseStream = ex.Response.GetResponseStream())
                {
                    string message;
                    try
                    {
                        message = await new StreamReader(responseStream).ReadToEndAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        throw ex;
                    }

                    throw new Exception(message, ex);
                }
            }
        }

        private void SetCredentials(WebRequest request)
        {
            if (!string.IsNullOrEmpty(this.UserName))
            {
                request.Credentials = new NetworkCredential(this.UserName, this.Password);

                // local instances of TFS 2015 can return file:/// URLs which result in FileWebRequest instances that do not allow headers
                if (request is HttpWebRequest)
                {
                    request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + ":" + this.Password));
                }
            }
        }
    }
}
