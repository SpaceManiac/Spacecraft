using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace spacecraft
{
	public partial class Map
	{
		// continued in MapIO.cs

		public delegate void BlockChangeHandler(Map map, BlockPosition pos, Block type);
		/// <summary>
		/// Triggred when a block is changed by map processes
		/// </summary>
		public event BlockChangeHandler BlockChange;

		public const uint levelFormatID = 0xFC000002;
		/// <summary>
		/// Max changse to the map per physics tick.
		/// </summary>
		public const int MaxPhysicsPerTick = 1000;

		private static short DefaultHeight, DefaultWidth, DefaultDepth;
		public byte[] data { get; protected set; }
		public int Length { get { return xdim * ydim * zdim; } }

		public short xdim;
		public short ydim;
		public short zdim;
		public Position spawn;
		public byte spawnHeading;

		public Dictionary<string, string> meta = new Dictionary<string, string>();
		public Dictionary<string, Pair<Position, byte>> landmarks = new Dictionary<string, Pair<Position, byte>>();

        public Dictionary<Position, string> teleportNames = new Dictionary<Position, string>();
        public Dictionary<Position, Pair<Position, byte>> teleportDests = new Dictionary<Position, Pair<Position, byte>>();


		public Map()
		{
			physicsCount = 0;
			//data = new byte[];
			xdim = 0;
			ydim = 0;
			zdim = 0;

			DefaultDepth = (short)Config.GetInt("depth", 64);
			DefaultHeight = (short)Config.GetInt("height", 64);
			DefaultWidth = (short)Config.GetInt("width", 64);

			if (!IsValidDimension(DefaultDepth) || !IsValidDimension(DefaultHeight) || !IsValidDimension(DefaultWidth))
			{
				Spacecraft.Log("Specified default map dimensions are invalid, defaulting to 256x256x256");
				DefaultDepth = DefaultHeight = DefaultWidth = 256;
			}

			DefaultDepth = (short)Math.Max((short)0, Math.Min(DefaultDepth, (short)2048));
			DefaultWidth = (short)Math.Max((short)0, Math.Min(DefaultWidth, (short)2048));
			DefaultHeight = (short)Math.Max((short)0, Math.Min(DefaultHeight, (short)2048));
		}

		public string[] GetLandmarkList()
		{
			string[] l = new string[landmarks.Keys.Count];
            landmarks.Keys.CopyTo(l, 0);
			return l;
		}

		public void SetSpawn(Position p, byte heading)
		{
			spawn = p;
			spawnHeading = heading;
		}

		public void Generate()
		{
			Generate(false);
		}

		public void Generate(bool skipTcl)
		{
			Spacecraft.Log("Generating map...");

			xdim = DefaultWidth;
			ydim = DefaultHeight;
			zdim = DefaultDepth;

			data = new byte[xdim * ydim * zdim];

			DateTime Begin = DateTime.Now;

			if (Scripting.Initialized && !skipTcl && Scripting.HookDefined("onLevelGeneration"))
			{
				int value = Scripting.ExecuteHook("onLevelGeneration", xdim + " " + ydim + " " + zdim);
				if (!Scripting.IsOk(value))
				{
					Spacecraft.LogError("Tcl map generation failed", new SpacecraftException("Tcl map generation failed:\n" + Scripting.Interpreter.Result));
					Generate(true);
				}
			}
			else
			{
				physicsCount = 0;
				spawn = new Position((short)(16 * xdim), (short)(16 * ydim + 48), (short)(16 * zdim));
				// Spawn the player in the (approximate) center of the map. Each block is 32x32x32 pixels.

				for (short x = 0; x < xdim; ++x)
				{
					if (x == (short)(xdim / 2)) {
						Spacecraft.Log("Generation 50% complete");
					}
					
					for (short z = 0; z < zdim; ++z) {
						for (short y = 0; y < ydim / 2; ++y) {
							if (y == ydim / 2 - 1) {
								SetTile_Fast(x, y, z, Block.Grass);
							} else {
								SetTile_Fast(x, y, z, Block.Dirt);
							}
						}
					}
				}
			}
			DateTime End = DateTime.Now;
			Spacecraft.Log("Generation complete. Took " + (End - Begin).TotalMilliseconds + "ms");
		}

        public void GetCompressedCopy(Stream stream, bool prependBlockCount)
        {
            GetCompressedCopy(stream, prependBlockCount, true);
        }

		// zips a copy of the block array
		public void GetCompressedCopy(Stream stream, bool prependBlockCount, bool translateForClient)
		{
			using (GZipStream compressor = new GZipStream(stream, CompressionMode.Compress))
			{
				if (prependBlockCount)
				{
					// convert block count to network order
					int convertedBlockCount = IPAddress.HostToNetworkOrder(data.Length);
					// write block count to gzip stream
					compressor.Write(BitConverter.GetBytes(convertedBlockCount), 0, sizeof(int));
				}

                byte[] temp = data;

                if (translateForClient)
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        temp[i] = (byte) BlockInfo.Translate((Block)temp[i]);
                    }

                }
				compressor.Write(temp, 0, temp.Length);
			}

            GC.Collect();
		}

		public void CopyBlocks(byte[] source, int offset)
		{
			data = new byte[xdim * ydim * zdim];
			Array.Copy(source, offset, data, 0, data.Length);
		}

		public bool ValidateBlockTypes()
		{
			for (int i = 0; i < data.Length; ++i)
			{
				if (data[i] > (byte)Block.Obsidian)
				{
					return false;
				}
			}
			return true;
		}

        /// <summary>
        /// Search/replace blocks.
        /// </summary>
        /// <param name="From">The Block to do a search for.</param>
        /// <param name="To">The Block to replace with.</param>
        /// <param name="max">The limit on how many blocks to replace; use -1 for no limit.</param>
		public void ReplaceAll(Block From, Block To, int max)
		{
			lock (PhysicsMutex)
			{
				int total = 0;
				for (short x = 0; x < xdim; x++)
				{
					for (short y = 0; y < ydim; y++)
					{
						for (short z = 0; z < zdim; z++)
						{
							if (GetTile(x, y, z) == From)
							{
								SetTile(x, y, z, To);
								if (++total >= max && max != -1) return;
							}
						}
					}
				}
			} // lock(PhysicsMutex)
		}

		public void Dehydrate()
		{
			lock (PhysicsMutex)
			{
				for (short x = 0; x < xdim; x++)
				{
					for (short y = 0; y < ydim; y++)
					{
						for (short z = 0; z < zdim; z++)
						{
							if (GetTile(x, y, z) == Block.Water || GetTile(x, y, z) == Block.Lava)
							{
								SetTile(x, y, z, Block.Air);
							}
						}
					}
				}
			} // lock(PhysicsMutex)
		}

		public void SetTile(short x, short y, short z, Block tile) {
			SetTile(x,y,z,tile,true);
		}

		public void SetTile(short x, short y, short z, Block tile, bool calcHeights)
		{

            if (tile == Block.Undefined)
                throw new ArgumentException("Tried to set undefined block.");

			if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;

			if(calcHeights) {
				RecalculateHeight(x, y, z, BlockInfo.IsOpaque(tile));
	
				if (Heights[x, z] == y && tile == Block.Dirt) {
					tile = Block.Grass;
				}
			}
		            

			BlockPosition pos = new BlockPosition(x, y, z);

			data[BlockIndex(x, y, z)] = (byte)tile;
			AlertPhysicsAround(pos);

			if (BlockChange != null)
				BlockChange(this, pos, tile);
		}


        /// <summary>
        /// Sets a tile and bypasses all of the validation checks, height calculations, physics calculations and events. USE WITH EXTREME CAUTION
        /// HERE BE DRAGONS!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="tile"></param>
		public void SetTile_Fast(short x, short y, short z, Block tile)
		{
			data[BlockIndex(x, y, z)] = (byte)tile;
		}

		public Block GetTile(BlockPosition pos)
		{
			return GetTile(pos.x, pos.y, pos.z);
		}

		public Block GetTile(short x, short y, short z)
		{
			if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return Block.Adminium;
			//if (x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) throw new IndexOutOfRangeException(x.ToString() + "," + y.ToString() + "," + z.ToString() + " invalid");
			return (Block)data[BlockIndex(x, y, z)];
		}

		public int BlockIndex(short x, short y, short z)
		{
			return ((y * zdim + z) * xdim + x);
		}

		public static int BlockIndex(short x, short y, short z, short xdim, short zdim)
		{
			return ((y * zdim + z) * xdim + x);
		}
	}
}