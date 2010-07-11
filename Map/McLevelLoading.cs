using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Net;

namespace spacecraft
{
	public static partial class McLevelFactory
	{
		public static Map Load(string filename)
		{
			StreamReader RawReader = new StreamReader(filename);
			GZipStream Reader = new GZipStream(RawReader.BaseStream, CompressionMode.Decompress);

			NBT.BinaryTag tag = NBT.NbtParser.ParseTagStream(Reader);
			
			Reader.Close();
			RawReader.Close();
			
			// TODO: parse BinaryTag into Map.

            throw new NotImplementedException();
		}

		
	}
}
