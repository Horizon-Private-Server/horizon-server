using Deadlocked.Server.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Deadlocked.Server
{
    public static class Utils
    {
        public static byte[] ReverseEndian(byte[] ba)
        {
            byte[] ret = new byte[ba.Length];
            for (int i = 0; i < ba.Length; i += 4)
            {
                int max = i + 3;
                if (max >= ba.Length)
                    max = ba.Length - 1;

                for (int x = max; x >= i; x--)
                    ret[i + (max - x)] = ba[x];
            }
            return ret;
        }

        public static byte[] FromString(string str)
        {
            byte[] buffer = new byte[str.Length / 2];

            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = byte.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return buffer;
        }

        public static byte[] FromStringFlipped(string str)
        {
            byte[] buffer = new byte[str.Length / 2];

            int strIndex = str.Length - 2;
            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = byte.Parse(str.Substring(strIndex, 2), System.Globalization.NumberStyles.HexNumber);
                strIndex -= 2;
            }

            return buffer;
        }

        public static void PrettyPrintByteArray(byte[] buffer)
        {
            string str = "";
            for (int i = 0; i < buffer.Length; ++i)
            {
                char c = (char)buffer[i];
                Console.Write(buffer[i].ToString("X2") + " ");
                str += char.IsControl(c) ? '.' : c;
            }

            Console.WriteLine();
            Console.WriteLine(" STR: " + str);
        }

        public static void PrintByteArray(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; ++i)
                Console.Write(buffer[i].ToString("X2"));
        }

        #region Serialization

        public static void Write(this BinaryWriter writer, IPAddress ip)
        {
            if (ip == null)
                writer.Write(new byte[16]);
            else
                writer.Write(Encoding.UTF8.GetBytes(ip.ToString().PadRight(16, '\0')));
        }

        public static IPAddress ReadIPAddress(this BinaryReader reader)
        {
            return IPAddress.Parse(reader.ReadString(16));
        }

        public static void Write(this BinaryWriter writer, string str, int length)
        {
            if (str == null)
                writer.Write(new byte[length]);
            else if (str.Length >= length)
                writer.Write(Encoding.UTF8.GetBytes(str.Substring(0, length-1) + "\0"));
            else
                writer.Write(Encoding.UTF8.GetBytes(str.PadRight(length, '\0')));
        }

        public static string ReadString(this BinaryReader reader, int length)
        {
            byte[] buffer = reader.ReadBytes(length);
            int i = 0;
            for (i = 0; i < buffer.Length; ++i)
                if (buffer[i] == 0)
                    break;

            if (i > 0)
                return Encoding.UTF8.GetString(buffer, 0, i);
            else
                return string.Empty;
        }

        #endregion

        #region LINQ

        /// <summary>
        /// By user Servy on
        /// https://stackoverflow.com/questions/24630643/linq-group-by-sum-of-property.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> GroupWhileAggregating<T, TAccume>(
            this IEnumerable<T> source,
            TAccume seed,
            Func<TAccume, T, TAccume> accumulator,
            Func<TAccume, T, bool> predicate)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    yield break;

                List<T> list = new List<T>() { iterator.Current };
                TAccume accume = accumulator(seed, iterator.Current);
                while (iterator.MoveNext())
                {
                    accume = accumulator(accume, iterator.Current);
                    if (predicate(accume, iterator.Current))
                    {
                        list.Add(iterator.Current);
                    }
                    else
                    {
                        yield return list;
                        list = new List<T>() { iterator.Current };
                        accume = accumulator(seed, iterator.Current);
                    }
                }
                yield return list;
            }
        }

        /// <summary>
        /// By user Ash on
        /// https://stackoverflow.com/questions/914109/how-to-use-linq-to-select-object-with-minimum-or-maximum-property-value
        /// </summary>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, null);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer = comparer ?? Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var min = sourceIterator.Current;
                var minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }

        #endregion

        #region Time

        public static uint GetUnixTime()
        {
            return DateTime.UtcNow.ToUnixTime();
        }

        public static uint ToUnixTime(this DateTime time)
        {
            return (uint)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        #endregion

        #region MediusComparisonOperator

        public static bool Compare(this MediusComparisonOperator op, long lhs, long rhs)
        {
            switch (op)
            {
                case MediusComparisonOperator.EQUAL_TO: return lhs == rhs;
                case MediusComparisonOperator.GREATER_THAN: return lhs > rhs;
                case MediusComparisonOperator.GREATER_THAN_OR_EQUAL_TO: return lhs >= rhs;
                case MediusComparisonOperator.LESS_THAN: return lhs < rhs;
                case MediusComparisonOperator.LESS_THAN_OR_EQUAL_TO: return lhs <= rhs;
                case MediusComparisonOperator.NOT_EQUALS: return lhs != rhs;
                default: return false;
            }   
        }

        #endregion

    }
}
