using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    public abstract class BaseLobbyExtMessage : BaseMediusMessage
    {
        public override NetMessageTypes MessageClass => NetMessageTypes.MessageClassLobbyExt;

        /// <summary>
        /// 
        /// </summary>
        public string MessageID;

        public BaseLobbyExtMessage()
        {
            MessageID = GenerateMessageId();
        }

        #region Serialization

        /// <summary>
        /// Deserializes the message from plaintext.
        /// </summary>
        /// <param name="reader"></param>
        public override void Deserialize(BinaryReader reader)
        {
            //
            base.Deserialize(reader);

            // 
            MessageID = reader.ReadString(MediusConstants.MESSAGEID_MAXLEN);
        }

        /// <summary>
        /// Serialize contents of the message.
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            //
            base.Serialize(writer);

            // 
            writer.Write(MessageID, MediusConstants.MESSAGEID_MAXLEN);
        }

        #endregion

        protected string GenerateMessageId()
        {
            return "1";
        }

    }
}
