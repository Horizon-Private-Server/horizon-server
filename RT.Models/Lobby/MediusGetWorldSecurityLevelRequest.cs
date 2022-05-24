using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.GetWorldSecurityLevel)]
    public class MediusGetWorldSecurityLevelRequest : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetWorldSecurityLevel;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public MediusWorldSecurityLevelType SecurityLevel;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(2);
            SessionKey = reader.ReadString();
            MediusWorldID = reader.ReadInt32();
            SecurityLevel = reader.Read<MediusWorldSecurityLevelType>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[2]);
            writer.Write(SessionKey);
            writer.Write(MediusWorldID);
            writer.Write(SecurityLevel);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"CharacterEncoding: {MediusWorldID} " +
                $"Language: {SecurityLevel}";
        }
    }
}
