using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApprovalBot.Models
{
    public static class CardActionTypes
    {
        public static string SelectFile = "selectFile";
        public static string WrongFile = "wrongFile";
        public static string SendApprovalRequest = "sendApproval";
        public static string SelectApproval = "selectApproval";
    }

    public class ActionData
    {
        public string CardAction { get; set; }
        public string SelectedFile { get; set; }
        public string Approvers { get; set; }
        public string SelectedApproval { get; set; }

        public static ActionData Parse(object obj)
        {
            return ((JToken)obj).ToObject<ActionData>();
        }
    }
}