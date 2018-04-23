using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApprovalBot.Models
{
    public enum ResponseStatus
    {
        NotResponded,
        Approved,
        Rejected
    }

    public class ApproverInfo
    {
        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }
        [JsonProperty(PropertyName = "response")]
        public ResponseStatus Response { get; set; }
        [JsonProperty(PropertyName = "responseNote")]
        public string ResponseNote { get; set; }
    }
}