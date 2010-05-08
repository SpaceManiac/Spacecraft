using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Net;

[Serializable()]
public class Map
{
	public const uint levelFormatID = 0xFC000002;
    private static short DefaultHeight=64, DefaultWidth=64, DefaultDepth = 64;
	
    public byte[] data { get; protected set; }
    public int Length { get { return xdim * ydim * zdim; } }
	
    public short xdim { get; protected set; }
	public short ydim { get; protected set; }
	public short zdim { get; protected set; }
    public Position spawn { get; protected set; }
    public byte spawnHeading { get; protected set; }
	
	public Dictionary<string, string> meta;
	public Dictionary<string, Pair<Position, byte>> landmarks;
	
    private uint physicsCount;
    private bool physicsSuspended = false;
    
    public Map()
    {
        physicsCount = 0;
        data = new byte[]{0x02,0x03,0x04,0x05};
		xdim = 0; ydim = 0; zdim = 0;
        landmarks = new Dictionary<string, Pair<Position, byte>>();
		meta = new Dictionary<string, string>();
        //StreamWriter s = new StreamWriter("test.txt");
        //foo.Serialize(s.BaseStream, this);
        //s.Close();
    }
	
	public string GetLandmarkList()
	{
		string r = "";
		foreach(KeyValuePair<string, Pair<Position, byte>> pair in landmarks) {
			r += " " + pair.Key;
		}
		return r;
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
        for(short x = 0; x < xdim; ++x) {
            for(short z = 0; z < zdim; ++z) {
                for(short y = 0; y < ydim / 2; ++y) {
					if(y == ydim / 2 - 1) {
						SetTile(x, y, z, Block.Grass);
					} else {
                    	SetTile(x, y, z, Block.Dirt);
					}
                }
            }
        }
    }
	
	// ==== Loading ====
    
    public static Map Load(string filename)
    {
        Spacecraft.Log("Loading " + filename + "...");
        if(!File.Exists(filename)) {
            Spacecraft.Log("Map does not exist: " + filename);
            return null;
        }

        switch(Path.GetExtension(filename).ToLowerInvariant()) {
            case ".fcm":
                return LoadFCM(filename);
            //case ".dat":
                //return MapLoaderDAT.Load( _world, fileName );
            default:
                throw new Exception("Unknown map file format.");
        }
    }
	
	private static Map LoadFCM(string filename) {
        FileStream fs = null;
		Map map = new Map();
        try {
            fs = File.OpenRead(filename);
            if (map.ReadHeader(fs)) {
                map.ReadMetadata(fs);
                map.ReadBlocks(fs);
				return map;
            } else {
                return null;
            }
        } catch(EndOfStreamException) {
            Spacecraft.Log("Map.LoadFCM: Unexpected end of file - possible corruption!");
            return null;
        } catch(Exception ex) {
            Spacecraft.Log("Map.Load: Error trying to read from \"" + filename + "\": " + ex.Message);
            return null;
        } finally {
            if(fs != null) {
                fs.Close();
            }
        }
    }
	
	private bool ReadHeader(FileStream fs) {
        BinaryReader reader = new BinaryReader( fs );
        try {
        	uint format = reader.ReadUInt32();
			if(format != levelFormatID) {
                Spacecraft.Log( "Map.ReadHeader: Incorrect level format id (expected {0}, got {1}).", levelFormatID, format );
                //return false;
            }
	
			// odd order is intentional: fCraft uses x,y,height to mean x,z,y
			
            xdim = (short)reader.ReadUInt16();
            if( !IsValidDimension( xdim ) ) {
                Spacecraft.Log("Map.ReadHeader: Invalid dimension specified for widthX: {0}.", xdim );
                return false;
            }

            zdim = (short)reader.ReadUInt16();
            if( !IsValidDimension( zdim ) ) {
                Spacecraft.Log( "Map.ReadHeader: Invalid dimension specified for widthY: {0}.", zdim );
                return false;
            }

            ydim = (short)reader.ReadUInt16();
            if( !IsValidDimension( ydim ) ) {
                Spacecraft.Log( "Map.ReadHeader: Invalid dimension specified for height: {0}.", ydim );
                return false;
            }

            short x = reader.ReadInt16();
            short z = reader.ReadInt16();
            short y = reader.ReadInt16();
			spawn = new Position(x, y, z);
            spawnHeading = reader.ReadByte();
            /* pitchSpawn = */ reader.ReadByte();
            if( spawn.x > xdim * 32 || spawn.y > ydim * 32 || spawn.z > zdim * 32 ||
                spawn.x < 0 || spawn.y < 0 || spawn.z < 0 ) {
                Spacecraft.Log( "Map.ReadHeader: Spawn coordinates are outside the valid range! Using center of the map instead." );
                spawn = new Position((short)(xdim / 2 * 32), (short)(ydim / 2 * 32), (short)(zdim / 2 * 32));
				spawnHeading = 0;
            }

        } catch( FormatException ex ) {
            Spacecraft.Log( "Map.ReadHeader: Cannot parse one or more of the header entries: {0}", ex.Message );
            return false;
        }
        return true;
    }

