using RT.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Models.Misc
{
    public interface IMediusChatMessage
    {
        MediusChatMessageType MessageType { get; }
        int TargetID { get; }
        string Message { get; }
    }
}
