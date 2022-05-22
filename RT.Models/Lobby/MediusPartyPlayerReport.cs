using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.PartyPlayerReport)]
    public class MediusPartyPlayerReport : BaseLobbyExtMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.PartyPlayerReport;

        public bool IsSuccess => StatusCode >= 0;

        // Not Used
        public MessageId MessageID { get; set; }
        public MediusCallbackStatus StatusCode;
        //

        public string SessionKey; // SESSIONKEY_MAXLEN

        public int MediusWorldID;
        public int PartyIndex;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);

            // 
            MediusWorldID = reader.ReadInt32();
            PartyIndex = reader.ReadInt32();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);

            // 
            writer.Write(MediusWorldID);
            writer.Write(PartyIndex);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"SessionKey: {SessionKey} " +
                $"MediusWorldID: {MediusWorldID} " +
                $"PartyIndex: {PartyIndex}";
        }
    }
}
