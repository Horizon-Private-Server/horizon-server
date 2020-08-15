using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.AccountGetIDResponse)]
    public class MediusAccountGetIDResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.AccountGetIDResponse;

        public int AccountID;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            AccountID = reader.ReadInt32();
            StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(AccountID);
            writer.Write(StatusCode);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"AccountID:{AccountID}" + " " +
$"StatusCode:{StatusCode}";
        }
    }
}