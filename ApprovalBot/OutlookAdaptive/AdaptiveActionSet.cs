using AdaptiveCards;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace ApprovalBot.OutlookAdaptive
{
    public class AdaptiveActionSet : AdaptiveElement
    {
        public const string TypeName = "ActionSet";

        public override string Type { get; set; } = TypeName;

        public AdaptiveActionSet(){}

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(AdaptiveHorizontalAlignment), "left")]
        public AdaptiveHorizontalAlignment HorizontalAlignment { get; set; }

        [JsonRequired]
        public List<AdaptiveAction> Actions { get; set; } = new List<AdaptiveAction>();
    }
}