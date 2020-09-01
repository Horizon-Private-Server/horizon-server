using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Cryptography
{
    [JsonConverter(typeof(RSAConverter))]
    public class PS2_RSA : ICipher
    {
        public CipherContext Context { get; protected set; } = CipherContext.RSA_AUTH;

        public string Comment = "";
        public BigInteger N { get; protected set; }
        public BigInteger E { get; protected set; }
        public BigInteger D { get; protected set; }
        
        public PS2_RSA(BigInteger n, BigInteger e, BigInteger d)
        {
            N = n;
            E = e;
            D = d;
        }
        
        private BigInteger Encrypt(BigInteger m)
        {
            return m.ModPow(E, N);
        }

        private BigInteger Decrypt(BigInteger c)
        {
            return c.ModPow(D, N);
        }

        public virtual bool Decrypt(byte[] input, byte[] hash, out byte[] plain)
        {
            bool match = false;
            var plainBigInt = Decrypt(input.ToBigInteger());

            plain = plainBigInt.ToBA();
            Hash(plain, out var ourHash);
            match = ourHash.SequenceEqual(hash);
            if (match)
                return true;

            // Handle case where message > n
            plainBigInt = plainBigInt.Add(N);
            plain = plainBigInt.ToBA();
            Hash(plain, out ourHash);
            return ourHash.SequenceEqual(hash);
        }

        public virtual bool Encrypt(byte[] input, out byte[] cipher, out byte[] hash)
        {
            Hash(input, out hash);
            cipher = Encrypt(input.ToBigInteger()).ToBA();
            return true;
        }

        public virtual void Hash(byte[] input, out byte[] hash)
        {
            hash = SHA1.Hash(input, Context);
        }

        #region Comparison

        public override bool Equals(object obj)
        {
            if (obj is PS2_RSA rsa)
                return rsa.Equals(this);

            return base.Equals(obj);
        }

        public bool Equals(PS2_RSA b)
        {
            return b.Context == this.Context &&
                b.N.CompareTo(this.N) == 0 &&
                b.E.CompareTo(this.E) == 0 &&
                b.D.CompareTo(this.D) == 0;
        }

        #endregion

        public override string ToString()
        {
            return $"PS2_RSA({Context}, {N}, {E}, {D})";
        }
    }

    public class RSAConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            PS2_RSA rsa = (PS2_RSA)value;

            writer.WriteStartObject();
            writer.WritePropertyName("comment");
            serializer.Serialize(writer, rsa.Comment.ToString());
            writer.WritePropertyName("n");
            serializer.Serialize(writer, rsa.N.ToString());
            writer.WritePropertyName("e");
            serializer.Serialize(writer, rsa.E.ToString());
            writer.WritePropertyName("d");
            serializer.Serialize(writer, rsa.D.ToString());
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);

            var sN = (string)jsonObject["n"];
            var sE = (string)jsonObject["e"];
            var sD = (string)jsonObject["d"];
            return new PS2_RSA(new BigInteger(sN, 10), new BigInteger(sE, 10), new BigInteger(sD, 10)) { Comment = (string)jsonObject["comment"] };
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(PS2_RSA);
        }
    }

    static class RSAUtils
    {
        public static byte[] ToBA(this BigInteger b)
        {
            return b.ToByteArrayUnsigned().Reverse().ToArray();
        }

        public static BigInteger ToBigInteger(this byte[] ba)
        {
            return new BigInteger(1, ba.Reverse().ToArray());
        }
    }
}
