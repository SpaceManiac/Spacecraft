using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Net;
using MiscUtil.Conversion;

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

        #region Old code

        /*/// <summary>
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

        /// <summary>
        /// How many items are left in the current TAG_List, if any.
        /// </summary>
        static int ListItemsRemaining = 0;

        /// <summary>
        /// The type of items in the list currently being parsed.
        /// </summary>
        static TagType ListType = TagType.UNKNOWN;

        static BinaryTag CurrentList;

        public static BinaryTag ParseTagStreamDepracated(Stream ByteStream)
        {
            // Initialize everything.
            if (CompoundStack == null)
                CompoundStack = new Stack<BinaryTag>();

            if (Children == null)
                Children = new List<BinaryTag>();
            
            bool FoundEnd = false;


            // C# complains if this isn't defined.
            BinaryTag Root = new BinaryTag() { Name = "", Payload = null, Type = TagType.End };


            // While there is still an unterminated TAG_Compound.
            while (!FoundEnd || CompoundStack.Count > 0)
            {
                TagType ID = TagType.UNKNOWN;

                if (ListType == TagType.UNKNOWN)
                {
                    ID = (TagType)ByteStream.ReadByte();
                }
                else
                {
                    ID = ListType;
                }


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


               // Root.Type = ID;

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
                        ListType = (TagType) ByteStream.ReadByte();
                        
                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        int ListLength = BitConverter.ToInt32(buffer,0);
                        ListItemsRemaining = ListLength;
                        CurrentList = Root;
                        break;
                    
                    case TagType.Compound:
                        CompoundStack.Push(Root);
                        //ParseTagStream(ByteStream);
                        break;
                    
                    default:
                        throw new IOException("Parser exploded.");
                }

                if (!FoundEnd && ID != TagType.Compound)
                {
                    Children.Add(Root);
                    if (ListType != TagType.UNKNOWN)
                    { // We've just parsed a list item, so decrement the counter.
                        --ListItemsRemaining;
                    }
                }
                // No more list items remaining
                if (ListItemsRemaining == 0 && ListType != TagType.UNKNOWN)
                {
                    // No more list items remaining
                    ListType = TagType.UNKNOWN;
                    BinaryTag[] children = Children.ToArray();
                    CurrentList.Payload = children;
                }

            }
            return Root;
        }*/

        #endregion


        static StringBuilder XMLBuilder = new StringBuilder();

        static Stack<TagType> CompoundStack = new Stack<TagType>();

        static int ListItemsRemaining = -1; // How many items remain in the list. -1 represents no list at all.
        static byte[] buffer;
        static TagType ListType = TagType.UNKNOWN;

        public static XmlTextReader ParseTagStream(Stream ByteStream)
        {
            bool EOF = false;
            bool Started = true;

            do
            {
                TagType ID;
                if (CompoundStack.Count == 0 || CompoundStack.Peek() == TagType.Compound)
                {
                    int B = ByteStream.ReadByte();
                    ID = (TagType) B;
                }
                else
                {
                    ID = ListType;
                }
                string name = "";
                if (ListItemsRemaining <= 0 && ID != TagType.End)
                    name = ReadShortPrefixedString(ByteStream);

                switch (ID)
                {
                    case TagType.End:
                        XMLBuilder.Append("</compound>");
                        System.Diagnostics.Debug.WriteLine(ByteStream.Position);
                        CompoundStack.Pop();
                        break;
                    
                    case TagType.Byte:
                        XMLBuilder.Append("<byte ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");
                        XMLBuilder.Append(ByteStream.ReadByte().ToString());
                        XMLBuilder.Append("\" />");
                        break;

                    case TagType.Short:
                        XMLBuilder.Append("<short ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");
                        
                        int one = ByteStream.ReadByte();
                        int two = ByteStream.ReadByte();
                        short S = (short) (one * 256 + two);

                        XMLBuilder.Append(S.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    
                    case TagType.Int:
                        XMLBuilder.Append("<int ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");


                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        int I = EndianBitConverter.Big.ToInt32(buffer,0);

                        XMLBuilder.Append(I.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    
                    case TagType.Long:
                        XMLBuilder.Append("<long ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");


                        buffer = new byte[8];
                        ByteStream.Read(buffer, 0, 8);
                        long L =  EndianBitConverter.Big.ToInt64(buffer, 0);

                        XMLBuilder.Append(L.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    
                    case TagType.Float:
                        XMLBuilder.Append("<single ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");


                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        float F = EndianBitConverter.Big.ToSingle(buffer, 0);


                        XMLBuilder.Append(F.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    
                    case TagType.Double:
                        XMLBuilder.Append("<double ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");


                        buffer = new byte[8];
                        ByteStream.Read(buffer, 0, 8);
                        double D = EndianBitConverter.Big.ToDouble(buffer, 0);

                        XMLBuilder.Append(D.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    
                    case TagType.ByteArray:
                        XMLBuilder.Append("<byte_array ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");


                        buffer = new byte[4];
                        ByteStream.Read(buffer, 0, 4);
                        int length =  EndianBitConverter.Big.ToInt32(buffer, 0);

                        buffer = new byte[length];
                        ByteStream.Read(buffer, 0, length);
                        string RawValue = Convert.ToBase64String(buffer);

                        XMLBuilder.Append(RawValue.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    

                    case TagType.String:
                        XMLBuilder.Append("<string ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" value=\"");

                        string value = ReadShortPrefixedString(ByteStream);

                        XMLBuilder.Append( value.ToString());
                        XMLBuilder.Append("\" />");
                        break;
                    
                    case TagType.List:
                        XMLBuilder.Append("<list ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" >");

                        ListType = (TagType) ByteStream.ReadByte();

                        CompoundStack.Push(ListType);

                        ListItemsRemaining = 0;
                        ListItemsRemaining += ByteStream.ReadByte() << 24;
                        ListItemsRemaining += ByteStream.ReadByte() << 16;
                        ListItemsRemaining += ByteStream.ReadByte() << 8;
                        ListItemsRemaining += ByteStream.ReadByte();
                        break;
                        //throw new NotImplementedException();
                    
                    case TagType.Compound:
                        XMLBuilder.Append("<compound ");
                        if (name != "")
                        {
                            XMLBuilder.Append("name=\"");
                            XMLBuilder.Append(name);
                            XMLBuilder.Append("\"");
                        }
                        XMLBuilder.Append(" >");
                        CompoundStack.Push(TagType.Compound);
                        Started = true;
                        break;
                    
                    default:
                        throw new IOException("The parser exploded.");
                }

                if (ListItemsRemaining > 0 && ID == CompoundStack.Peek())
                {
                    --ListItemsRemaining;
                    if (ListItemsRemaining == 0)
                    {
                        CompoundStack.Pop();
                        XMLBuilder.Append("</list>");
                    }
                }

                if (Started && CompoundStack.Count == 0)
                    EOF = true;

            }
            while (!EOF);


            MemoryStream M = new MemoryStream(UTF8Encoding.UTF8.GetBytes(XMLBuilder.ToString()));
            return new XmlTextReader(M);

        }

        private static string ReadShortPrefixedString(Stream ByteStream)
        {
            int one = ByteStream.ReadByte();
            int two = ByteStream.ReadByte();
            short len = (short) (one * 256 + two);

            byte[] buffer = new byte[len];
            ByteStream.Read(buffer, 0, len);
            return UTF8Encoding.UTF8.GetString(buffer);
        }
    }

    public enum TagType : byte
    {
        UNKNOWN = 255,
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
