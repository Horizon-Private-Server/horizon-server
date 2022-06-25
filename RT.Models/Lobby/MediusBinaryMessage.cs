using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    /// <summary>
    /// Introduced in Medius API v2.7
    /// - Sends a binary message to everyone in the channel, 
    /// or to a specific account id.
    /// </summary>
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.BinaryMessage)]
    public class MediusBinaryMessage : BaseLobbyExtMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.BinaryMessage;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusBinaryMessageType MessageType;
        public int TargetAccountID;
        public byte[] Message = new byte[Constants.BINARYMESSAGE_MAXLEN];

        //Resistance 2
        public int Unk1;
        public int Unk2;
        public int Unk3;
        public string GameName;
        public int Unk4;
        public int Unk5;
        public int Unk6;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            MessageType = reader.Read<MediusBinaryMessageType>();
            TargetAccountID = reader.ReadInt32();

            /*
            //Resistance 2 Binary Msg
            if(reader.AppId == 21731)
            {
                Unk1 = reader.ReadInt32();
                Unk2 = reader.ReadInt32();
                Unk3 = reader.ReadInt32();
                reader.ReadBytes(11);
                GameName = reader.ReadString(Constants.R2GAMENAME_MAXLEN);
                reader.ReadBytes(37);
                Unk4 = reader.ReadInt32();
                Unk5 = reader.ReadInt32();
                Unk6 = reader.ReadInt32();
            }
            */
            Message = reader.ReadBytes(Constants.BINARYMESSAGE_MAXLEN);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(MessageType);
            writer.Write(TargetAccountID);
            writer.Write(Message, Constants.BINARYMESSAGE_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"SessionKey:{SessionKey} " +
                $"MessageType:{MessageType} " +
                $"TargetAccountID:{TargetAccountID} " +
                $"Message:{BitConverter.ToString(Message)}";
        }
    }
}
