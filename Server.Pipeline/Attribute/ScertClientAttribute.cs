using Org.BouncyCastle.Math;
using RT.Cryptography;
using RT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Pipeline.Attribute
{
    public class ScertClientAttribute
    {
        public int MediusVersion { get; set; }
        public bool IsPS3Client => MediusVersion >= 112;
        public CipherService CipherService { get; set; }

        public bool OnMessage(BaseScertMessage message)
        {
            if (message is RT_MSG_CLIENT_HELLO clientHello)
            {
                MediusVersion = clientHello.Parameters[1];
                OnMediusVersionChanged();
                return true;
            }

            return false;
        }

        private void OnMediusVersionChanged()
        {
            if (IsPS3Client)
            {
                CipherService = new CipherService(new PS3CipherFactory());
            }
            else
            {
                CipherService = new CipherService(new PS2CipherFactory());
            }
        }

        public ICipher GetDefaultRSAKey(PS2_RSA rsa)
        {
            if (IsPS3Client)
            {
                return new PS3_RSA(rsa.N, rsa.E, rsa.D);
            }
            else
            {
                return rsa;
            }
        }
    }
}
