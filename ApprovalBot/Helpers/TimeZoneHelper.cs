using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApprovalBot.Helpers
{
    public static class TimeZoneHelper
    {
        public static string GetAdaptiveDateTimeString(DateTimeOffset dateTimeOffset)
        {
            var rfc3389String = $"{dateTimeOffset.UtcDateTime.ToString("s")}Z";

            // Returns string like
            // {{DATE(2018-04-26T07:00:00Z,SHORT)}} {{TIME(2018-04-26T07:00:00Z)}}
            // See docs at https://docs.microsoft.com/en-us/adaptive-cards/create/textfeatures#datetime-function-rules
            return $"{{{{DATE({rfc3389String},SHORT)}}}} {{{{TIME({rfc3389String})}}}}";
        }
    }
}