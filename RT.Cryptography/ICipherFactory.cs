using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Cryptography
{
    public interface ICipherFactory
    {
        ICipher CreateNew(CipherContext context);
        ICipher CreateNew(CipherContext context, byte[] publicKey);
    }
}
