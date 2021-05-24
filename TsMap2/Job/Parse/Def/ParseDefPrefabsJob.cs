﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Serilog;
using TsMap2.Factory;
using TsMap2.Helper;
using TsMap2.Model;
using TsMap2.Scs;

namespace TsMap2.Job.Parse.Def {
    public class ParseDefPrefabsJob : ThreadJob {
        private bool _isFirstFileRead;

        protected override void Do() {
            Log.Debug( "[Job][Prefab] Loading" );

            // --- Check RFS
            if ( this.Store().Rfs == null )
                throw new JobException( "[Job][Prefab] The root file system was not initialized. Check the game path", this.JobName(), null );

            ScsDirectory worldDirectory = this.Store().Rfs.GetDirectory( ScsPath.Def.WorldPath );
            if ( worldDirectory == null ) {
                var message = $"[Job][Prefab] Could not read '{ScsPath.Def.WorldPath}' dir";
                throw new JobException( message, this.JobName(), ScsPath.Def.WorldPath );
                // Log.Error( "[Job][Prefab] Could not read '{0}' dir", ScsPath.Def.WorldPath );
            }

            List< ScsFile > prefabFiles = worldDirectory.GetFiles( ScsPath.Def.PrefabFileName );
            if ( prefabFiles == null ) {
                var message = $"[Job][Prefab] Could not read {ScsPath.Def.PrefabFileName} files";
                throw new JobException( message, this.JobName(), ScsPath.Def.PrefabFileName );
                // Log.Error( "[Job][Prefab] Could not read {0} files", ScsPath.Def.PrefabFileName );
                // return;
            }

            var _isFirstFileRead = false;
            foreach ( ScsFile prefabFile in prefabFiles ) {
                if ( !prefabFile.GetFileName().StartsWith( "prefab" ) ) continue;

                byte[]   data  = prefabFile.Entry.Read();
                string[] lines = Encoding.UTF8.GetString( data ).Split( '\n' );

                var token    = 0UL;
                var path     = "";
                var category = "";

                // -- Raw generation
                if ( !_isFirstFileRead ) {
                    RawHelper.SaveRawFile( RawType.PREFAB, prefabFile.GetFullName(), data );
                    _isFirstFileRead = true;
                }
                // -- ./Raw generation

                foreach ( string line in lines ) {
                    ( bool validLine, string key, string value ) = ScsSiiHelper.ParseLine( line );

                    if ( validLine )
                        switch ( key ) {
                            case "prefab_model":
                                token = ScsHash.StringToToken( ScsSiiHelper.Trim( value.Split( '.' )[ 1 ] ) );
                                break;
                            case "prefab_desc":
                                path = ScsHelper.GetFilePath( value.Split( '"' )[ 1 ] );
                                break;
                            case "category":
                                category = value.Split( '"' )[ 1 ];
                                break;
                        }

                    if ( !line.Contains( "}" ) || token == 0 || path == "" ) continue;

                    this.Store().Def.AddPrefab( this.Parse( path, token, category ) );

                    token    = 0;
                    path     = "";
                    category = "";
                }
            }

            Log.Information( "[Job][Prefab] Loaded. Found: {0}", this.Store().Def.Prefabs.Count );
        }

