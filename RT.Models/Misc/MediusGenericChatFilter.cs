﻿using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    public class MediusGenericChatFilter : IStreamSerializer
    {

        public byte[] GenericChatFilterBitfield = new byte[Constants.MEDIUS_GENERIC_CHAT_FILTER_BYTES_LEN];

        public void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            GenericChatFilterBitfield = reader.ReadBytes(Constants.MEDIUS_GENERIC_CHAT_FILTER_BYTES_LEN);
        }

        public void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            writer.Write(GenericChatFilterBitfield, Constants.MEDIUS_GENERIC_CHAT_FILTER_BYTES_LEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"GenericChatFilterBitfield:{BitConverter.ToString(GenericChatFilterBitfield)}";
        }
    }
}
