using Deadlocked.Server.Messages.DME;
using Deadlocked.Server.Messages.RTIME;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Deadlocked.Server.Messages
{
    public abstract class BaseMessage
    {
        /// <summary>
        /// Message id.
        /// </summary>
        public abstract RT_MSG_TYPE Id { get; }

        public BaseMessage()
        {

        }

        #region Serialization

        /// <summary>
        /// Deserializes the message from plaintext.
        /// </summary>
        /// <param name="reader"></param>
        public abstract void Deserialize(BinaryReader reader);

        /// <summary>
        /// Serializes the message and encrypts it with a given cipher.
        /// </summary>
        public void Serialize(ICipher cipher, out List<byte[]> results, out List<byte[]> hashes)
        {
            // 
            results = new List<byte[]>();
            hashes = new List<byte[]>();

            // serialize
            Serialize(out var plains);

            // encrypt
            foreach (var plain in plains)
            {
                if (!cipher.Encrypt(plain, out var result, out var hash))
                    throw new InvalidOperationException($"Unable to encrypt {Id} message: {BitConverter.ToString(plain).Replace("-", "")}");

                results.Add(result);
                hashes.Add(hash);
            }
        }

        /// <summary>
        /// Serializes the message.
        /// </summary>
        public void Serialize(out List<byte[]> results)
        {
            results = new List<byte[]>();
            byte[] result = null;
            var buffer = new byte[1024 * 10];
            int length = 0;

            // Serialize message
            using (MemoryStream stream = new MemoryStream(buffer, true))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    Serialize(writer);
                    length = (int)writer.BaseStream.Position;
                }
            }

            // Check for fragmentation
            if (Id == RT_MSG_TYPE.RT_MSG_SERVER_APP && length > MediusConstants.MEDIUS_MESSAGE_MAXLEN)
            {
                MediusAppPacketIds appId = (MediusAppPacketIds)((buffer[1] << 8) | buffer[0]);
                var fragments = DMETypePacketFragment.FromPayload(appId, buffer, 2, length - 2);

                foreach (var frag in fragments)
                {
                    new RT_MSG_SERVER_APP() { AppMessage = frag }.Serialize(out var fragBuffers);
                    results.AddRange(fragBuffers);
                }
            }
            else
            {
                // Add id and length to header
                result = new byte[length + 3];
                result[0] = (byte)this.Id;
                result[1] = (byte)(length & 0xFF);
                result[2] = (byte)((length >> 8) & 0xFF);

                Array.Copy(buffer, 0, result, 3, length);
                results.Add(result);
            }
        }

        /// <summary>
        /// Send message to clients.
        /// </summary>
        public void Send(params ClientSocket[] clients)
        {
            // Serialize
            Serialize(out var msgBuffers);

            // Condense as much as possible
            var msgs = msgBuffers.GroupWhileAggregating(0, (sum, item) => sum + item.Length, (sum, item) => sum < MediusConstants.MEDIUS_MESSAGE_MAXLEN).SelectMany(x => x);

            // 
            foreach (var client in clients)
                foreach (var msg in msgs)
                    client.Send(msg);
        }

        /// <summary>
        /// Serialize contents of the message.
        /// </summary>
        protected abstract void Serialize(BinaryWriter writer);

        #endregion

        #region Dynamic Instantiation

        private static Dictionary<RT_MSG_TYPE, Type> _messageClassById = null;


        private static void Initialize()
        {
            if (_messageClassById != null)
                return;

            _messageClassById = new Dictionary<RT_MSG_TYPE, Type>();

            // Populate
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(BaseMessage));
            var types = assembly.GetTypes();

            foreach (Type classType in types)
            {
                // Objects by Id
                var attrs = (MessageAttribute[])classType.GetCustomAttributes(typeof(MessageAttribute), true);
                if (attrs != null && attrs.Length > 0)
                    _messageClassById.Add(attrs[0].MessageId, classType);
            }
        }

        public static void RegisterMessage(RT_MSG_TYPE id, Type type)
        {
            // Init first
            Initialize();

            // Set or overwrite.
            if (!_messageClassById.ContainsKey(id))
                _messageClassById.Add(id, type);
            else
                _messageClassById[id] = type;
        }

        public static List<BaseMessage> InstantiateBruteforce(byte[] messageBuffer, Func<RT_MSG_TYPE, CipherContext, IEnumerable<ICipher>> getCiphersCallback = null)
        {
            // Init first
            Initialize();

            List<BaseMessage> msgs = new List<BaseMessage>();
            BaseMessage msg = null;

            // 
            using (MemoryStream stream = new MemoryStream(messageBuffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    while (reader.BaseStream.CanRead && reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        // Reset
                        msg = null;

                        // Parse header
                        byte rawId = reader.ReadByte();
                        RT_MSG_TYPE id = (RT_MSG_TYPE)(rawId & 0x7F);
                        bool encrypted = rawId >= 0x80;
                        ushort len = reader.ReadUInt16();

                        // Get class
                        if (!_messageClassById.TryGetValue(id, out var classType))
                            classType = null;

                        // Decrypt
                        if (len > 0 && encrypted)
                        {
                            byte[] hash = reader.ReadBytes(4);
                            CipherContext context = (CipherContext)(hash[3] >> 5);
                            var ciphers = getCiphersCallback(id, context);
                            byte[] cipherText = reader.ReadBytes(len);

                            foreach (var cipher in ciphers)
                            {
                                if (cipher.Decrypt(cipherText, hash, out var plain))
                                {
                                    msg = Instantiate(classType, id, plain);
                                    break;
                                }
                            }

                            if (msg == null)
                                Console.WriteLine($"Unable to decrypt {id}: {BitConverter.ToString(messageBuffer).Replace("-", "")}");
                        }
                        else
                        {
                            msg = Instantiate(classType, id, reader.ReadBytes(len));
                        }

                        if (msg != null)
                            msgs.Add(msg);
                    }
                }
            }

            return msgs;
        }

        public static List<BaseMessage> Instantiate(byte[] messageBuffer, Func<RT_MSG_TYPE, CipherContext, ICipher> getCipherCallback = null)
        {
            // Init first
            Initialize();

            List<BaseMessage> msgs = new List<BaseMessage>();
            BaseMessage msg = null;

            // 
            using (MemoryStream stream = new MemoryStream(messageBuffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    while (reader.BaseStream.CanRead && reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        // Reset
                        msg = null;

                        // Parse header
                        byte rawId = reader.ReadByte();
                        RT_MSG_TYPE id = (RT_MSG_TYPE)(rawId & 0x7F);
                        bool encrypted = rawId >= 0x80;
                        ushort len = reader.ReadUInt16();

                        // End
                        if (len > (reader.BaseStream.Length - reader.BaseStream.Position))
                            break;

                        // Get class
                        if (!_messageClassById.TryGetValue(id, out var classType))
                            classType = null;

                        // Decrypt
                        if (encrypted)
                        {
                            byte[] hash = reader.ReadBytes(4);
                            byte[] cipherText = reader.ReadBytes(len);
                            CipherContext context = (CipherContext)(hash[3] >> 5);
                            var cipher = getCipherCallback(id, context);

                            if (cipher.Decrypt(cipherText, hash, out var plain))
                                msg = Instantiate(classType, id, plain);
                            else
                                msg = Instantiate(classType, id, new byte[0]);
                        }
                        else
                        {
                            msg = Instantiate(classType, id, reader.ReadBytes(len));
                        }

                        if (msg != null)
                            msgs.Add(msg);
                    }
                }
            }

            return msgs;
        }

        private static BaseMessage Instantiate(Type classType, RT_MSG_TYPE id, byte[] plain)
        {
            BaseMessage msg = null;

            // 
            using (MemoryStream stream = new MemoryStream(plain))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (classType == null)
                        msg = new RawMessage(id);
                    else
                        msg = (BaseMessage)Activator.CreateInstance(classType);

                    try
                    {
                        msg.Deserialize(reader);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error deserializing {id} {BitConverter.ToString(plain)}");
                        Console.WriteLine(e);
                    }
                }
            }

            return msg;
        }

        #endregion

        public override string ToString()
        {
            return $"Id:{Id}";
        }

    }
}
