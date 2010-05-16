using System;
using System.Collections.Generic;
using System.Text;

namespace spacecraft
{
    /* Types used by the Packet class to guareentee properties about the data being sent. */

    public struct NetworkString
    {
        byte[] _contents;
        const byte FILLER_CHAR = 0x20;
        public const int Size = 64;

        public static implicit operator byte[](NetworkString s)
        {
            byte[] b = new byte[Size];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = FILLER_CHAR;
            }
            s._contents.CopyTo(b, 0);
            return b;
        }
        public NetworkString(byte[] bar)
        {
            _contents = new byte[Size];
            for (int i = 0; i < _contents.Length; i++)
            {
                _contents[i] = FILLER_CHAR;
            }
            bar.CopyTo(_contents, 0);
        }
    }

    public struct NetworkByteArray
    {
        byte[] _contents;
        const byte FILLER_CHAR =  0x00;
        public const int Size = 1024;

        public static implicit operator byte[](NetworkByteArray s)
        {
            byte[] b = new byte[Size];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = FILLER_CHAR;
            }
            s._contents.CopyTo(b, 0);
            return b;
        }
        public NetworkByteArray(byte[] bar)
        {
            _contents = new byte[Size];
            for (int i = 0; i < _contents.Length; i++)
            {
                _contents[i] = FILLER_CHAR;
            }
            bar.CopyTo(_contents, 0);
        }
    }

    public struct NetworkShort
    {
        short _content;
        public const int Size = 2;

        public static implicit operator short(NetworkShort s)
        {
            return s._content;
        }
        public static implicit operator NetworkShort(short s)
        {
            NetworkShort q;
            q._content = s;
            return q;
        }
        public static implicit operator byte[](NetworkShort s)
        {
            return BitConverter.GetBytes(s._content);
        }
    }
}
