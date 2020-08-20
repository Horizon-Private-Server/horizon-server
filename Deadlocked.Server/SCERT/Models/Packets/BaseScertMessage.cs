using DotNetty.Buffers;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.SCERT.Models.Packets
{
    public abstract class BaseScertMessage
    {
        public const int HEADER_SIZE = 3;
        public const int HASH_SIZE = 4;

        /// <summary>
        /// Message id.
        /// </summary>
        public abstract RT_MSG_TYPE Id { get; }

        public BaseScertMessage()
        {

        }

        #region Serialization

        /// <summary>
        /// Deserializes the message from plaintext.
        /// </summary>
        /// <param name="reader"></param>
        public abstract void Deserialize(BinaryReader reader);

        /// <summary>
        /// Serializes the message.
        /// </summary>
        public List<byte[]> Serialize()
        {
            var results = new List<byte[]>();
            byte[] result = new byte[1024 * 4];
            int length;

            // Serialize message
            using (MemoryStream stream = new MemoryStream(result, true))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)this.Id);
                    writer.Write((ushort)0);
                    Serialize(writer);
                    length = (int)writer.BaseStream.Position - HEADER_SIZE;

                    writer.Seek(1, SeekOrigin.Begin);
                    writer.Write((ushort)length);
                }
            }

            results.Add(result);
            return results;
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
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(BaseScertMessage));
            var types = assembly.GetTypes();

            foreach (Type classType in types)
            {
                // Objects by Id
                var attrs = (ScertMessageAttribute[])classType.GetCustomAttributes(typeof(ScertMessageAttribute), true);
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

        public static BaseScertMessage Instantiate(RT_MSG_TYPE id, byte[] hash, byte[] messageBuffer, Func<RT_MSG_TYPE, CipherContext, ICipher> getCipherCallback = null)
        {
            // Init first
            Initialize();

            BaseScertMessage msg = null;

            // Get class
            if (!_messageClassById.TryGetValue(id, out var classType))
                classType = null;

            // Decrypt
            if (hash != null)
            {
                CipherContext context = (CipherContext)(hash[3] >> 5);
                var cipher = getCipherCallback(id, context);

                if (cipher.Decrypt(messageBuffer, hash, out var plain))
                {
                    msg = Instantiate(classType, id, plain);
                }

                // This is a hack to make the dme server connect
                // We don't really care what their key is since we're not encrypting our response
                else if (id == RT_MSG_TYPE.RT_MSG_CLIENT_CRYPTKEY_PUBLIC)
                {
                    msg = Instantiate(classType, id, plain);
                }
                else
                {
                    Console.WriteLine($"Unable to decrypt {id}, HASH:{BitConverter.ToString(hash)} DATA:{BitConverter.ToString(messageBuffer)}");
                }
            }
            else
            {
                msg = Instantiate(classType, id, messageBuffer);
            }

            return msg;
        }

        private static BaseScertMessage Instantiate(Type classType, RT_MSG_TYPE id, byte[] plain)
        {
            BaseScertMessage msg = null;

            // 
            using (MemoryStream stream = new MemoryStream(plain))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (classType == null)
                        msg = new RawScertMessage(id);
                    else
                        msg = (BaseScertMessage)Activator.CreateInstance(classType);

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

    [AttributeUsage(AttributeTargets.Class)]
    public class ScertMessageAttribute : Attribute
    {
        public RT_MSG_TYPE MessageId;

        public ScertMessageAttribute(RT_MSG_TYPE id)
        {
            MessageId = id;
        }
    }
}
