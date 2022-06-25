using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    /// <summary>
    /// Introduced in Medius API v2.7.
    /// Allow an application to post ascii information about a 
    /// problem that occurred during online gameplay.This function is strictly used
    /// only during development, QA and Public Beta phases of a title. In general, an
    /// application should not ship with calls to this function.
    /// </summary>
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.PostDebugInfo)]
    public class MediusPostDebugInfoRequest : BaseLobbyExtMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.PostDebugInfo;

        //
        public MessageId MessageID { get; set; }

        //
        public string Message; // DEBUGMESSAGE_MAXLEN


        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            MessageID = reader.Read<MessageId>();

            //
            Message = reader.ReadString(Constants.DEBUGMESSAGE_MAXLEN);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            writer.Write(MessageID);

            //
            writer.Write(Message, Constants.DEBUGMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"Message: {Message}";
        }
    }
}
