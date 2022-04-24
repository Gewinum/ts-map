﻿using System.IO;
using Serilog;
using TsMap2.Helper;

namespace TsMap2.Scs.FileSystem.Map {
    public class ScsMapRoadSideItem : ScsMapItem {
        public ScsMapRoadSideItem( ScsSector sector ) : base( sector, sector.LastOffset ) {
            Valid = false;
            if ( Sector.Version < 846 )
                TsRoadSideItem825();
            else if ( Sector.Version >= 846 && Sector.Version < 855 )
                TsRoadSideItem846();
            else if ( Sector.Version >= 855 && Sector.Version < 875 )
                TsRoadSideItem855();
            else if ( Sector.Version >= 875 )
                TsRoadSideItem875();
            else
                Log.Warning( $"Unknown base file version ({Sector.Version}) for item {Type} in file '{Path.GetFileName( Sector.FilePath )}' @ {Sector.LastOffset}." );
        }

        private void TsRoadSideItem825() {
            int fileOffset = Sector.LastOffset + 0x34; // Set position at start of flags
            fileOffset += 0x15 + 3 * 0x18;             // 0x15(flags & sign_id & node_uid) + 3 * 0x18(sign_template_t)

            int tmplTextLength                    = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
            if ( tmplTextLength != 0 ) fileOffset += 0x04 + tmplTextLength; // 0x04(textPadding)

            int signAreaCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x04 ); // 0x04(cursor after tmplTextLength)
            fileOffset += 0x04;                                                              // cursor after signAreaCount
            for ( var i = 0; i < signAreaCount; i++ ) {
                int subItemCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x04 ); // 0x04(some float value)
                fileOffset += 0x04;                                                             // cursor after subItemCount
                for ( var x = 0; x < subItemCount; x++ ) {
                    short itemType = MemoryHelper.ReadInt16( Sector.Stream, fileOffset );

                    fileOffset += 0x06; // cursor after (count & itemType)

                    if ( itemType == 0x05 ) {
                        int textLength = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
                        fileOffset += 0x04 + 0x04 + textLength; // 0x04(cursor after textlength) + 0x04(padding)
                    }
                    //else if (itemType == 0x06)
                    //{
                    //    fileOffset += 0x04; // 0x04(padding)
                    //}
                    else if ( itemType == 0x01 )
                        fileOffset += 0x01; // 0x01(padding)
                    else
                        fileOffset += 0x04; // 0x04(padding)
                }
            }

            BlockSize = fileOffset - Sector.LastOffset;
        }

        private void TsRoadSideItem846() {
            int fileOffset = Sector.LastOffset + 0x34; // Set position at start of flags
            fileOffset += 0x15 + 3 * 0x18;             // 0x15(flags & sign_id & node_uid) + 3 * 0x18(sign_template_t)

            int tmplTextLength                    = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
            if ( tmplTextLength != 0 ) fileOffset += 0x04 + tmplTextLength; // 0x04(textPadding)

            int signAreaCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x04 ); // 0x04(cursor after tmplTextLength)
            fileOffset += 0x04;                                                              // cursor after signAreaCount
            for ( var i = 0; i < signAreaCount; i++ ) {
                int subItemCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x0C ); // 0x0C(padding)
                fileOffset += 0x04;                                                             // cursor after subItemCount
                for ( var x = 0; x < subItemCount; x++ ) {
                    short itemType = MemoryHelper.ReadInt16( Sector.Stream, fileOffset );

                    fileOffset += 0x06; // cursor after (count & itemType)

                    if ( itemType == 0x05 ) {
                        int textLength = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
                        fileOffset += 0x04 + 0x04 + textLength; // 0x04(cursor after textlength) + 0x04(padding)
                    }
                    //else if (itemType == 0x06)
                    //{
                    //    fileOffset += 0x04; // 0x04(padding)
                    //}
                    else if ( itemType == 0x01 )
                        fileOffset += 0x01; // 0x01(padding)
                    else
                        fileOffset += 0x04; // 0x04(padding)
                }
            }

            BlockSize = fileOffset - Sector.LastOffset;
        }

        private void TsRoadSideItem855() {
            int fileOffset = Sector.LastOffset + 0x34; // Set position at start of flags
            fileOffset += 0x15 + 3 * 0x18;             // 0x15(flags & sign_id & node_uid) + 3 * 0x18(sign_template_t)

            int tmplTextLength                    = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
            if ( tmplTextLength != 0 ) fileOffset += 0x04 + tmplTextLength; // 0x04(textPadding)

            int signAreaCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x04 ); // 0x04(cursor after tmplTextLength)
            fileOffset += 0x04;                                                              // cursor after signAreaCount
            for ( var i = 0; i < signAreaCount; i++ ) {
                int subItemCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x0C ); // 0x0C(padding)
                fileOffset += 0x04;                                                             // cursor after subItemCount
                for ( var x = 0; x < subItemCount; x++ ) {
                    short itemType = MemoryHelper.ReadInt16( Sector.Stream, fileOffset );

                    int itemCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x02 ); // 0x02(itemType)

                    fileOffset += 0x04; // cursor after count

                    if ( itemType == 0x05 ) {
                        int textLength = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
                        fileOffset += 0x04 + 0x04 + textLength; // 0x04(cursor after textlength) + 0x04(padding)
                    } else if ( itemType == 0x06 )
                        fileOffset += 0x08 * itemCount; // 0x08(m_stand_id)
                    else if ( itemType == 0x01 )
                        fileOffset += 0x01; // 0x01(padding)
                    else
                        fileOffset += 0x04; // 0x04(padding)
                }
            }

            BlockSize = fileOffset - Sector.LastOffset;
        }

        private void TsRoadSideItem875() {
            int   fileOffset    = Sector.LastOffset + 0x34;                                   // Set position at start of flags
            sbyte templateCount = MemoryHelper.ReadInt8( Sector.Stream, fileOffset += 0x25 ); // 0x20(flags & sign_id & uid & look & variant)
            fileOffset += 0x01 + templateCount * 0x18;                                        // 0x01(templateCount) + templateCount * 0x18(sign_template_t)

            int tmplTextLength                    = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
            if ( tmplTextLength != 0 ) fileOffset += 0x04 + tmplTextLength; // 0x04(textPadding)

            int signAreaCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x04 ); // 0x04(cursor after tmplTextLength)
            fileOffset += 0x04;                                                              // cursor after signAreaCount
            for ( var i = 0; i < signAreaCount; i++ ) {
                int subItemCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x0C ); // 0x0C(padding)
                fileOffset += 0x04;                                                             // cursor after subItemCount
                for ( var x = 0; x < subItemCount; x++ ) {
                    short itemType = MemoryHelper.ReadInt16( Sector.Stream, fileOffset );

                    int itemCount = MemoryHelper.ReadInt32( Sector.Stream, fileOffset += 0x02 ); // 0x02(itemType)

                    fileOffset += 0x04; // cursor after count

                    if ( itemType == 0x05 ) {
                        int textLength = MemoryHelper.ReadInt32( Sector.Stream, fileOffset );
                        fileOffset += 0x04 + 0x04 + textLength; // 0x04(cursor after textlength) + 0x04(padding)
                    } else if ( itemType == 0x06 )
                        fileOffset += 0x08 * itemCount; // 0x08(m_stand_id)
                    else if ( itemType == 0x01 )
                        fileOffset += 0x01; // 0x01(padding)
                    else
                        fileOffset += 0x04; // 0x04(padding)
                }
            }

            BlockSize = fileOffset - Sector.LastOffset;
        }
    }
}