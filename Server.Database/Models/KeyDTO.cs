using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Database.Models
{
    public class ServerFlagsDTO
    {
        public MaintenanceDTO MaintenanceMode { get; set; }
    }

    public class MaintenanceDTO
    {
        public bool IsActive { get; set; }
        public DateTime? FromDt { get; set; }
        public DateTime? ToDt { get; set; }
    }
}
