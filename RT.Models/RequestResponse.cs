using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Models
{
    public interface IMediusRequest
    {
        MessageId MessageID { get; set; }

        IMediusResponse GetDefaultFailedResponse(IMediusRequest request);
    }

    public interface IMediusResponse
    {
        MessageId MessageID { get; set; }

        bool IsSuccess { get; }
    }
}
