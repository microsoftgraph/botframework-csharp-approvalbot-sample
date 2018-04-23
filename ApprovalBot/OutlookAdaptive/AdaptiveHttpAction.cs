using AdaptiveCards;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ApprovalBot.OutlookAdaptive
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum AdaptiveHttpActionMethod
    {
        GET,
        POST
    }

    public class AdaptiveHttpActionHeader
    {
        [JsonRequired]
        public string Name { get; set; }

        [JsonRequired]
        public string Value { get; set; }
    }

    public class AdaptiveHttpAction : AdaptiveAction
    {
        public const string TypeName = "Action.Http";

        public override string Type { get; set; } = TypeName;

        [JsonRequired]
        public AdaptiveHttpActionMethod Method { get; set; }

        [JsonRequired]
        public Uri Url { get; set; }

        [DefaultValue(null)]
        public List<AdaptiveHttpActionHeader> Headers { get; set; }

        [DefaultValue(null)]
        public string Body { get; set; }
    }
}