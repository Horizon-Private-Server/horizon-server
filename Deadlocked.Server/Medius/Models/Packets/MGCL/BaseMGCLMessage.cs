using RT.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Deadlocked.Server.Medius.Models.Packets.MGCL
{
    public abstract class BaseMGCLMessage : BaseMediusMessage
    {
        public override NetMessageTypes PacketClass => NetMessageTypes.MessageClassLobbyReport;

        /// <summary>
        /// 
        /// </summary>
        public string MessageID;

        public BaseMGCLMessage()
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
            MessageID = reader.ReadString(Constants.MGCL_MESSAGEID_MAXLEN);
        }

        /// <summary>
        /// Serialize contents of the message.
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            //
            base.Serialize(writer);

            // 
            writer.Write(MessageID, Constants.MGCL_MESSAGEID_MAXLEN);
        }

        #endregion

        protected string GenerateMessageId()
        {
            return "1";
        }

    }
}
