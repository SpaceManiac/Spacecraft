using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using spacecraft;

class Program
{
    static void Main()
    {
        StreamReader RawReader = new StreamReader("test.nbt");
        GZipStream Reader = new GZipStream(RawReader.BaseStream, CompressionMode.Decompress);

        BinaryTag foo = spacecraft.McLevelFactory.ParseTagStream(Reader);
        int a = 0;
    }
}
