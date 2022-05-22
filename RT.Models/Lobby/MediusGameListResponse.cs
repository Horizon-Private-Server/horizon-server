﻿using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.GameListResponse)]
    public class MediusGameListResponse : BaseLobbyMessage, IMediusResponse
    {

        public override byte PacketType => (byte)MediusLobbyMessageIds.GameListResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public int MediusWorldID;
        public string GameName;
        public MediusWorldStatus WorldStatus;
        public MediusGameHostType GameHostType;
        public ushort PlayerCount;
        public bool EndOfList;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            MediusWorldID = reader.ReadInt32();
            GameName = reader.ReadString(Constants.GAMENAME_MAXLEN);
            WorldStatus = reader.Read<MediusWorldStatus>();
            GameHostType = reader.Read<MediusGameHostType>();
            PlayerCount = reader.ReadUInt16();
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(MediusWorldID);
            writer.Write(GameName, Constants.GAMENAME_MAXLEN);
            writer.Write(WorldStatus);
            writer.Write(GameHostType);
            writer.Write(PlayerCount);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"MediusWorldID: {MediusWorldID} " +
                $"GameName: {GameName} " +
                $"WorldStatus: {WorldStatus} " +
                $"GameHostType: {GameHostType} " +
                $"PlayerCount: {PlayerCount} " +
                $"EndOfList: {EndOfList}";
        }
    }
}