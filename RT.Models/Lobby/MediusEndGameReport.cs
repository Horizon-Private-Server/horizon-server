using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.EndGameReport)]
    public class MediusEndGameReport : BaseLobbyMessage
    {


		public override byte PacketType => (byte)MediusLobbyMessageIds.EndGameReport;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public string WinningTeam; // WINNINGTEAM_MAXLEN
        public string WinningPlayer; // ACCOUNTNAME_MAXLEN
        public int FinalScore;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
            MediusWorldID = reader.ReadInt32();
            WinningTeam = reader.ReadString(Constants.WINNINGTEAM_MAXLEN);
            WinningPlayer = reader.ReadString(Constants.ACCOUNTNAME_MAXLEN);
            FinalScore = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(MediusWorldID);
            writer.Write(WinningTeam, Constants.WINNINGTEAM_MAXLEN);
            writer.Write(WinningPlayer, Constants.ACCOUNTNAME_MAXLEN);
            writer.Write(FinalScore);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey} " +
$"MediusWorldID:{MediusWorldID} " +
$"WinningTeam:{WinningTeam} " +
$"WinningPlayer:{WinningPlayer} " +
$"FinalScore:{FinalScore}";
        }
    }
}
