using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.MGCL
{
    [MediusApp(MediusAppPacketIds.MediusServerConnectNotification)]
    public class MediusServerConnectNotification : BaseAppMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.MediusServerConnectNotification;

        public MGCL_EVENT_TYPE ConnectEventType;
        public uint MediusWorldUID;
        public string PlayerSessionKey; // MGCL_SESSIONKEY_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            ConnectEventType = reader.Read<MGCL_EVENT_TYPE>();
            MediusWorldUID = reader.ReadUInt32();
            PlayerSessionKey = reader.ReadString(MediusConstants.MGCL_SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(ConnectEventType);
            writer.Write(MediusWorldUID);
            writer.Write(PlayerSessionKey, MediusConstants.MGCL_SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"ConnectEventType:{ConnectEventType}" + " " +
$"MediusWorldUID:{MediusWorldUID}" + " " +
$"PlayerSessionKey:{PlayerSessionKey}";
        }
    }
}