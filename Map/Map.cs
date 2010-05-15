using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Net;

namespace spacecraft
{
    [Serializable()]
    public partial class Map
    {
		// continued in MapIO.cs
		
        public const uint levelFormatID = 0xFC000002;
        private static short DefaultHeight = 64, DefaultWidth = 64, DefaultDepth = 64;

        public byte[] data { get; protected set; }
        public int Length { get { return xdim * ydim * zdim; } }

        public short xdim { get; protected set; }
        public short ydim { get; protected set; }
        public short zdim { get; protected set; }
        public Position spawn { get; protected set; }
        public byte spawnHeading { get; protected set; }

        public Dictionary<string, string> meta = new Dictionary<string, string>();
        public Dictionary<string, Pair<Position, byte>> landmarks = new Dictionary<string, Pair<Position, byte>>();

        private uint physicsCount;
        private bool physicsSuspended = false;

        public Map()
        {
            physicsCount = 0;
            data = new byte[] { 0x02, 0x03, 0x04, 0x05 };
            xdim = 0; ydim = 0; zdim = 0;
            //StreamWriter s = new StreamWriter("test.txt");
            //foo.Serialize(s.BaseStream, this);
            //s.Close();
        }

        public string[] GetLandmarkList()
        {
            List<string> l = new List<string>();
            foreach (KeyValuePair<string, Pair<Position, byte>> pair in landmarks)
            {
                l.Add(pair.Key);
            }
            return l.ToArray();
        }

        public void SetSpawn(Position p, byte heading)
        {
            spawn = p;
            spawnHeading = heading;
        }

        public void Generate()
        {
            physicsCount = 0;
            Spacecraft.Log("Generating map...");

            xdim = DefaultWidth;
            ydim = DefaultHeight;
            zdim = DefaultDepth;
            spawn = new Position((short)(16 * xdim), (short)(16 * ydim + 48), (short)(16 * zdim));
            // Spawn the player in the (approximate) center of the map. Each block is 32x32x32 pixels.
            data = new byte[Length];
            for (short x = 0; x < xdim; ++x)
            {
                for (short z = 0; z < zdim; ++z)
                {
                    for (short y = 0; y < ydim / 2; ++y)
                    {
                        if (y == ydim / 2 - 1)
                        {
                            SetTile(x, y, z, Block.Grass);
                        }
                        else
                        {
                            SetTile(x, y, z, Block.Dirt);
                        }
                    }
                }
            }
        }

        // zips a copy of the block array
        public void GetCompressedCopy(Stream stream, bool prependBlockCount)
        {
            using (GZipStream compressor = new GZipStream(stream, CompressionMode.Compress))
            {
                if (prependBlockCount)
                {
                    // convert block count to big-endian
                    int convertedBlockCount = IPAddress.HostToNetworkOrder(data.Length);
                    // write block count to gzip stream
                    compressor.Write(BitConverter.GetBytes(convertedBlockCount), 0, sizeof(int));
                }
                compressor.Write(data, 0, data.Length);
            }
        }

        // ==== Simulation ====

