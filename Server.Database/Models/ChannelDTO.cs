using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Database.Models
{
    public class ChannelDTO
    {
        public int Id { get; set; }
        public int AppId { get; set; }
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        public int GenericField1 { get; set; }
        public int GenericField2 { get; set; }
        public int GenericField3 { get; set; }
        public int GenericField4 { get; set; }
        public int GenericFieldFilter { get; set; }
    }
}
