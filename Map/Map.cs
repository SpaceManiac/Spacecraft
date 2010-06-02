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

        public delegate void BlockChangeHandler(Map map, BlockPosition pos, Block type);
        /// <summary>
        /// Triggred when a block is changed by map processes
        /// </summary>
        public event BlockChangeHandler BlockChange;
		
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
        public bool PhysicsSuspended = false;

        public Map()
        {
            physicsCount = 0;
            //data = new byte[];
            xdim = 0; 
            ydim = 0; 
            zdim = 0;
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
            spawn = new Position((short)(16 * xdim), (short)(16 * ydim + 64), (short)(16 * zdim));
            // Spawn the player in the (approximate) center of the map. Each block is 32x32x32 pixels.
            data = new byte[Length];
            for (short x = 0; x < xdim; ++x) {
                for (short z = 0; z < zdim; ++z) {
                    for (short y = 0; y < ydim / 2; ++y) {
                        if (y == ydim / 2 - 1) {
                            SetTile(x, y, z, Block.Grass);
                        } else {
                            SetTile(x, y, z, Block.Dirt);
                        }
                    }
                }
            }
        }

        // zips a copy of the block array
        public void GetCompressedCopy(Stream stream, bool prependBlockCount)
        {
            using (GZipStream compressor = new GZipStream(stream, CompressionMode.Compress)) {
                if (prependBlockCount) {
                    // convert block count to big-endian
                    int convertedBlockCount = IPAddress.HostToNetworkOrder(data.Length);
                    // write block count to gzip stream
                    compressor.Write(BitConverter.GetBytes(convertedBlockCount), 0, sizeof(int));
                }
                compressor.Write(data, 0, data.Length);
            }
        }

        // ==== Simulation ====

        public void Physics()
        {
            if (PhysicsSuspended) return;
            // run twice per second
            physicsCount++;

            List<PhysicsTask> FluidList = new List<PhysicsTask>();
            List<PhysicsTask> SandList = new List<PhysicsTask>();
            List<PhysicsTask> SpongeList = new List<PhysicsTask>();
            
            short y_1 = (short)(ydim / 2 - 1);
            short y_2 = (short)(ydim / 2 - 2);
            short z2 = (short)(zdim - 1);
            for (short x = 0; x < xdim; ++x) {
            	if(GetTile(x, y_1, 0) == Block.Air) {
            		FluidList.Add(new PhysicsTask(x, y_1, 0, Block.Water));
            	}
            	if(GetTile(x, y_1, z2) == Block.Air) {
            		FluidList.Add(new PhysicsTask(x, y_1, z2, Block.Water));
            	}
            	if(GetTile(x, y_2, 0) == Block.Air) {
            		FluidList.Add(new PhysicsTask(x, y_2, 0, Block.Water));
            	}
            	if(GetTile(x, y_2, z2) == Block.Air) {
            		FluidList.Add(new PhysicsTask(x, y_2, z2, Block.Water));
            	}
            }
            short x2 = (short)(xdim - 1);
            for (short z = 1; z < zdim - 1; ++z) {
            	if(GetTile(0, y_1, z) == Block.Air) {
            		FluidList.Add(new PhysicsTask(0, y_1, z, Block.Water));
            	}
            	if(GetTile(x2, y_1, z) == Block.Air) {
            		FluidList.Add(new PhysicsTask(x2, y_1, z, Block.Water));
            	}
            	if(GetTile(0, y_2, z) == Block.Air) {
            		FluidList.Add(new PhysicsTask(0, y_2, z, Block.Water));
            	}
            	if(GetTile(x2, y_2, z) == Block.Air) {
            		FluidList.Add(new PhysicsTask(x2, y_2, z, Block.Water));
            	}
            }

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
                                SetSend(x, y, z, Block.Grass);
                            }
                            if (tile == Block.Grass && !lit && Spacecraft.random.NextDouble() < 0.7)
                            {
                                SetSend(x, y, z, Block.Dirt);
                            }
                        }
                        // water & lava
                        if (tile == Block.Water || tile == Block.Lava)
                        {
                            if (tile != Block.Lava || physicsCount % 2 == 0)
                            {
                                Block under = GetTile(x, (short)(y - 1), z);
                                if (true)//!BlockInfo.IsFluid(under) && under != Block.Air)
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
                        // sand and gravel
                        if (tile == Block.Sand || tile == Block.Gravel)
                        {
                        	short lowY = y;
                        	if(GetTile(x, (short)(lowY - 1), z) == Block.Air || BlockInfo.IsFluid(GetTile(x, (short)(lowY - 1), z))) {
                        		--lowY;
                        	}
                        	if(lowY != y) {
	                        	SandList.Add(new PhysicsTask(x, y, z, Block.Air));
	                        	SandList.Add(new PhysicsTask(x, lowY, z, tile));
	                        }
                        }
                    }
                }
            }

            foreach (PhysicsTask task in FluidList) {
                if (!SpongeList.Contains(new PhysicsTask(task.x, task.y, task.z, Block.Air))) {
                    SetSend(task.x, task.y, task.z, task.tile);
                }
            }
            foreach (PhysicsTask task in SandList) {
            	SetSend(task.x, task.y, task.z, task.tile);
            }
            foreach (PhysicsTask task in SpongeList) {
                if (BlockInfo.IsFluid(GetTile(task.x, task.y, task.z))) {
                    SetSend(task.x, task.y, task.z, task.tile);
                }
            }
        }

        public void Dehydrate(NewServer Serv)
        {
            PhysicsSuspended = true;
            for (short x = 0; x < xdim; ++x)
            {
                for (short y = 0; y < ydim; ++y)
                {
                    for (short z = 0; z < zdim; ++z)
                    {
                        if (GetTile(x, y, z) == Block.Water || GetTile(x, y, z) == Block.Lava)
                        {
                            Serv.ChangeBlock(new BlockPosition(x, y, z), Block.Air);
                        }
                    }
                }
            }
            PhysicsSuspended = false;
        }

        public void SetSend(short x, short y, short z, Block tile)
        {
            if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
            data[BlockIndex(x, y, z)] = (byte)tile;
            if(BlockChange != null)
            	BlockChange(this, new BlockPosition(x, y, z), tile);
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