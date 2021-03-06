using NBT;
using System;
using System.IO;
using System.IO.Compression;

namespace spacecraft
{
	public static partial class McLevelFactory
	{
		public static Map Load(string filename)
		{
			StreamReader RawReader = new StreamReader(filename);
			GZipStream Reader = new GZipStream(RawReader.BaseStream, CompressionMode.Decompress);

			BinaryTag Tree = NbtParser.ParseTagStream(Reader);
			
			Reader.Close();
			RawReader.Close();

			BinaryTag MapTag  = new BinaryTag(){ Payload = null, };

			// Find the MapTag data.
			foreach (var Item in (BinaryTag[])Tree.Payload)
			{
				if (Item.Name == "Map")
				{
					MapTag = Item;
					break;
				}
			}

			if (!(MapTag.Payload is BinaryTag[]))
				throw new IOException("Map tree did not or contained invalid Map tag!");

			BinaryTag[] Items = MapTag.Payload as BinaryTag[];

			Map MapInProgress = new Map();

			foreach (var Item in Items)
			{
				switch (Item.Name)
				{
					case "Width":
						MapInProgress.xdim = (short)Item.Payload;
						break;
					case "Height":
						MapInProgress.ydim = (short) Item.Payload;
						break;
					case "Length":
						MapInProgress.zdim = (short)Item.Payload;
						break;
					case "Blocks":
						MapInProgress.CopyBlocks((byte[])Item.Payload, 0);
						break;
					case "Spawn":
						BinaryTag[] List = (BinaryTag[]) Item.Payload;
						short x = (short) List[0].Payload;
						short y = (short)List[1].Payload;
						short z = (short)List[2].Payload;

						x *= 32;
						y *= 32;
						z *= 32;

						MapInProgress.SetSpawn(new Position(x, y, z), 0);

						break;
					default: 
						break;
				}
			}

			// We do our own optimizations.
			MapInProgress.ReplaceAll(Block.StillWater, Block.Water, -1);
			MapInProgress.ReplaceAll(Block.StillLava, Block.Lava, -1);

			return MapInProgress;
		}
	}
}
