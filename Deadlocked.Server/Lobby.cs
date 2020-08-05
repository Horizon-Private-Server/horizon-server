using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.App;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server
{
    public class Lobby
    {
        public MediusCreateGameRequest GameInfo;
        public int MediusWorldID = 0;

        public Lobby()
        {

        }

        public Lobby(MediusCreateGameRequest req)
        {

        }

    }
}
