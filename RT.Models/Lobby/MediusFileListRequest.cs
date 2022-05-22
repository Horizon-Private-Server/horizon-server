using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FileListFiles)]
    public class MediusFileListRequest : BaseLobbyMessage, IMediusRequest
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FileListFiles;

        public char[] FileNameBeginsWith;
        public uint FilesizeGreaterThan;
        public uint FilesizeLessThan;
        public uint OwnerByID;
        public uint NewerThanTimestamp;
        public uint StartingEntryNumber;
        public uint PageSize;
        public MessageId MessageID { get; set; }

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            FileNameBeginsWith = reader.ReadChars(Constants.MEDIUS_FILE_MAX_FILENAME_LENGTH);
            FilesizeGreaterThan = reader.ReadUInt32();
            FilesizeLessThan = reader.ReadUInt32();
            OwnerByID = reader.ReadUInt32();
            NewerThanTimestamp = reader.ReadUInt32();
            StartingEntryNumber = reader.ReadUInt32();
            PageSize = reader.ReadUInt32();
            //
            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(2);
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(FileNameBeginsWith);
            writer.Write(FilesizeGreaterThan);
            writer.Write(FilesizeLessThan);
            writer.Write(OwnerByID);
            writer.Write(NewerThanTimestamp);
            writer.Write(StartingEntryNumber);
            writer.Write(PageSize);

            //
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(2);
        }


        public override string ToString()
        {
            return base.ToString() + " " +

                $"FileNameBeginsWith: {Convert.ToString(FileNameBeginsWith)} " +
                $"FilesizeGreaterThan: {FilesizeGreaterThan} " +
                $"FilesizeLessThan: {FilesizeLessThan} " +
                $"OwnerByID: {OwnerByID} " +
                $"NewerThanTimestamp: {NewerThanTimestamp} " +
                $"StartingEntryTimestamp: {StartingEntryNumber} " +
                $"MessageID:{MessageID} ";
        }
    }
}
