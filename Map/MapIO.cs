using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;

// Thanks to fragmer of fCraft for letting us use his map format and map loading code!
// http://fcraft.net/
// Adapted for Spacecraft

namespace spacecraft
{
	public partial class Map
	{
		public const string levelName = "level.fcm";

		// continued in Map.cs

		// ==== Loading ====

		public static Map Load(string filename)
		{
			Spacecraft.Log("Loading " + filename + "...");
			if (!File.Exists(filename))
			{
				throw new FileLoadException("Map does not exist.");
			}

			switch (Path.GetExtension(filename).ToLowerInvariant())
			{
				case ".fcm":
					return LoadFCM(filename);
				case ".dat":
					return DatLoading.Load(filename);
				default:
					throw new FileLoadException("Unknown map file format.");
			}
		}

		public static Map LoadFCM(string filename)
		{
			FileStream fs = null;
			Map map = new Map();
			try
			{
				fs = File.OpenRead(filename);
				if (map.ReadHeader(fs))
				{
					map.ReadMetadata(fs);
					map.ReadBlocks(fs);
					return map;
				}
				else
				{
					return null;
				}
			}
			catch (EndOfStreamException)
			{
				Spacecraft.Log("Map.LoadFCM: Unexpected end of file - possible corruption!");
				return null;
			}
			/*catch (Exception ex)
			{
				Spacecraft.Log("Map.Load: Error trying to read from \"" + filename + "\": " + ex.Message);
				return null;
			}*/
			finally
			{
				if (fs != null)
				{
					fs.Close();
				}
			}
		}

		private bool ReadHeader(FileStream fs)
		{
			BinaryReader reader = new BinaryReader(fs);
			try
			{
				uint format = reader.ReadUInt32();
				if (format != Map.levelFormatID)
				{
					Spacecraft.Log("Map.ReadHeader: Incorrect level format id (expected {0}, got {1}).", Map.levelFormatID, format);
					//return false;
				}

				// odd order is intentional: fCraft uses xDiff,yDiff,height to mean xDiff,zDiff,yDiff

				xdim = (short)reader.ReadUInt16();
				if (!Map.IsValidDimension(xdim))
				{
					Spacecraft.Log("Map.ReadHeader: Invalid dimension specified for widthX: {0}.", xdim);
					return false;
				}

				zdim = (short)reader.ReadUInt16();
				if (!Map.IsValidDimension(zdim))
				{
					Spacecraft.Log("Map.ReadHeader: Invalid dimension specified for widthY: {0}.", zdim);
					return false;
				}

				ydim = (short)reader.ReadUInt16();
				if (!Map.IsValidDimension(ydim))
				{
					Spacecraft.Log("Map.ReadHeader: Invalid dimension specified for height: {0}.", ydim);
					return false;
				}

				short x = reader.ReadInt16();
				short z = reader.ReadInt16();
				short y = reader.ReadInt16();
				spawn = new Position(x, y, z);
				spawnHeading = reader.ReadByte();
				/* pitchSpawn = */
				reader.ReadByte();
				if (spawn.x > xdim * 32 || spawn.y > ydim * 32 || spawn.z > zdim * 32 ||
					spawn.x < 0 || spawn.y < 0 || spawn.z < 0)
				{
					Spacecraft.Log("Map.ReadHeader: Spawn coordinates are outside the valid range! Using center of the map instead.");
					spawn = new Position((short)(xdim / 2 * 32), (short)(ydim / 2 * 32), (short)(zdim / 2 * 32));
					spawnHeading = 0;
				}

			}
			catch (FormatException ex)
			{
				Spacecraft.Log("Map.ReadHeader: Cannot parse one or more of the header entries: {0}", ex.Message);
				return false;
			}
			return true;
		}

