using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Deadlocked.Server.Messages.MGCL
{
    public abstract class BaseMGCLMessage : BaseAppMessage
    {
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
            MessageID = reader.ReadString(MediusConstants.MGCL_MESSAGEID_MAXLEN);
        }

        /// <summary>
        /// Serialize contents of the message.
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            //
            base.Serialize(writer);

            // 
            writer.Write(MessageID, MediusConstants.MGCL_MESSAGEID_MAXLEN);
        }

        #endregion

        protected string GenerateMessageId()
        {
            return "1";
        }

    }
}
