using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.CreateClanResponse)]
    public class MediusCreateClanResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.CreateClanResponse;

        public MediusCallbackStatus StatusCode;
        public int ClanID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            ClanID = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(ClanID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"ClanID:{ClanID}";
        }
    }
}