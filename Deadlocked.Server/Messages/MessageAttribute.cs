using System;
using System.Collections.Generic;
using System.Text;

namespace Deadlocked.Server.Messages
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageAttribute : Attribute
    {
        public RT_MSG_TYPE MessageId;

        public MessageAttribute(RT_MSG_TYPE id)
        {
            MessageId = id;
        }
    }
}
