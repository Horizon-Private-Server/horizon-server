using Server.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Server.Database
{
    class DbSimulated
    {
        public int AccountIdCounter { get; set; } = 1;
        public int ClanIdCounter { get; set; } = 1;
        public int ClanMessageIdCounter { get; set; } = 1;
        public int ClanInvitationIdCounter { get; set; } = 1;
        public List<AccountDTO> Accounts { get; set; } = new List<AccountDTO>();
        public List<ClanDTO> Clans { get; set; } = new List<ClanDTO>();
        public Dictionary<int, Dictionary<string, string>> AppSettings { get; set; } = new Dictionary<int, Dictionary<string, string>>();

        public bool Save(string filepath, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Encryption key cannot be null or empty.");

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKey(key, aes.KeySize / 8);
                    aes.GenerateIV();

                    using (var outStream = new MemoryStream())
                    {
                        outStream.Write(aes.IV, 0, aes.IV.Length); // store IV at start of file

                        using (var cryptoStream = new CryptoStream(outStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (var inStream = new MemoryStream(jsonBytes))
                        {
                            inStream.CopyTo(cryptoStream);
                        }

                        // save
                        File.WriteAllBytes(filepath, outStream.ToArray());
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save database: {ex}");
                return false;
            }
        }

        public void Load(string filepath, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Encryption key cannot be null or empty.");

            if (!File.Exists(filepath))
                return;

            var inBytes = File.ReadAllBytes(filepath);
            using (var inStream = new MemoryStream(inBytes))
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKey(key, aes.KeySize / 8);

                    var iv = new byte[aes.BlockSize / 8];
                    inStream.Read(iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var cryptoStream = new CryptoStream(inStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var outStream = new MemoryStream())
                    {
                        cryptoStream.CopyTo(outStream);

                        var jsonBytes = outStream.ToArray();
                        var json = Encoding.UTF8.GetString(jsonBytes);
                        var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<DbSimulated>(json);

                        this.AccountIdCounter = obj.AccountIdCounter;
                        this.ClanIdCounter = obj.ClanIdCounter;
                        this.ClanMessageIdCounter = obj.ClanMessageIdCounter;
                        this.ClanInvitationIdCounter = obj.ClanInvitationIdCounter;
                        this.Accounts = obj.Accounts;
                        this.Clans = obj.Clans;
                        this.AppSettings = obj.AppSettings;
                    }
                }
            }
        }


        // Derive a fixed-length key from a passphrase
        private static byte[] DeriveKey(string password, int length)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                Array.Resize(ref hash, length);
                return hash;
            }
        }
    }
}