    private void ReadMetadata( FileStream fs ) {
        BinaryReader reader = new BinaryReader( fs );
        try {
            int metaSize = (int)reader.ReadUInt16();

            for( int i = 0; i < metaSize; i++ ) {
                string key = ReadLengthPrefixedString( reader );
                string value = ReadLengthPrefixedString( reader );
				if(key == "@landmark") {
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
				} else {
					meta.Add( key, value );
				}
				Spacecraft.Log( "Map.ReadMetadata: {0} = {1} ", key, value);
            }
        } catch( FormatException ex ) {
            Spacecraft.Log( "Map.ReadHeader: Cannot parse one or more of the metadata entries: {0}", ex.Message );
        }
    }

    private void ReadBlocks( FileStream fs ) {
        int blockCount = xdim * ydim * zdim;
        data = new byte[blockCount];

        GZipStream decompressor = new GZipStream( fs, CompressionMode.Decompress );
        decompressor.Read( data, 0, blockCount );
        decompressor.Flush();
    }

    string ReadLengthPrefixedString(BinaryReader reader) {
        int length = (int)reader.ReadUInt32();
        byte[] stringData = reader.ReadBytes(length);
        return ASCIIEncoding.ASCII.GetString(stringData);
    }


    // Only power-of-2 dimensions are allowed
    public static bool IsValidDimension( int dimension ) {
        return dimension > 0 && dimension % 16 == 0 && dimension < 2048;
    }

	
	// ==== Saving ====

    public bool Save( string fileName ) {
        string tempFileName = fileName + "." + (new Random().Next().ToString());

        using( FileStream fs = File.Create( tempFileName ) ) {
            try {
                WriteHeader( fs );
                WriteMetadata( fs );
                GetCompressedCopy( fs, false );
            } catch( IOException ex ) {
                Spacecraft.Log( "Map.Save: Unable to open file \"{0}\" for writing: {1}",
                               tempFileName, ex.Message );
                if( File.Exists( tempFileName ) ) {
                    File.Delete( tempFileName );
                }
                return false;
            }
        }
        if( File.Exists( fileName ) ) {
            File.Delete( fileName );
        }
        // TODO: this fails for no reason 
		File.Move( tempFileName, fileName );
		Spacecraft.Log( "Saved map succesfully to {0}", fileName );
        return true;
    }


    void WriteHeader( FileStream fs ) {
        BinaryWriter writer = new BinaryWriter( fs );
        writer.Write( levelFormatID );
        writer.Write( (ushort)xdim );
        writer.Write( (ushort)zdim );
        writer.Write( (ushort)ydim );
        writer.Write( (ushort)spawn.x );
        writer.Write( (ushort)spawn.z );
        writer.Write( (ushort)spawn.y );
        writer.Write( (byte)spawnHeading );
        writer.Write( (byte)0 );
        writer.Flush();
    }


    void WriteMetadata( FileStream fs ) {
        BinaryWriter writer = new BinaryWriter( fs );
        writer.Write( (ushort)meta.Count );
		foreach(KeyValuePair<string, Pair<Position, byte>> pair in landmarks) {
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
			            
        foreach( KeyValuePair<string, string> pair in meta ) {
            WriteLengthPrefixedString( writer, pair.Key );
            WriteLengthPrefixedString( writer, pair.Value );
        }
        writer.Flush();
    }


    void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
        byte[] stringData = ASCIIEncoding.ASCII.GetBytes( s );
        writer.Write( (uint)stringData.Length );
        writer.Write( stringData );
    }

