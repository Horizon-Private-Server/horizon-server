﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Common.Stream
{
    public class MessageReader : BinaryReader
    {
        public int MediusVersion { get; set; }

        public int AppId { get; set; }

        public MessageReader(System.IO.Stream input) : base(input) { }
        public MessageReader(System.IO.Stream input, Encoding encoding) : base(input, encoding) { }
        public MessageReader(System.IO.Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }
    }
}
