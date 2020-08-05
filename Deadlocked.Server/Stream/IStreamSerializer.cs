using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Stream
{
    public interface IStreamSerializer
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);

    }
}
