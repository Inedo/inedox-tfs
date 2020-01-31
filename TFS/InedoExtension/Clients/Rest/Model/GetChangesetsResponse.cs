﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Visual Studio via: 
//     Edit > Paste Special > Paste JSON as Classes
//     
//     JSON generated from: https://dev.azure.com/inedo/ProfitCalcTfvc/_apis/tfvc/changesets?$top=2&searchCriteria.itemPath=%24%2F&api-version=2.0
//
// </auto-generated>
//------------------------------------------------------------------------------

using System;

namespace Inedo.Extensions.TFS.VisualStudioOnline.Model
{
    class GetChangesetsResponse
    {
        public int count { get; set; }
        public GetChangesetResponse[] value { get; set; }
    }

    public class GetChangesetResponse
    {
        public int changesetId { get; set; }
        public string url { get; set; }
        public User author { get; set; }
        public User checkedInBy { get; set; }
        public DateTime createdDate { get; set; }
        public string comment { get; set; }
    }

    public class User
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
    }
}
