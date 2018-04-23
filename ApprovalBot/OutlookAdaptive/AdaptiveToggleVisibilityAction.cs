using AdaptiveCards;
using Newtonsoft.Json;

namespace ApprovalBot.OutlookAdaptive
{
    public class AdaptiveToggleVisibilityAction : AdaptiveAction
    {
        public const string TypeName = "Action.ToggleVisibility";

        public override string Type { get; set; } = TypeName;

        [JsonRequired]
        public string[] TargetElements { get; set; }
    }
}