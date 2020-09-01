using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Common
{
    public interface IStreamSerializer
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);

    }
}