        private TsPrefab Parse( string path, ulong token, string category ) {
            var fileOffset = 0x0;

            ScsFile file = this.Store().Rfs.GetFileEntry( path );

            if ( file == null ) return null;

            byte[] stream  = file.Entry.Read();
            int    version = MemoryHelper.ReadInt32( stream, fileOffset );

            // -- Raw generation
            if ( !this._isFirstFileRead ) {
                RawHelper.SaveRawFile( RawType.PREFAB, file.GetFullName(), stream );
                this._isFirstFileRead = true;
            }
            // -- ./Raw generation

            if ( version < 0x15 ) {
                var message = $"[Job][Prefab] {path} file version ({version}) too low, min. is {0x15}";
                throw new JobException( message, this.JobName(), new JObject { path, version } );
                // Log.Error( "{0} file version ({1}) too low, min. is {0x15}", path, version );
                // return null;
            }

            var  nodeCount         = BitConverter.ToInt32( stream, fileOffset += 0x04 );
            var  navCurveCount     = BitConverter.ToInt32( stream, fileOffset += 0x04 );
            bool validRoad         = navCurveCount != 0;
            var  spawnPointCount   = BitConverter.ToInt32( stream, fileOffset += 0x0C );
            var  mapPointCount     = BitConverter.ToInt32( stream, fileOffset += 0x0C );
            var  triggerPointCount = BitConverter.ToInt32( stream, fileOffset += 0x04 );

            if ( version > 0x15 ) fileOffset += 0x04; // http://modding.scssoft.com/wiki/Games/ETS2/Modding_guides/1.30#Prefabs

            int nodeOffset         = MemoryHelper.ReadInt32( stream, fileOffset += 0x08 );
            int spawnPointOffset   = MemoryHelper.ReadInt32( stream, fileOffset += 0x10 );
            int mapPointOffset     = MemoryHelper.ReadInt32( stream, fileOffset += 0x10 );
            int triggerPointOffset = MemoryHelper.ReadInt32( stream, fileOffset += 0x04 );

            return new TsPrefab( token,
                                 category,
                                 validRoad,
                                 ParsePrefabNodes( stream, nodeCount, nodeOffset ),
                                 ParseSpawnPoint( stream, spawnPointCount, spawnPointOffset ),
                                 ParseMapPoints( stream, mapPointCount, mapPointOffset ),
                                 ParseTriggerPoint( stream, triggerPointCount, triggerPointOffset ) );
        }

        private static List< TsPrefabNode > ParsePrefabNodes( byte[] stream, int nodeCount, int nodeOffset ) {
            var prefabNodes = new List< TsPrefabNode >();

            for ( var i = 0; i < nodeCount; i++ ) {
                int nodeBaseOffset = nodeOffset + i * TsPrefab.NodeBlockSize;
                var node = new TsPrefabNode {
                    X    = MemoryHelper.ReadSingle( stream, nodeBaseOffset + 0x10 ),
                    Z    = MemoryHelper.ReadSingle( stream, nodeBaseOffset + 0x18 ),
                    RotX = MemoryHelper.ReadSingle( stream, nodeBaseOffset + 0x1C ),
                    RotZ = MemoryHelper.ReadSingle( stream, nodeBaseOffset + 0x24 )
                };

                var laneCount      = 0;
                int nodeFileOffset = nodeBaseOffset + 0x24;
                for ( var j = 0; j < 8; j++ )
                    if ( MemoryHelper.ReadInt32( stream, nodeFileOffset += 0x04 ) != -1 )
                        laneCount++;

                for ( var j = 0; j < 8; j++ )
                    if ( MemoryHelper.ReadInt32( stream, nodeFileOffset += 0x04 ) != -1 )
                        laneCount++;

                node.LaneCount = laneCount;

                prefabNodes.Add( node );
            }

            return prefabNodes;
        }

        private static List< TsSpawnPoint > ParseSpawnPoint( byte[] stream, int spawnPointCount, int spawnPointOffset ) {
            var spawnPoints = new List< TsSpawnPoint >();

            for ( var i = 0; i < spawnPointCount; i++ ) {
                int spawnPointBaseOffset = spawnPointOffset + i * TsPrefab.SpawnPointBlockSize;
                var spawnPoint = new TsSpawnPoint {
                    X    = MemoryHelper.ReadSingle( stream, spawnPointBaseOffset ),
                    Z    = MemoryHelper.ReadSingle( stream, spawnPointBaseOffset + 0x08 ),
                    Type = (TsSpawnPointType) MemoryHelper.ReadUInt32( stream, spawnPointBaseOffset + 0x1C )
                };
                spawnPoints.Add( spawnPoint );
                // Log.Msg($"Spawn point of type: {spawnPoint.Type} in {_filePath}");
            }

            return spawnPoints;
        }

