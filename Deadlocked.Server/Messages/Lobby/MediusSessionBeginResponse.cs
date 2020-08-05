using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.SessionBeginResponse)]
    public class MediusSessionBeginResponse : BaseLobbyMessage
    {
        public override MediusAppPacketIds Id => MediusAppPacketIds.SessionBeginResponse;

        public MediusCallbackStatus StatusCode;
        public string SessionKey; // SESSIONKEY_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
    $"StatusCode:{StatusCode}" +
    $"SessionKey:{SessionKey}";
        }
    }
}
