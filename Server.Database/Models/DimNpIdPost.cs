using System;

namespace Server.Database.Models
{
    public partial class DimNpIdPost
    {
        public int Id { get; set; }

        public byte[] data { get; set; }
        public byte term { get; set; }
        public byte[] dummy;

        public byte[] opt;
        public byte[] reserved;

        public DateTime CreateDt { get; set; }
        public DateTime? ModifiedDt { get; set; }
        public DateTime FromDt { get; set; }
        public DateTime? ToDt { get; set; }
    }
}
