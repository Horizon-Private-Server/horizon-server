using RT.Common;
using Server.Common;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.UpdateUserState)]
    public class MediusUpdateUserState : BaseLobbyMessage
    {
		public override byte PacketType => (byte)MediusLobbyMessageIds.UpdateUserState;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusUserAction UserAction;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
            UserAction = reader.Read<MediusUserAction>();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(UserAction);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey: {SessionKey} " +
             $"UserAction: {UserAction}";
        }
    }
}
