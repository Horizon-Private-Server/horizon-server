using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Models
{
    public interface IMediusRequest
    {
        string MessageID { get; set; }
    }

    public interface IMediusResponse
    {
        string MessageID { get; set; }

        bool IsSuccess { get; }
    }
}
