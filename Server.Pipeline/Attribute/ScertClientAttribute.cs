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
            // PS2 medius uses version < 112
            if (MediusVersion < 112)
            {
                CipherService = new CipherService(new PS2CipherFactory());
            }
            else
            {
                CipherService = new CipherService(new PS3CipherFactory());
            }
        }

        public ICipher GetDefaultRSAKey(PS2_RSA rsa)
        {
            // PS2 medius uses version < 112
            if (MediusVersion < 112)
            {
                return rsa;
            }
            else
            {
                return new PS3_RSA(rsa.N, rsa.E, rsa.D);
            }
        }
    }
}
