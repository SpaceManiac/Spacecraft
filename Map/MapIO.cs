using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

// Thanks to fragmer of fCraft for letting us use his map format and map loading code!
// http://fcraft.net/
// Adapted for Spacecraft

namespace spacecraft
{
	public partial class Map
	{
		public const string levelName = "level.fcm";

		// continued in MapTag.cs

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
					// TODO: FCMFactory class.
				case ".dat":
					return DatLevelFactory.Load(filename);
				case ".nbt":
				case ".mclevel":
				case ".mclvl":
					return McLevelFactory.Load(filename);
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
				Spacecraft.Log("MapTag.Load: Error trying to read from \"" + filename + "\": " + ex.Message);
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
				
				landmarks.Clear();

				for (int i = 0; i < metaSize; i++)
				{
					string key = ReadLengthPrefixedString(reader);
					string value = ReadLengthPrefixedString(reader);
					switch (key)
					{
						case "@landmark":
							string[] keyval = value.Split(new char[] { '=' });
							string name = keyval[0];
							string[] parts = keyval[1].Split(new char[] { ',' });
							
							short x = Convert.ToInt16(parts[0]);
							short z = Convert.ToInt16(parts[1]);
							short y = Convert.ToInt16(parts[2]);
							byte heading = Convert.ToByte(parts[3]);

							Position pos = new Position(x, y, z);
							landmarks.Add(name, new Pair<Position, byte>(pos, heading));
							break;

						case "@heights":
							Heights = new int[xdim, zdim];
							string[] tuples = value.Split('|');
							foreach (var item in tuples)
							{
								string[] parts2 = item.Split(',');
								Heights[int.Parse(parts2[0]), int.Parse(parts2[1])] = int.Parse(parts2[2]);
							}
							break;

						default:
							meta.Add(key, value);
							break;
					}
				}
			}
			catch (FormatException ex)
			{
				Spacecraft.LogError("Map.ReadHeader: Cannot parse one or more of the metadata entries", ex);
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
			try {
				using (FileStream fs = File.Create(tempFileName))
				{
					try
					{
						WriteHeader(fs);
						WriteMetadata(fs);
						GetCompressedCopy(fs, false, false);
					}
					catch (IOException ex)
					{
						Spacecraft.LogError("unable to open file \"" + tempFileName + "\" for writing: {1}", ex);
						if (File.Exists(tempFileName)) {
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
			}
			catch(Exception ex) {
				Spacecraft.LogError("unknown error while saving map", ex);
				return false;
			}
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
