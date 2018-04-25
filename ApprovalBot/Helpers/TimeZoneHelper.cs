using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApprovalBot.Helpers
{
    public static class TimeZoneHelper
    {
        public static string GetDateTimeStringInTimeZone(DateTimeOffset dateTimeOffset, string timeZone)
        {
            var targetTZ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);

            var convertedOffset = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTimeOffset, timeZone);
            return convertedOffset.ToString("MMMM dd, yyyy h:mm tt");
        }
    }
}