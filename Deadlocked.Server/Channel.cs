using Deadlocked.Server.Messages;
using Deadlocked.Server.Messages.Lobby;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server
{
    public class Channel
    {
        public static int IdCounter = 1;

        public int Id = 0;

        private DateTime utcTimeLastReport = DateTime.UtcNow;
        private bool isAlive => (DateTime.UtcNow - utcTimeLastReport).TotalSeconds < 60;

        public Channel()
        {
            Id = IdCounter++;
        }

        public void OnPlayerReport(ClientObject client, MediusPlayerReport report)
        {
            utcTimeLastReport = DateTime.UtcNow;
        }
    }
}
