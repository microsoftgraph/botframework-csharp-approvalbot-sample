using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApprovalBot.Models
{
    public class Approval
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "requestor")]
        public string Requestor { get; set; }
        [JsonProperty(PropertyName = "file")]
        public ApprovalFileInfo File { get; set; }
        [JsonProperty(PropertyName = "approvers")]
        public List<ApproverInfo> Approvers { get; set; }
        [JsonProperty(PropertyName = "requestDate")]
        public DateTimeOffset RequestDate { get; set; }
    }
}