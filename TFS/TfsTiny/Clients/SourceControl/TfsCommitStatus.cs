using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.TFS.TfsTiny.Clients.SourceControl
{
    internal class TfsCommitStatus
    {
        public int? ChangeSetId { get; set; }
        public string Error { get; set; }
    }
}