        private static List< TsMapPoint > ParseMapPoints( byte[] stream, int mapPointCount, int mapPointOffset ) {
            var mapPoints = new List< TsMapPoint >();

            for ( var i = 0; i < mapPointCount; i++ ) {
                int   mapPointBaseOffset    = mapPointOffset + i * TsPrefab.MapPointBlockSize;
                byte  roadLookFlags         = MemoryHelper.ReadUint8( stream, mapPointBaseOffset + 0x01 );
                var   laneTypeFlags         = (byte) ( roadLookFlags & 0x0F );
                var   laneOffsetFlags       = (byte) ( roadLookFlags >> 4 );
                sbyte controlNodeIndexFlags = MemoryHelper.ReadInt8( stream, mapPointBaseOffset + 0x04 );
                int laneOffset = laneOffsetFlags switch {
                                     1 => 1,
                                     2 => 2,
                                     3 => 5,
                                     4 => 10,
                                     5 => 15,
                                     6 => 20,
                                     7 => 25,
                                     _ => 0
                                 };

                int laneCount = laneTypeFlags switch // TODO: Change these (not really used atm)
                                {
                                    0  => 1,
                                    1  => 2,
                                    2  => 4,
                                    3  => 6,
                                    4  => 8,
                                    5  => 5,
                                    6  => 7,
                                    8  => 3,
                                    13 => -1,
                                    14 => -2,
                                    _  => 1
                                };

                sbyte controlNodeIndex = controlNodeIndexFlags switch {
                                             1  => 0,
                                             2  => 1,
                                             4  => 2,
                                             8  => 3,
                                             16 => 4,
                                             32 => 5,
                                             _  => -1
                                         };

                byte prefabColorFlags = MemoryHelper.ReadUint8( stream, mapPointBaseOffset + 0x02 );

                byte navFlags = MemoryHelper.ReadUint8( stream, mapPointBaseOffset + 0x05 );
                bool hidden   = ( navFlags & 0x02 ) != 0; // Map Point is Control Node

                var point = new TsMapPoint {
                    LaneCount        = laneCount,
                    LaneOffset       = laneOffset,
                    Hidden           = hidden,
                    PrefabColorFlags = prefabColorFlags,
                    X                = MemoryHelper.ReadSingle( stream, mapPointBaseOffset + 0x08 ),
                    Z                = MemoryHelper.ReadSingle( stream, mapPointBaseOffset + 0x10 ),
                    Neighbours       = new List< int >(),
                    NeighbourCount   = MemoryHelper.ReadInt32( stream, mapPointBaseOffset + 0x14 + 0x04 * 6 ),
                    ControlNodeIndex = controlNodeIndex
                };

                for ( var x = 0; x < point.NeighbourCount; x++ ) point.Neighbours.Add( MemoryHelper.ReadInt32( stream, mapPointBaseOffset + 0x14 + x * 0x04 ) );

                mapPoints.Add( point );
            }

            return mapPoints;
        }

        private static List< TsTriggerPoint > ParseTriggerPoint( byte[] stream, int triggerPointCount, int triggerPointOffset ) {
            var triggerPoints = new List< TsTriggerPoint >();

            for ( var i = 0; i < triggerPointCount; i++ ) {
                int triggerPointBaseOffset = triggerPointOffset + i * TsPrefab.TriggerPointBlockSize;
                var triggerPoint = new TsTriggerPoint {
                    TriggerId          = MemoryHelper.ReadUInt32( stream, triggerPointBaseOffset ),
                    TriggerActionToken = MemoryHelper.ReadUInt64( stream, triggerPointBaseOffset + 0x04 ),
                    X                  = MemoryHelper.ReadSingle( stream, triggerPointBaseOffset + 0x1C ),
                    Z                  = MemoryHelper.ReadSingle( stream, triggerPointBaseOffset + 0x24 )
                };
                triggerPoints.Add( triggerPoint );
            }

            return triggerPoints;
        }
    }
}