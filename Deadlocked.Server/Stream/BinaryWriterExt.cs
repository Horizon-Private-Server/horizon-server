using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Deadlocked.Server.Stream
{
    public static class BinaryWriterExt
    {

        public static void Write<T>(this BinaryWriter writer, T value)
        {
            writer.WriteObject(value, typeof(T));
        }

        public static void WriteObject(this BinaryWriter writer, object value, Type type)
        {
            if (value == null || type == null)
                return;

            if (value is IStreamSerializer)
                (value as IStreamSerializer).Serialize(writer);
            else if (type.IsEnum)
                writer.WriteObject(value, type.GetEnumUnderlyingType());
            else if (type == typeof(bool))
                writer.Write((bool)value);
            else if (type == typeof(byte))
                writer.Write((byte)value);
            else if (type == typeof(sbyte))
                writer.Write((sbyte)value);
            else if (type == typeof(char))
                writer.Write((char)value);
            else if (type == typeof(short))
                writer.Write((short)value);
            else if (type == typeof(ushort))
                writer.Write((ushort)value);
            else if (type == typeof(int))
                writer.Write((int)value);
            else if (type == typeof(uint))
                writer.Write((uint)value);
            else if (type == typeof(long))
                writer.Write((long)value);
            else if (type == typeof(ulong))
                writer.Write((ulong)value);
            else if (type == typeof(float))
                writer.Write((float)value);
            else if (type == typeof(double))
                writer.Write((double)value);
            else if (type == typeof(string))
                writer.Write((string)value);
            else
                throw new Exception("Unable to serialize object " + value + " of type " + type);
        }
    }
}
