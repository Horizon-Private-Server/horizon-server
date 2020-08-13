using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.FileCreate)]
    public class MediusFileCreateRequest : BaseAppMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.FileCreate;

        public MediusFile MediusFileToCreate;
        public MediusFileAttributes MediusFileCreateAttributes;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MediusFileToCreate = reader.Read<MediusFile>();
            MediusFileCreateAttributes = reader.Read<MediusFileAttributes>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MediusFileToCreate);
            writer.Write(MediusFileCreateAttributes);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"MediusFileToCreate:{MediusFileToCreate}" + " " +
$"MediusFileCreateAttributes:{MediusFileCreateAttributes}";
        }
    }
}