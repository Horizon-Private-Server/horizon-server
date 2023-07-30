using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GetWorldSecurityLevelResponse)]
    public class MediusGetWorldSecurityLevelResponse : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetWorldSecurityLevelResponse;

        /// <summary>
        /// Message ID
        /// </summary>
        public MessageId MessageID { get; set; }
        /// <summary>
        /// Response code for the request to get the security level about a world
        /// </summary>
        public MediusCallbackStatus StatusCode;
        /// <summary>
        /// The world ID of the lobby world or game world.
        /// </summary>
        public int MediusWorldID;
        /// <summary>
        /// Application type; chat channel or game
        /// </summary>
        public MediusApplicationType AppType;
        /// <summary>
        /// Security level information
        /// </summary>
        public MediusWorldSecurityLevelType SecurityLevel;


        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);

            //
            StatusCode = reader.Read<MediusCallbackStatus>();
            MediusWorldID = reader.ReadInt32();
            AppType = reader.Read<MediusApplicationType>();
            SecurityLevel = reader.Read<MediusWorldSecurityLevelType>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);

            //
            writer.Write(StatusCode);
            writer.Write(MediusWorldID);
            writer.Write(AppType);
            writer.Write(SecurityLevel);
        }

        public IMediusResponse GetDefaultFailedResponse(IMediusRequest request)
        {
            if (request == null)
                return null;

            // return new MediusGetWorldSecurityLevelResponse()
            // {
            //     MessageID = request.MessageID,
            //     StatusCode = MediusCallbackStatus.MediusNoResult
            // };
            return null;
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"MediusWorldID: {MediusWorldID} " +
                $"AppType: {AppType} " +
                $"SecurityLevel: {SecurityLevel}";
        }
    }
}