		private void ReadMetadata(FileStream fs)
		{
			BinaryReader reader = new BinaryReader(fs);
			try
			{
				int metaSize = (int)reader.ReadUInt16();

				for (int i = 0; i < metaSize; i++)
				{
					string key = ReadLengthPrefixedString(reader);
					string value = ReadLengthPrefixedString(reader);
					switch (key)
					{
						case "@landmark:":
							int p = value.IndexOf("=");
							string name = value.Substring(0, p);
							int p2 = value.IndexOf(",", p + 1);
							short x = Convert.ToInt16(value.Substring(p + 1, p2 - p));
							int p3 = value.IndexOf(",", p2 + 1);
							short z = Convert.ToInt16(value.Substring(p2 + 1, p3 - p2));
							int p4 = value.IndexOf(",", p3 + 1);
							short y = Convert.ToInt16(value.Substring(p3 + 1, p4 - p3));
							int p5 = value.IndexOf(",", p4 + 1);
							byte heading = Convert.ToByte(value.Substring(p5 + 1));

							Position pos = new Position(x, y, z);
							landmarks.Add(name, new Pair<Position, byte>(pos, heading));
							break;

						case "@heights":
							Heights = new int[xdim, zdim];
							string[] tuples = value.Split('|');
							foreach (var item in tuples)
							{
								string[] parts = item.Split(',');
								Heights[int.Parse(parts[0]), int.Parse(parts[1])] = int.Parse(parts[2]);
							}
							break;

						default:
							meta.Add(key, value);
							break;
					}
					Spacecraft.Log("Map.ReadMetadata: {0} = {1} ", key, value);
				}
			}
			catch (FormatException ex)
			{
				Spacecraft.Log("Map.ReadHeader: Cannot parse one or more of the metadata entries: {0}", ex.Message);
			}
		}

		private void ReadBlocks(FileStream fs)
		{
			int blockCount = xdim * ydim * zdim;
			data = new byte[blockCount];

			GZipStream decompressor = new GZipStream(fs, CompressionMode.Decompress);
			decompressor.Read(data, 0, blockCount);
			decompressor.Flush();

	}

		private static string ReadLengthPrefixedString(BinaryReader reader)
		{
			int length = (int)reader.ReadUInt32();
			byte[] stringData = reader.ReadBytes(length);
			return ASCIIEncoding.ASCII.GetString(stringData);
		}

		public static bool IsValidDimension(int dimension)
		{
			return dimension > 0 && dimension % 16 == 0 && dimension < 2048;
		}

		// ==== Saving ====

		public bool Save(string fileName)
		{
			string tempFileName = fileName + "." + (Spacecraft.random.Next().ToString());

			using (FileStream fs = File.Create(tempFileName))
			{
				try
				{
					WriteHeader(fs);
					WriteMetadata(fs);
					GetCompressedCopy(fs, false);
				}
				catch (IOException ex)
				{
					Spacecraft.Log("Map.Save: Unable to open file \"{0}\" for writing: {1}",
								   tempFileName, ex.Message);
					if (File.Exists(tempFileName))
					{
						File.Delete(tempFileName);
					}
					return false;
				}
			}
			if (File.Exists(fileName))
			{
				File.Delete(fileName);
			}

			File.Move(tempFileName, fileName);
			File.Delete(tempFileName);
			return true;
		}

		void WriteHeader(FileStream fs)
		{
			BinaryWriter writer = new BinaryWriter(fs);
			writer.Write(levelFormatID);
			writer.Write((ushort)xdim);
			writer.Write((ushort)zdim);
			writer.Write((ushort)ydim);
			writer.Write((ushort)spawn.x);
			writer.Write((ushort)spawn.z);
			writer.Write((ushort)spawn.y);
			writer.Write((byte)spawnHeading);
			writer.Write((byte)0);
			writer.Flush();
		}

		void WriteMetadata(FileStream fs)
		{
			BinaryWriter writer = new BinaryWriter(fs);
			writer.Write((ushort)(meta.Count + landmarks.Count));
			foreach (KeyValuePair<string, Pair<Position, byte>> pair in landmarks)
			{
				string key = pair.Key;
				Position p = pair.Value.First;
				byte heading = pair.Value.Second;
				WriteLengthPrefixedString(writer, "@landmark");

				StringBuilder data = new StringBuilder();
				data.Append(key);
				data.Append("=");
				data.Append(p.x);
				data.Append(",");
				data.Append(p.z);
				data.Append(",");
				data.Append(p.y);
				data.Append(",");
				data.Append(heading);
				WriteLengthPrefixedString(writer, data.ToString());

				StringBuilder HeightString = new StringBuilder();
				for (int x = 0; x < Heights.GetLength(0); x++)
				{
					for (int z = 0; z < Heights.GetLength(1); z++)
					{
						HeightString.Append(x);
						HeightString.Append(",");
						HeightString.Append(z);
						HeightString.Append(",");
						HeightString.Append(Heights[x,z]);
					}    
				}

			}

			foreach (KeyValuePair<string, string> pair in meta)
			{
				WriteLengthPrefixedString(writer, pair.Key);
				WriteLengthPrefixedString(writer, pair.Value);
			}
			writer.Flush();
		}

		void WriteLengthPrefixedString(BinaryWriter writer, string s)
		{
			byte[] stringData = ASCIIEncoding.ASCII.GetBytes(s);
			writer.Write((uint)stringData.Length);
			writer.Write(stringData);
		}
	}
}
