using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.PartyCreateRequest)]
    public class MediusPartyCreateRequest : BaseLobbyExtMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.PartyCreateRequest;

        public MessageId MessageID { get; set; }
        
        //
        public string SessionKey; // SESSIONKEY_MAXLEN
        public int ApplicationID;
        public int MinPlayers;
        public int MaxPlayers;
        public string PartyName; // PARTYNAME_MAXLEN
        public string PartyPassword; // PARTYPASSWORD_MAXLEN
        public int GenericField1;
        public int GenericField2;
        public int GenericField3;
        public int GenericField4;
        public int GenericField5;
        public int GenericField6;
        public int GenericField7;
        public int GenericField8;
        public MediusGameHostType GameHostType;
        public string ServerSessionKey; //SESSIONKEY_MAXLEN


        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            ApplicationID = reader.ReadInt32();
            MinPlayers = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
            PartyName = reader.ReadString(Constants.PARTYNAME_MAXLEN);
            PartyPassword = reader.ReadString(Constants.PARTYPASSWORD_MAXLEN);
            GenericField1 = reader.ReadInt32();
            GenericField2 = reader.ReadInt32();
            GenericField3 = reader.ReadInt32();
            GenericField4 = reader.ReadInt32();
            GenericField5 = reader.ReadInt32();
            GenericField6 = reader.ReadInt32();
            GenericField7 = reader.ReadInt32();
            GenericField8 = reader.ReadInt32();
            GameHostType = reader.Read<MediusGameHostType>();
            ServerSessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(2);
            writer.Write((short)ApplicationID);
            writer.Write(MinPlayers);
            writer.Write(MaxPlayers);
            writer.Write(PartyName);
            writer.Write(PartyPassword);
            writer.Write(GenericField1);
            writer.Write(GenericField2);
            writer.Write(GenericField3);
            writer.Write(GenericField4);
            writer.Write(GenericField5);
            writer.Write(GenericField6);
            writer.Write(GenericField7);
            writer.Write(GenericField8);
            writer.Write(GameHostType);
            writer.Write(ServerSessionKey);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"ApplicationID: {ApplicationID} " +
                $"MinPlayers: {MinPlayers} " +
                $"MaxPlayers: {MaxPlayers} " +
                $"PartyName: {PartyName} " +
                $"PartyPassword: {PartyPassword} " +
                $"GenericField1: {GenericField1} " +
                $"GenericField2: {GenericField2} " +
                $"GenericField3: {GenericField3} " +
                $"GenericField4: {GenericField4} " +
                $"GenericField5: {GenericField5} " +
                $"GenericField6: {GenericField6} " +
                $"GenericField7: {GenericField7} " +
                $"GenericField8: {GenericField8} " +
                $"GameHostType: {GameHostType} " +
                $"ServerSessionKey: {ServerSessionKey} ";
        }
    }
}
