using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.UpdateUserState)]
    public class MediusUpdateUserState : BaseAppMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.UpdateUserState;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusUserAction UserAction;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
            UserAction = reader.Read<MediusUserAction>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(UserAction);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"UserAction:{UserAction}";
        }
    }
}