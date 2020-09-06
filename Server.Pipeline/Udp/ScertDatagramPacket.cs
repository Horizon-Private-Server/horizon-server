using RT.Models;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Server.Pipeline.Udp
{
    public class ScertDatagramPacket
    {

        public EndPoint Source { get; set; }

        public EndPoint Destination { get; set; }

        public BaseScertMessage Message { get; set; }


        public ScertDatagramPacket(BaseScertMessage message, EndPoint destination)
        {
            this.Destination = destination;
            this.Source = null;
            this.Message = message;
        }

        public ScertDatagramPacket(BaseScertMessage message, EndPoint target, EndPoint source)
        {
            this.Source = source;
            this.Destination = target;
            this.Message = message;
        }

    }
}
