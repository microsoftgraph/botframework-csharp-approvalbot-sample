using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApprovalBot.Models
{
    public class ApprovalFileInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SharingUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}