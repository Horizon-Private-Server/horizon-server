using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Common
{
    public interface IStreamSerializer
    {
        void Serialize(Stream.MessageWriter writer);
        void Deserialize(Stream.MessageReader reader);

    }
}
