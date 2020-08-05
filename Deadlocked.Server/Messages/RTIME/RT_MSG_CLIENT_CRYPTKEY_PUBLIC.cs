using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.RTIME
{
    [Message(RT_MSG_TYPE.RT_MSG_CLIENT_CRYPTKEY_PUBLIC)]
    public class RT_MSG_CLIENT_CRYPTKEY_PUBLIC : BaseMessage
    {

        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CRYPTKEY_PUBLIC;

        // 
        public byte[] Key = null;

        public override void Deserialize(BinaryReader reader)
        {
            Key = reader.ReadBytes(0x40);
        }

        protected override void Serialize(BinaryWriter writer)
        {
            if (Key == null || Key.Length != 0x40)
                throw new InvalidOperationException("Unable to serialize CLIENT_GET_KEY key because key is either null or not 64 bytes long!");
            
            writer.Write(Key);
        }
    }
}
