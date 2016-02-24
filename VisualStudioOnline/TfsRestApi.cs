using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        public GetBuildResponse GetBuild(int buildId)
        {
            return this.Invoke<GetBuildResponse>("GET", $"build/builds/{buildId}", QueryString.Default);
        }

        public GetBuildResponse[] GetBuilds(string buildNumber = null, string resultFilter = null, string statusFilter = null, int? top = null)
        {
            var query = new QueryString() { BuildNumber = buildNumber, ResultFilter = resultFilter, StatusFilter = statusFilter, Top = top };

            return this.Invoke<GetBuildsResponse>("GET", "build/builds", query).value;
        }

        public GetBuildDefinitionResponse[] GetBuildDefinitions()
        {
            return this.Invoke<GetBuildDefinitionsResponse>("GET", "build/definitions", QueryString.Default).value;
        }

        public GetBuildResponse QueueBuild(int definitionId)
        {
            return this.Invoke<GetBuildResponse>(
                "POST", 
                "build/builds", 
                QueryString.Default, 
                new
                {
                    definition = new { id = definitionId }
                }
            );
        }

        public void DownloadArtifact(int buildId, string artifactName, string filePath)
        {
            var response = this.Invoke<GetBuildArtifactsResponse>("GET", $"build/builds/{buildId}/artifacts", QueryString.Default);
            var artifact = response.value.FirstOrDefault(a => string.Equals(a.name, artifactName, StringComparison.OrdinalIgnoreCase));
            if (artifact == null)
                throw new InvalidOperationException($"Artifact \"{artifactName}\" could not be found for build ID # {buildId}.");

            string url = artifact.resource.downloadUrl;
            this.DownloadFile(url, filePath);
        }

        private object Invoke(string method, string relativeUrl, QueryString query, object data = null)
        {
            return this.Invoke<object>(method, relativeUrl, query, data);
        }

        private void DownloadFile(string url, string filePath)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.UserAgent = "BuildMasterTFSExtension/" + typeof(TfsRestApi).Assembly.GetName().Version.ToString();
            request.Method = "GET";
            
            if (!string.IsNullOrEmpty(this.UserName))
            {
                request.Credentials = new NetworkCredential(this.UserName, this.Password);
                request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + ":" + this.Password));
            }

            try
            {
                using (var response = request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var fileStream = FileEx.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    responseStream.CopyTo(fileStream);
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

        private T Invoke<T>(string method, string relativeUrl, QueryString query, object data = null)
        {
            string url = this.apiBaseUrl + relativeUrl + query.ToString();

            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.UserAgent = "BuildMasterTFSExtension/" + typeof(TfsRestApi).Assembly.GetName().Version.ToString();
            request.ContentType = "application/json";
            request.Method = method;
            if (data != null)
            {
                using (var requestStream = request.GetRequestStream())
                using (var writer = new StreamWriter(requestStream, InedoLib.UTF8Encoding))
                {
                    InedoLib.Util.JavaScript.WriteJson(writer, data);
                }
            }

            if (!string.IsNullOrEmpty(this.UserName))
            {
                request.Credentials = new NetworkCredential(this.UserName, this.Password);
                request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + ":" + this.Password));
            }

            try
            {
                using (var response = request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var js = new JavaScriptSerializer();
                    string s = reader.ReadToEnd();
                    return js.Deserialize<T>(s);
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
    }
}
