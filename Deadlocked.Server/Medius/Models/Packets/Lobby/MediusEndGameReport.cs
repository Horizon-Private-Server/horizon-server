using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.EndGameReport)]
    public class MediusEndGameReport : BaseMediusMessage
    {

        public override TypesAAA MessageType => TypesAAA.EndGameReport;

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
            SessionKey = reader.ReadString(MediusConstants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
            MediusWorldID = reader.ReadInt32();
            WinningTeam = reader.ReadString(MediusConstants.WINNINGTEAM_MAXLEN);
            WinningPlayer = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            FinalScore = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, MediusConstants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(MediusWorldID);
            writer.Write(WinningTeam, MediusConstants.WINNINGTEAM_MAXLEN);
            writer.Write(WinningPlayer, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(FinalScore);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"MediusWorldID:{MediusWorldID}" + " " +
$"WinningTeam:{WinningTeam}" + " " +
$"WinningPlayer:{WinningPlayer}" + " " +
$"FinalScore:{FinalScore}";
        }
    }
}