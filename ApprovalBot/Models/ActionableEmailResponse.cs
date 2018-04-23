using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApprovalBot.Models
{
    public class ActionableEmailResponse
    {
        public string UserEmail { get; set; }
        public string ApprovalId { get; set; }
        public string Response { get; set; }
        public string Notes { get; set; }
    }
}