        public void Physics(MinecraftServer srv)
        {
            if (physicsSuspended) return;
            // run twice per second
            physicsCount++;

            List<PhysicsTask> FluidList = new List<PhysicsTask>();
            List<PhysicsTask> SpongeList = new List<PhysicsTask>();

            for (short x = 0; x < xdim; ++x)
            {
                for (short y = 0; y < ydim; ++y)
                {
                    for (short z = 0; z < zdim; ++z)
                    {
                        Block tile = GetTile(x, y, z);
                        if (physicsCount % 10 == 0)
                        {
                            // grass
                            bool lit = true;
                            for (short y2 = (short)(y + 1); y2 < ydim; ++y2)
                            {
                                if (BlockInfo.IsOpaque(GetTile(x, y2, z)))
                                {
                                    lit = false;
                                    break;
                                }
                            }
                            if (tile == Block.Dirt && lit && Spacecraft.random.NextDouble() < 0.2)
                            {
                                SetSend(srv, x, y, z, Block.Grass);
                            }
                            if (tile == Block.Grass && !lit && Spacecraft.random.NextDouble() < 0.7)
                            {
                                SetSend(srv, x, y, z, Block.Dirt);
                            }
                        }
                        // water & lava
                        if (tile == Block.Water || tile == Block.Lava)
                        {
                            if (tile != Block.Lava || physicsCount % 2 == 0)
                            {
                                Block under = GetTile(x, (short)(y - 1), z);
                                if (!BlockInfo.IsFluid(under) && under != Block.Air)
                                {
                                    if (GetTile((short)(x + 1), y, z) == Block.Air)
                                    {
                                        FluidList.Add(new PhysicsTask((short)(x + 1), y, z, tile));
                                    }
                                    if (GetTile((short)(x - 1), y, z) == Block.Air)
                                    {
                                        FluidList.Add(new PhysicsTask((short)(x - 1), y, z, tile));
                                    }
                                    if (GetTile(x, y, (short)(z + 1)) == Block.Air)
                                    {
                                        FluidList.Add(new PhysicsTask(x, y, (short)(z + 1), tile));
                                    }
                                    if (GetTile(x, y, (short)(z - 1)) == Block.Air)
                                    {
                                        FluidList.Add(new PhysicsTask(x, y, (short)(z - 1), tile));
                                    }
                                }
                                if (GetTile(x, (short)(y - 1), z) == Block.Air)
                                {
                                    FluidList.Add(new PhysicsTask(x, (short)(y - 1), z, tile));
                                }
                            }
                        }
                        // sponges
                        if (tile == Block.Sponge)
                        {
                            for (short diffX = -2; diffX <= 2; diffX++)
                            {
                                for (short diffY = -2; diffY <= 2; diffY++)
                                {
                                    for (short diffZ = -2; diffZ <= 2; diffZ++)
                                    {
                                        SpongeList.Add(new PhysicsTask((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ), Block.Air));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (PhysicsTask task in FluidList)
            {
                if (!SpongeList.Contains(new PhysicsTask(task.x, task.y, task.z, Block.Air)))
                {
                    SetSend(srv, task.x, task.y, task.z, task.tile);
                }
            }
            foreach (PhysicsTask task in SpongeList)
            {
                if (GetTile(task.x, task.y, task.z) == Block.Water || GetTile(task.x, task.y, task.z) == Block.Lava || GetTile(task.x, task.y, task.z) == Block.StillWater)
                {
                    SetSend(srv, task.x, task.y, task.z, task.tile);
                }
            }
        }

        public void Dehydrate(MinecraftServer srv)
        {
            physicsSuspended = true;
            for (short x = 0; x < xdim; ++x)
            {
                for (short y = 0; y < ydim; ++y)
                {
                    for (short z = 0; z < zdim; ++z)
                    {
                        if (GetTile(x, y, z) == Block.Water || GetTile(x, y, z) == Block.Lava)
                        {
                            SetSend(srv, x, y, z, Block.Air);
                        }
                    }
                }
            }
            physicsSuspended = false;
        }

        public void SetSend(MinecraftServer srv, short x, short y, short z, Block tile)
        {
            if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
            SetTile(x, y, z, tile);
            srv.SendAll(Connection.PacketSetBlock(x, y, z, tile));
        }

        public int BlockIndex(short x, short y, short z)
        {
            return ((y * zdim + z) * xdim + x);
        }

        public int BlockIndex2(short x, short y, short z)
        {
            return ((y * zdim + z) * xdim + x);
        }

		public static int BlockIndex2(short x, short y, short z, short xdim, short zdim)
		{
			return ((y * zdim + z) * xdim + x);
		}

        public void SetTile(short x, short y, short z, Block tile)
        {
            if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
            data[BlockIndex(x, y, z)] = (byte)tile;
        }

        public Block GetTile(short x, short y, short z)
        {
            if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return Block.Adminium;
            return (Block)data[BlockIndex(x, y, z)];
        }
    }
}