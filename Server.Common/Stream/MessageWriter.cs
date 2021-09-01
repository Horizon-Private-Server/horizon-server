using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Common.Stream
{
    public class MessageWriter : BinaryWriter
    {
        public int MediusVersion { get; set; }

        public MessageWriter() : base() { }
        public MessageWriter(System.IO.Stream output) : base(output) { }
        public MessageWriter(System.IO.Stream output, Encoding encoding) : base(output, encoding) { }
        public MessageWriter(System.IO.Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }
    }
}
