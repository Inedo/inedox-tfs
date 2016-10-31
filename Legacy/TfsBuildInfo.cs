using System;
using Microsoft.TeamFoundation.Build.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [Serializable]
    internal sealed class TfsBuildInfo
    {
        public TfsBuildInfo(IBuildDetail buildDetail)
        {
            this.BuildNumber = buildDetail.BuildNumber;
            this.DropLocation = buildDetail.DropLocation;
        }

        public string BuildNumber { get; set; }
        public string DropLocation { get; set; }
    }
}
