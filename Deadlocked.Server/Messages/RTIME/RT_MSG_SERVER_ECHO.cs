using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_SERVER_ECHO)]
    public class RT_MSG_SERVER_ECHO : BaseMessage
    {
        // EC 5A A7 4F CB 92 0E 00 
        // EE 5A A7 4F A9 AD 02 00 
        // F2 5A A7 4F FE 04 04 00 
        // 00 5B A7 4F 4B 34 08 00 
        // 02 5B A7 4F C0 44 04 00 
        // 1E 5B A7 4F 64 1B 00 00 
        // 20 5B A7 4F 06 11 00 00 

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_ECHO;

        // 
        public uint UnixTimestamp = Utils.GetUnixTime();
        public uint UNK_04 = 0x00000000;

        public override void Deserialize(BinaryReader reader)
        {
            UnixTimestamp = reader.ReadUInt32();
            UNK_04 = reader.ReadUInt32();
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(UnixTimestamp);
            writer.Write(UNK_04);
        }
    }
}
