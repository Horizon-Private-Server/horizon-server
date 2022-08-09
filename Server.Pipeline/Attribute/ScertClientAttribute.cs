﻿using Org.BouncyCastle.Math;
using RT.Cryptography;
using RT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Pipeline.Attribute
{
    public class ScertClientAttribute
    {
        public static RsaKeyPair DefaultRsaAuthKey = null;

        public int MediusVersion
        {
            get => _mediusVersion;
            set
            {
                _mediusVersion = value;
                OnMediusVersionChanged();
            }
        }

        public bool IsPS3Client => MediusVersion >= 112;
        public CipherService CipherService { get; set; } = null;
        public RsaKeyPair RsaAuthKey { get; set; } = null;

        private int _mediusVersion = 0;

        public ScertClientAttribute()
        {
            // default
            MediusVersion = 108;
        }

        public bool OnMessage(BaseScertMessage message)
        {
            if (message is RT_MSG_CLIENT_HELLO clientHello)
            {
                MediusVersion = clientHello.Parameters[1];
                return true;
            }
            else if (message is RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp && MediusVersion == 0)
            {
                MediusVersion = 108;
                return true;
            }

            return false;
        }

        private void OnMediusVersionChanged()
        {
            if (IsPS3Client)
            {
                CipherService = new CipherService(new PS3CipherFactory());
                CipherService.SetCipher(CipherContext.RSA_AUTH, (RsaAuthKey ?? DefaultRsaAuthKey).ToPS3());
            }
            else
            {
                CipherService = new CipherService(new PS2CipherFactory());
                CipherService.SetCipher(CipherContext.RSA_AUTH, (RsaAuthKey ?? DefaultRsaAuthKey).ToPS2());
            }
        }
    }
}
