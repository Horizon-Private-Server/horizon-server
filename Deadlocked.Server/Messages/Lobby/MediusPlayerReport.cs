using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.PlayerReport)]
    public class MediusPlayerReport : BaseAppMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.PlayerReport;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public string Stats; // ACCOUNTSTATS_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
            MediusWorldID = reader.ReadInt32();
            Stats = reader.ReadString(MediusConstants.ACCOUNTSTATS_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(MediusWorldID);
            writer.Write(Stats, MediusConstants.ACCOUNTSTATS_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"Stats:{Stats}";
        }
    }
}