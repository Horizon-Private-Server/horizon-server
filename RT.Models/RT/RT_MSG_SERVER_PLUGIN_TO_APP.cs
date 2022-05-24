using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_SERVER_PLUGIN_TO_APP)]
    public class RT_MSG_SERVER_PLUGIN_TO_APP : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_SERVER_PLUGIN_TO_APP;

        public byte incomingMessage;
        public byte size;
        public byte pluginId;
        public NetMessageType messageType;

        public uint protocolVersion;
        public int buildNumber;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {

            incomingMessage = reader.ReadByte();
            size = reader.ReadByte();
            pluginId = reader.ReadByte();
            messageType = (NetMessageType)reader.ReadInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            writer.Write(incomingMessage);
            writer.Write(size);
            writer.Write(pluginId);
            writer.Write(new byte[3]);
            writer.Write(messageType);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"incomingMessage: {incomingMessage} " +
                $"size: {size} " +
                $"pluginId: {pluginId} " +
                $"messageType: {messageType}";
        }

    }
}