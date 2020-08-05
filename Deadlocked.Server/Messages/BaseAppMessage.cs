using Deadlocked.Server.Messages.App;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Deadlocked.Server.Messages
{
    public abstract class BaseAppMessage
    {
        /// <summary>
        /// Message id.
        /// </summary>
        public abstract MediusAppPacketIds Id { get; }

        public BaseAppMessage()
        {

        }

        #region Serialization

        /// <summary>
        /// Deserializes the message from plaintext.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Deserialize(BinaryReader reader)
        {

        }

        /// <summary>
        /// Serialize contents of the message.
        /// </summary>
        public virtual void Serialize(BinaryWriter writer)
        {

        }

        #endregion

        #region Dynamic Instantiation

        private static Dictionary<MediusAppPacketIds, Type> _messageClassById = null;


        private static void Initialize()
        {
            if (_messageClassById != null)
                return;

            _messageClassById = new Dictionary<MediusAppPacketIds, Type>();

            // Populate
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(BaseAppMessage));
            var types = assembly.GetTypes();

            foreach (Type classType in types)
            {
                // Objects by Id
                var attrs = (MediusAppAttribute[])classType.GetCustomAttributes(typeof(MediusAppAttribute), true);
                if (attrs != null && attrs.Length > 0)
                    _messageClassById.Add(attrs[0].PacketId, classType);
            }
        }

        public static BaseAppMessage Instantiate(MediusAppPacketIds id, BinaryReader reader)
        {
            BaseAppMessage msg;

            // Init
            Initialize();

            // Instantiate
            if (!_messageClassById.TryGetValue(id, out var classType))
                msg = new RawAppMessage(id);
            else
                msg = (BaseAppMessage)Activator.CreateInstance(classType);

            // Deserialize
            msg.Deserialize(reader);
            return msg;
        }

        #endregion

    }
}
