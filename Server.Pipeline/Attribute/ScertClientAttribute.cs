using RT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Pipeline.Attribute
{
    public class ScertClientAttribute
    {
        public int MediusVersion { get; set; }

        public bool OnMessage(BaseScertMessage message)
        {
            if (message is RT_MSG_CLIENT_HELLO clientHello)
            {
                MediusVersion = clientHello.Parameters[1];
                return true;
            }

            return false;
        }
    }
}
