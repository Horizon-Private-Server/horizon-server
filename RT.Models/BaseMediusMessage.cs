using RT.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using RT.Common;
using Server.Common;
using Server.Common.Logging;

namespace RT.Models
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MediusMessageAttribute : Attribute
    {
        public NetMessageTypes MessageClass;
        public byte MessageType;

        public MediusMessageAttribute(NetMessageTypes msgClass, MediusDmeMessageIds msgType)
        {
            MessageClass = msgClass;
            MessageType = (byte)msgType;
        }

        public MediusMessageAttribute(NetMessageTypes msgClass, MediusMGCLMessageIds msgType)
        {
            MessageClass = msgClass;
            MessageType = (byte)msgType;
        }

        public MediusMessageAttribute(NetMessageTypes msgClass, MediusLobbyMessageIds msgType)
        {
            MessageClass = msgClass;
            MessageType = (byte)msgType;
        }

        public MediusMessageAttribute(NetMessageTypes msgClass, MediusLobbyExtMessageIds msgType)
        {
            MessageClass = msgClass;
            MessageType = (byte)msgType;
        }
    }

    public abstract class BaseMediusMessage
    {
        /// <summary>
        /// Message class.
        /// </summary>
        public abstract NetMessageTypes PacketClass { get; }

        /// <summary>
        /// Message type.
        /// </summary>
        public abstract byte PacketType { get; }

        public BaseMediusMessage()
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

        #region Logging

        /// <summary>
        /// Whether or not this message passes the log filter.
        /// </summary>
        public virtual bool CanLog()
        {
            switch (this.PacketClass)
            {
                case NetMessageTypes.MessageClassDME: return LogSettings.Singleton?.IsLog((MediusDmeMessageIds)this.PacketType) ?? false;
                case NetMessageTypes.MessageClassLobby: return LogSettings.Singleton?.IsLog((MediusLobbyMessageIds)this.PacketType) ?? false;
                case NetMessageTypes.MessageClassLobbyReport: return LogSettings.Singleton?.IsLog((MediusMGCLMessageIds)this.PacketType) ?? false;
                case NetMessageTypes.MessageClassLobbyExt: return LogSettings.Singleton?.IsLog((MediusLobbyExtMessageIds)this.PacketType) ?? false;
                default: return true;
            }
        }

        #endregion

        #region Dynamic Instantiation

        private static Dictionary<MediusDmeMessageIds, Type> _dmeMessageClassById = null;
        private static Dictionary<MediusMGCLMessageIds, Type> _mgclMessageClassById = null;
        private static Dictionary<MediusLobbyMessageIds, Type> _lobbyMessageClassById = null;
        private static Dictionary<MediusLobbyExtMessageIds, Type> _lobbyExtMessageClassById = null;


        private static void Initialize()
        {
            if (_dmeMessageClassById != null)
                return;

            _dmeMessageClassById = new Dictionary<MediusDmeMessageIds, Type>();
            _mgclMessageClassById = new Dictionary<MediusMGCLMessageIds, Type>();
            _lobbyMessageClassById = new Dictionary<MediusLobbyMessageIds, Type>();
            _lobbyExtMessageClassById = new Dictionary<MediusLobbyExtMessageIds, Type>();

            // Populate
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(BaseMediusMessage));
            var types = assembly.GetTypes();

            foreach (Type classType in types)
            {
                // Objects by Id
                var attrs = (MediusMessageAttribute[])classType.GetCustomAttributes(typeof(MediusMessageAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    switch (attrs[0].MessageClass)
                    {
                        case NetMessageTypes.MessageClassDME:
                            {
                                _dmeMessageClassById.Add((MediusDmeMessageIds)attrs[0].MessageType, classType);
                                break;
                            }
                        case NetMessageTypes.MessageClassLobbyReport:
                            {
                                _mgclMessageClassById.Add((MediusMGCLMessageIds)attrs[0].MessageType, classType);
                                break;
                            }
                        case NetMessageTypes.MessageClassLobby:
                            {
                                _lobbyMessageClassById.Add((MediusLobbyMessageIds)attrs[0].MessageType, classType);
                                break;
                            }
                        case NetMessageTypes.MessageClassLobbyExt:
                            {
                                _lobbyExtMessageClassById.Add((MediusLobbyExtMessageIds)attrs[0].MessageType, classType);
                                break;
                            }
                    }
                }
            }
        }

        public static BaseMediusMessage Instantiate(BinaryReader reader)
        {
            BaseMediusMessage msg;
            Type classType = null;

            // Init
            Initialize();

            NetMessageTypes msgClass = reader.Read<NetMessageTypes>();
            var msgType = reader.ReadByte();

            switch (msgClass)
            {
                case NetMessageTypes.MessageClassDME:
                    {
                        if (!_dmeMessageClassById.TryGetValue((MediusDmeMessageIds)msgType, out classType))
                            classType = null;
                        break;
                    }
                case NetMessageTypes.MessageClassLobbyReport:
                    {
                        if (!_mgclMessageClassById.TryGetValue((MediusMGCLMessageIds)msgType, out classType))
                            classType = null;
                        break;
                    }
                case NetMessageTypes.MessageClassLobby:
                    {
                        if (!_lobbyMessageClassById.TryGetValue((MediusLobbyMessageIds)msgType, out classType))
                            classType = null;
                        break;
                    }
                case NetMessageTypes.MessageClassLobbyExt:
                    {
                        if (!_lobbyExtMessageClassById.TryGetValue((MediusLobbyExtMessageIds)msgType, out classType))
                            classType = null;
                        break;
                    }
            }

            // Instantiate
            if (classType == null)
                msg = new RawMediusMessage(msgClass, msgType);
            else
                msg = (BaseMediusMessage)Activator.CreateInstance(classType);

            // Deserialize
            msg.Deserialize(reader);
            return msg;
        }

        #endregion

    }
}
