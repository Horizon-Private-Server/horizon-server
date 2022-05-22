namespace Server.Database.Models
{
    internal class SceNpIdTDO
    {
        public byte[] data { get; set; }
        public byte term { get; set; }
        public byte[] dummy { get; set; }

        public byte[] opt { get; set; }
        public byte[] reserved { get; set; }
    }
}
