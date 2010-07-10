using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace spacecraft
{
    public class McLevelFactory
    {
        public static Map Load(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Cannot read " + filename);
            }

            StreamReader RawReader = new StreamReader(filename);
            GZipStream Reader = new GZipStream(RawReader.BaseStream, CompressionMode.Decompress);

            ParseTagStream(Reader);



            return new Map();
        }

        public static void Save(Map M)
        {

        }

        /// <summary>
        /// Used for handling nested TAG_Compound items. The current Compound is always the first item.
        /// </summary>
        static Stack<BinaryTag> CompoundStack;
        /// <summary>
        /// The children of the current TAG_Compound.
        /// </summary>
        static List<BinaryTag> Children;

        /// <summary>
        /// For miscellanous use.
        /// </summary>
        static byte[] buffer; 

        public static BinaryTag ParseTagStream(Stream ByteStream)
        {
            // Initialize everything.
            if (CompoundStack == null)
                CompoundStack = new Stack<BinaryTag>();

            if (Children == null)
                Children = new List<BinaryTag>();
            
            bool FoundEnd = false;


            // C# complains if this isn't defined.
            BinaryTag Root = new BinaryTag() { Name = "", Payload = null, Type = TagType.End };

            while (!FoundEnd || CompoundStack.Count > 0)
            {
                TagType ID = (TagType)(Byte)ByteStream.ReadByte();

                Root = new BinaryTag(ID);

                if (ID != TagType.End)
                {
                    // Read the name of the Tag.
                    int partOne = ByteStream.ReadByte();
                    int partTwo = ByteStream.ReadByte();
                    short length = (short)(256 * partOne + partTwo);
                    buffer = new byte[length];
                    ByteStream.Read(buffer, 0, length);
                    Root.Name = UTF8Encoding.UTF8.GetString(buffer);
                }


                Root.Type = ID;

                switch (ID)
                {
                    case TagType.End:
                        BinaryTag[] children = Children.ToArray();
                        Root = CompoundStack.Pop();
                        Root.Payload = children;
                        FoundEnd = true;
                        break;
                    
                    case TagType.Byte:
                        Root.Payload = (byte) ByteStream.ReadByte();
                        break;
                    
                    case TagType.Short:
                        buffer = new byte[2];
                        ByteStream.Read(buffer, 0, 2);
                        Root.Payload = BitConverter.ToInt16(buffer, 0);
                        break;
                    
                    case TagType.Int:
                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        Root.Payload = BitConverter.ToInt32(buffer, 0);
                        break;
                    
                    case TagType.Long:
                        buffer = new byte[8];
                        ByteStream.Read(buffer, 0, 8);
                        Root.Payload = BitConverter.ToInt64(buffer, 0);
                        break;
                    
                    case TagType.Float:
                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        Root.Payload = BitConverter.ToSingle(buffer, 0);
                        break;
                    
                    case TagType.Double:
                        buffer = new byte[8];
                        ByteStream.Read(buffer, 0, 8);
                        Root.Payload = BitConverter.ToDouble(buffer, 0);
                        break;
                    
                    case TagType.ByteArray:
                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        int length = BitConverter.ToInt32(buffer, 0);

                        Root.Payload = new byte[length];
                        ByteStream.Read( (byte[])Root.Payload, 0, length);

                        break;
                    
                    case TagType.String:
                        buffer = new byte[2];
                        ByteStream.Read(buffer, 0, 2);

                        short len = (short)(buffer[0] * 256 + buffer[1]);

                        buffer = new byte[len];
                        ByteStream.Read(buffer, 0, len);
                        Root.Payload = UTF8Encoding.UTF8.GetString(buffer);

                        break;
                    
                    case TagType.List:
                        throw new NotImplementedException();
                    
                    case TagType.Compound:
                        CompoundStack.Push(Root);
                        //ParseTagStream(ByteStream);
                        break;
                    
                    default:
                        throw new IOException("Parser exploded.");
                }

                if (!FoundEnd && ID != TagType.Compound)
                    Children.Add(Root);

            }
            return Root;
        }

    }

    public enum TagType : byte
    {
        End = 0,
        Byte = 1,
        Short =2, 
        Int = 3,
        Long = 4,
        Float = 5,
        Double = 6,
        ByteArray = 7,
        String = 8,
        List = 9,
        Compound = 10,
    }

    public struct BinaryTag
    {
        public TagType Type;
        public string Name;
        public object Payload;

        public BinaryTag(TagType T)
        {
            this.Type = T;
            this.Name = "";
            this.Payload = null;
        }

    }



}