    // zips a copy of the block array
    public void GetCompressedCopy( Stream stream, bool prependBlockCount ) {
        using( GZipStream compressor = new GZipStream( stream, CompressionMode.Compress ) ) {
            if( prependBlockCount ) {
                // convert block count to big-endian
                int convertedBlockCount = IPAddress.HostToNetworkOrder( data.Length );
                // write block count to gzip stream
                compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, sizeof( int ) );
            }
            compressor.Write( data, 0, data.Length );
        }
    }
	
	// ==== Simulation ====
    
    public void Physics(MinecraftServer srv)
    {
        if(physicsSuspended) return;
        // run twice per second
        physicsCount++;
        
        ArrayList FluidList = new ArrayList();
        ArrayList SpongeList = new ArrayList();
        
        for(short x = 0; x < xdim; ++x) {
            for(short y = 0; y < ydim; ++y) {
                 for(short z = 0; z < zdim; ++z) {
                    Block tile = GetTile(x, y, z);
                    if(physicsCount % 10 == 0) {
                        // grass
                        bool lit = true;
                        for(short y2 = (short)(y + 1); y2 < ydim; ++y2) {
                            if(BlockInfo.IsOpaque(GetTile(x, y2, z))) {
                                lit = false;
                                break;
                            }
                        }
                        if(tile == Block.Dirt && lit && MinecraftServer.rnd.NextDouble() < 0.2) {
                            SetSend(srv, x, y, z, Block.Grass);
                        }
                        if(tile == Block.Grass && !lit && MinecraftServer.rnd.NextDouble() < 0.7) {
                            SetSend(srv, x, y, z, Block.Dirt);
                        }
                    }
                    // water & lava
                    if(tile == Block.Water || tile == Block.Lava) {
                        if(tile != Block.Lava || physicsCount % 2 == 0) {
							Block under = GetTile(x, (short)(y - 1), z);
							if (!BlockInfo.IsFluid(under) && under != Block.Air) {
	                            if (GetTile((short)(x + 1), y, z) == Block.Air) {
	                                FluidList.Add(new PositionBlock((short)(x + 1), y, z, tile));
	                            }
	                            if (GetTile((short)(x - 1), y, z) == Block.Air) {
	                                FluidList.Add(new PositionBlock((short)(x - 1), y, z, tile));
	                            }
	                            if (GetTile(x, y, (short)(z + 1)) == Block.Air) {
	                                FluidList.Add(new PositionBlock(x, y, (short)(z + 1), tile));
	                            }
	                            if (GetTile(x, y, (short)(z - 1)) == Block.Air) {
	                                FluidList.Add(new PositionBlock(x, y, (short)(z - 1), tile));
	                            }
							}
                            if (GetTile(x, (short)(y - 1), z) == Block.Air) {
                                FluidList.Add(new PositionBlock(x, (short)(y - 1), z, tile));
                            }
                        }
                    }
                    // sponges
                    if(tile == Block.Sponge) {
                        for(short diffX = -2; diffX <= 2; diffX++) {
                            for(short diffY = -2; diffY <= 2; diffY++) {
                                for(short diffZ = -2; diffZ <= 2; diffZ++) {
                                    SpongeList.Add(new PositionBlock((short)(x + diffX), (short)(y + diffY), (short)(z + diffZ), Block.Air));
                                }
                            }
                        }
                    }
                }
            }
        }
        
        foreach(PositionBlock task in FluidList) {
			if(!SpongeList.Contains(new PositionBlock(task.x, task.y, task.z, Block.Air))) {
            	SetSend(srv, task.x, task.y, task.z, task.tile);
           	}
		}
        foreach(PositionBlock task in SpongeList) {
            if (GetTile(task.x, task.y, task.z) == Block.Water || GetTile(task.x, task.y, task.z) == Block.Lava || GetTile(task.x, task.y, task.z) == Block.StillWater)
            {
                SetSend(srv, task.x, task.y, task.z, task.tile);
            }
        }
    }
    
    public void Dehydrate(MinecraftServer srv)
    {
        physicsSuspended = true;
        for(short x = 0; x < xdim; ++x) {
            for(short y = 0; y < ydim; ++y) {
                 for(short z = 0; z < zdim; ++z) {
                    if(GetTile(x,y,z) == Block.Water || GetTile(x,y,z) == Block.Lava) {
                        SetSend(srv, x, y, z, Block.Air);
                    }
                }
            }
        }
        physicsSuspended = false;
    }
    
    public void SetSend(MinecraftServer srv, short x, short y, short z, Block tile)
    {
        if(x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
        SetTile(x, y, z, tile);
        srv.SendAll(Connection.PacketSetBlock(x, y, z, tile));
    }
    
    public int BlockIndex(short x, short y, short z)
    {
        return ((y * zdim + z) * xdim + x);
    }
    
    public void SetTile(short x, short y, short z, Block tile)
    {
        if(x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return;
        data[BlockIndex(x, y, z)] = (byte)tile;
    }
    
    public Block GetTile(short x, short y, short z)
    {
        if(x >= xdim || y >= ydim || z >= zdim || x < 0 || y < 0 || z < 0) return Block.Adminium;
        return (Block)data[BlockIndex(x, y, z)];
    }
}
