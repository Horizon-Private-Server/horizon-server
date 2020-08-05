using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.AccountLogin)]
    public class MediusAccountLoginRequest : BaseLobbyMessage
    {
        public override MediusAppPacketIds Id => MediusAppPacketIds.AccountLogin;

        public string SessionKey = "13088";
        public string Username;
        public string Password;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            Username = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            Password = reader.ReadString(MediusConstants.PASSWORD_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(Username, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(Password, MediusConstants.PASSWORD_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"SessionKey:{SessionKey} " +
                $"USERNAME:{Username} " +
                $"PASS:{Password}";
        }
    }
}
