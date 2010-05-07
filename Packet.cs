using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace spacecraft
{
    [Serializable]
    public abstract class BasePacket
    {
        abstract public byte PacketID { get; }
        public static implicit operator byte[](BasePacket pack)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream s = new MemoryStream();
            formatter.Serialize(s, pack);
            return s.ToArray();
        }
    }

    /// <summary>
    /// A packet that informs the server of a player's arrival.
    /// </summary>
    public class PlayerIDPacket : BasePacket
    {
        override public byte PacketID { get { return 0x0; }  }
        byte Version;
        byte[] Username;
        byte[] Key;
        byte Unknown; // Unused.
    }

    /// <summary>
    /// Informing the server of a player making a change to a block.
    /// </summary>
    public class BlockUpdatePacket : BasePacket
    {
        override public byte PacketID { get { return 0x05; } }
        short X;
        short Y;
        short Z;
        byte Mode;
        byte Type;
    }

    /// <summary>
    /// Informs the server of the player's position and orientation.
    /// </summary>
    public class PositionUpdatePacket : BasePacket
    {
        override public byte PacketID { get { return 0x08; } }
        byte PlayerID;
        short X;
        short Y;
        short Z;
        byte Heading;
        byte Pitch;
    }

    public class MessagePacket : BasePacket
    {
        override public byte PacketID { get { return 0x0d; } }
        public byte Unused;
        public byte[] Message;
    }


}
