using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace spacecraft
{
	/* Types used by the Packet class to guareentee properties about the data being sent. */

	public abstract class NetworkByteContainer
	{
		protected byte[] _contents;
		protected static byte FILLER_CHAR;
		protected static int Size;

		public int thisSize;
		public byte thisFill;

		public override string ToString()
		{
			int end = Size;
			for (int i = _contents.Length - 1; i > 0; --i) // Starts at the end, counts backwards.
			{
				if (_contents[i] != this.thisFill)
				{ // Find last non-filler character, and record it.
					end = i;
					break;
				}
			}
			string output = Encoding.ASCII.GetString(_contents);
			// If FILLER_CHAR is anything other than null, it'll appear in the parsed string, so we need to take it out.
			return output.Substring(0, end + 1);
		}

		public static implicit operator byte[](NetworkByteContainer s)
		{
			byte[] b = new byte[s.thisSize];
			for (int i = 0; i < b.Length; i++)
			{
				b[i] = s.thisFill;
			}
			s._contents.CopyTo(b, 0);
			return b;
		}
	}

	public class NetworkString : NetworkByteContainer
	{
		new public const byte FILLER_CHAR = 0x20;
		new public const int Size = 64;

		public NetworkString(byte[] raw)
		{
			thisSize = Size;
			thisFill = FILLER_CHAR;

			_contents = new byte[Size];
			for (int i = 0; i < _contents.Length; i++)
			{
				_contents[i] = FILLER_CHAR;
			}
			raw.CopyTo(_contents, 0);
		}

		public static implicit operator NetworkString(string s)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			return new NetworkString(bytes);
		}
	}

	public class NetworkByteArray : NetworkByteContainer
	{
		new public const int  Size = 1024;
		new public const byte FILLER_CHAR = 0x00;

		public NetworkByteArray(byte[] bar)
		{
			thisSize = Size;
			thisFill = FILLER_CHAR;
			if (bar.Length > Size)
			{
				throw new ArgumentException();
			}
			_contents = new byte[Size];
			for (int i = 0; i < _contents.Length; i++)
			{
				_contents[i] = FILLER_CHAR;
			}
			bar.CopyTo(_contents, 0);
		}

		public static implicit operator NetworkByteArray(string s)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			return new NetworkByteArray(bytes);
		}
	}

	public struct NetworkShort
	{
		short _content;
		public const int Size = 2;

		public NetworkShort (byte[] array, int p)
		{
			short s = BitConverter.ToInt16(array, p);
			s = IPAddress.NetworkToHostOrder(s);
			this = s;
		}

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
			return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(s._content));
		}
	}
}
