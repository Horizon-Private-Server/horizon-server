using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.DME
{
    [MediusApp(MediusAppPacketIds.DMETypePacketFragment)]
    public class DMETypePacketFragment : BaseDMEMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.DMETypePacketFragment;

        public byte MessageClass;
        public byte MessageType;
        public ushort SubPacketSize;
        public ushort SubPacketCount;
        public ushort SubPacketIndex;
        public byte MultiPacketindex;
        public int PacketBufferSize;
        public int PacketBufferOffset;
        public byte[] Payload;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageClass = reader.ReadByte();
            MessageType = reader.ReadByte();
            SubPacketSize = reader.ReadUInt16();
            SubPacketCount = reader.ReadUInt16();
            SubPacketIndex = reader.ReadUInt16();
            MultiPacketindex = reader.ReadByte();
            reader.ReadBytes(3);
            PacketBufferSize = reader.ReadInt32();
            PacketBufferOffset = reader.ReadInt32();
            Payload = reader.ReadBytes(SubPacketSize);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageClass);
            writer.Write(MessageType);
            writer.Write(SubPacketSize);
            writer.Write(SubPacketCount);
            writer.Write(SubPacketIndex);
            writer.Write(MultiPacketindex);
            writer.Write(new byte[3]);
            writer.Write(PacketBufferSize);
            writer.Write(PacketBufferOffset);
            writer.Write(Payload);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"MessageClass:{MessageClass}" + " " +
$"MessageType:{MessageType}" + " " +
$"SubPacketSize:{SubPacketSize}" + " " +
$"SubPacketCount:{SubPacketCount}" + " " +
$"SubPacketIndex:{SubPacketIndex}" + " " +
$"MultiPacketindex:{MultiPacketindex}" + " " +
$"PacketBufferSize:{PacketBufferSize}" + " " +
$"PacketBufferOffset:{PacketBufferOffset}";
        }

        public static List<DMETypePacketFragment> FromPayload(MediusAppPacketIds id, byte[] payload)
        {
            return FromPayload(id, payload, 0, payload.Length);
        }

        public static List<DMETypePacketFragment> FromPayload(MediusAppPacketIds id, byte[] payload, int index, int length)
        {
            List<DMETypePacketFragment> fragments = new List<DMETypePacketFragment>();

            byte messageClass = (byte)id;
            byte messageType = (byte)(((int)id) >> 8);

            int i = 0;

            while (i < length)
            {
                ushort subPacketSize = (ushort)(length - i);
                if (subPacketSize > MediusConstants.DME_FRAGMENT_MAX_PAYLOAD_SIZE)
                    subPacketSize = MediusConstants.DME_FRAGMENT_MAX_PAYLOAD_SIZE;

                var frag = new DMETypePacketFragment()
                {
                    MessageClass = messageClass,
                    MessageType = messageType,
                    SubPacketSize = subPacketSize,
                    SubPacketCount = 0,
                    SubPacketIndex = (ushort)fragments.Count,
                    MultiPacketindex = 0,
                    PacketBufferSize = length,
                    PacketBufferOffset = i,
                    Payload = new byte[subPacketSize]
                };

                // Copy payload segment into fragment payload
                Array.Copy(payload, i + index, frag.Payload, 0, subPacketSize);

                // 
                fragments.Add(frag);

                // Increment i
                i += subPacketSize;
            }

            // Recorrect fragment counts
            foreach (var frag in fragments)
                frag.SubPacketCount = (ushort)fragments.Count;

            return fragments;
        }
    }
}