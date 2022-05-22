using RT.Common;
using Server.Common;
using System;
using System.IO;

namespace RT.Models
{
    public class MediusFile : IStreamSerializer
    {
        public string Filename;
        public byte[] ServerChecksum = new byte[Constants.MEDIUS_FILE_CHECKSUM_NUMBYTES];
        public uint FileID;
        public uint FileSize;
        public uint CreationTimeStamp;
        public uint OwnerID;
        public uint GroupID;
        public ushort OwnerPermissionRWX;
        public ushort GroupPermissionRWX;
        public ushort GlobalPermissionRWX;
        public ushort ServerOperationID;

        public void Deserialize(BinaryReader reader)
        {
            // 
            Filename = reader.ReadString(Constants.MEDIUS_FILE_MAX_FILENAME_LENGTH);
            ServerChecksum = reader.ReadBytes(Constants.MEDIUS_FILE_CHECKSUM_NUMBYTES);
            FileID = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            CreationTimeStamp = reader.ReadUInt32();
            OwnerID = reader.ReadUInt32();
            GroupID = reader.ReadUInt32();
            OwnerPermissionRWX = reader.ReadUInt16();
            GroupPermissionRWX = reader.ReadUInt16();
            GlobalPermissionRWX = reader.ReadUInt16();
            ServerOperationID = reader.ReadUInt16();
        }

        public void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(Filename, Constants.MEDIUS_FILE_MAX_FILENAME_LENGTH);
            writer.Write(ServerChecksum, Constants.MEDIUS_FILE_CHECKSUM_NUMBYTES);
            writer.Write(FileID);
            writer.Write(FileSize);
            writer.Write(CreationTimeStamp);
            writer.Write(OwnerID);
            writer.Write(GroupID);
            writer.Write(OwnerPermissionRWX);
            writer.Write(GroupPermissionRWX);
            writer.Write(GlobalPermissionRWX);
            writer.Write(ServerOperationID);
        }

        public override string ToString()
        {
            return $"Filename: {Filename} " +
                $"ServerChecksum: {BitConverter.ToString(ServerChecksum)} " +
                $"FileID: {FileID} " +
                $"FileSize: {FileSize} " +
                $"CreationTimeStamp: {CreationTimeStamp} " +
                $"OwnerID: {OwnerID} " +
                $"GroupID: {GroupID} " +
                $"OwnerPermissionRWX: {OwnerPermissionRWX} " +
                $"GroupPermissionRWX: {GroupPermissionRWX} " +
                $"GlobalPermissionRWX: {GlobalPermissionRWX} " +
                $"ServerOperationID: {ServerOperationID}";
        }
    }
}