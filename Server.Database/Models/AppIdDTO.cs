using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Database.Models
{
    public class AppIdDTO
    {
        public string Name { get; set; }
        public List<int> AppIds { get; set; }
    }
}
