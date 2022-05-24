using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.MatchFindGameRequest)]
    public class MediusMatchFindGameRequest : BaseLobbyExtMessage, IMediusRequest
    {

        public override byte PacketType => (byte)MediusLobbyExtMessageIds.MatchFindGameRequest;

        public MessageId MessageID { get; set; }
        public string SessionKey; // SESSIONKEY_MAXLEN
        public uint SupersetID;
        public uint GameWorldID;
        public string GamePassword; // GAMEPASSWORD_MAXLEN
        public MediusJoinType PlayerJoinType;
        public uint MinPlayers;
        public uint MaxPlayers;
        public char[] GameHostTypeBitField;
        public uint MatchOptions;
        public string ServerSessionKey; // SESSIONKEY_MAXLEN
        public char[] RequestData;
        public int GroupMemberListSize;
        public int ApplicationDataSize;
        public char[] GroupMemberAccountIDList;
        public char[] ApplicationData;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            SupersetID = reader.ReadUInt32();
            GameWorldID = reader.ReadUInt32();
            PlayerJoinType = reader.Read<MediusJoinType>();
            MinPlayers = reader.ReadUInt32();
            MaxPlayers = reader.ReadUInt32();
            GameHostTypeBitField = reader.ReadChars(4);
            MatchOptions = reader.ReadUInt32();
            ServerSessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            RequestData = reader.ReadChars(16);
            GroupMemberListSize = reader.ReadInt32();
            ApplicationDataSize = reader.ReadInt32();
            GroupMemberAccountIDList = reader.ReadChars(GroupMemberListSize);
            ApplicationData = reader.ReadChars(ApplicationDataSize);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(SupersetID);
            writer.Write(GameWorldID);
            writer.Write(PlayerJoinType);
            writer.Write(MinPlayers);
            writer.Write(MaxPlayers);
            writer.Write(GameHostTypeBitField);
            writer.Write(MatchOptions);
            writer.Write(ServerSessionKey);
            writer.Write(RequestData);
            writer.Write(GroupMemberListSize);
            writer.Write(ApplicationDataSize);
            writer.Write(GroupMemberAccountIDList);
            writer.Write(ApplicationData);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"SupersetID: {SupersetID} " +
                $"GameWorldID: {GameWorldID} " +
                $"PlayerJoinType: {PlayerJoinType} " +
                $"MinPlayers: {MinPlayers} " +
                $"MaxPlayers: {MaxPlayers} " +
                $"GameHostTypeBitField: {GameHostTypeBitField} " +
                $"MatchOptions: {MatchOptions} " +
                $"ServerSessionKey: {ServerSessionKey} " +
                $"RequestData: {RequestData} " +
                $"GroupMemberListSize: {GroupMemberListSize} " +
                $"ApplicationDataSize: {ApplicationDataSize} " +
                $"GroupMemberAccountIDList: {GroupMemberAccountIDList} " +
                $"ApplicationData: {ApplicationData} ";
        }
    }
}
