using RT.Common;
using System;
using System.Collections.Generic;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_MEMORY_POKE)]
    public class RT_MSG_SERVER_MEMORY_POKE : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_MEMORY_POKE;

        public long MsgDataLen;
        public long Address = 0;
        public byte[] Payload;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            Address = reader.ReadUInt32();
            MsgDataLen = reader.ReadUInt32();
            Payload = reader.ReadBytes((int)MsgDataLen);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(Address);
            writer.Write(Payload?.Length ?? 0);
            if (Payload != null)
                writer.Write(Payload);
        }

        public override string ToString()
        {
            return base.ToString() + " " +

                $"Address: {Address:X8} " +
                $"MsgDataLen: {MsgDataLen} " +
                $"Payload: {BitConverter.ToString(Payload)}";
        }


        public static List<RT_MSG_SERVER_MEMORY_POKE> FromPayload(uint address, byte[] payload)
        {
            int i = 0;
            var msgs = new List<RT_MSG_SERVER_MEMORY_POKE>();

            while (i < payload.Length)
            {
                int len = (payload.Length - i);
                if (len > Constants.MEDIUS_MESSAGE_MAXLEN)
                    len = Constants.MEDIUS_MESSAGE_MAXLEN;

                // 
                var msg = new RT_MSG_SERVER_MEMORY_POKE()
                {
                    Address = (uint)(address + i),
                    Payload = new byte[len],
                    SkipEncryption = true
                };

                // 
                Array.Copy(payload, i, msg.Payload, 0, len);

                // 
                msgs.Add(msg);

                i += len;
            }


            return msgs;
        }
    }
}