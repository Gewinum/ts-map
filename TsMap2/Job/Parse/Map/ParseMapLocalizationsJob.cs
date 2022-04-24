﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;
using TsMap2.Helper;
using TsMap2.Model.Ts;
using TsMap2.Scs.FileSystem;

namespace TsMap2.Job.Parse.Map {
    public class ParseMapLocalizationsJob : ThreadJob {
        private bool _isFirstFileRead;

        protected override void Do() {
            Log.Debug( "[Job][Localizations] Loading" );


            UberDirectory localeDir = Store().Ubs.GetDirectory( "locale" );
            if ( localeDir == null ) {
                Log.Warning( "[Job][Localizations] Could not find locale directory." );
                return;
            }


            var localizationList = new List< string >();

            // foreach ( string localeDirDirectoryName in localeDir.GetSubDirectoryNames() ) {
            //     localizationList.Add( localeDirDirectoryName );
            //
            //     foreach ( string localeFile in Store().Ubs.GetDirectory( localeDirDirectoryName ).GetFiles(  ) )
            //         ParseLocale( localeFile.Value, localeDirDirectory.Value.GetCurrentDirectoryName() );
            // }

            Log.Information( "[Job][Localizations] Loaded." );
        }

        private void ParseLocale( UberFile localeFile, string locale ) {
            if ( localeFile == null ) return;

            byte[] entryContents = localeFile.Entry.Read();
            uint   magic         = MemoryHelper.ReadUInt32( entryContents, 0 );
            string fileContents = magic == 21720627
                                      ? MemoryHelper.Decrypt3Nk( entryContents )
                                      : Encoding.UTF8.GetString( entryContents );

            // if ( fileContents == null ) {
            //     Log.Warning( "Could not read locale file '{0}'", localeFile.GetPath() );
            //     return;
            // }

            var key = string.Empty;

            // -- Raw generation
            // if ( !_isFirstFileRead ) {
            //     RawHelper.SaveRawFile( RawType.MAP_LOCALIZATION, localeFile.GetFullName(), entryContents );
            //     _isFirstFileRead = true;
            // }
            // -- ./Raw generation

            foreach ( string l in fileContents.Split( '\n' ) ) {
                if ( !l.Contains( ':' ) ) continue;

                if ( l.Contains( "key[]" ) )
                    key = l.Split( '"' )[ 1 ];

                else if ( l.Contains( "val[]" ) ) {
                    string val = l.Split( '"' )[ 1 ];

                    if ( key != string.Empty && val != string.Empty ) {
                        IEnumerable< TsCity > cities = Store().Def.Cities.Values.Where( x => x.LocalizationToken == key );

                        foreach ( TsCity city in cities )
                            Store().Def.Cities[ city.Token ].AddLocalizedName( locale, val );

                        TsCountry country = Store().Def.Countries.Values.FirstOrDefault( x => x.LocalizationToken == key );
                        // if ( country != null )
                        //     Store().Def.Countries[ country.Token ].AddLocalizedName( locale, val );
                    }
                }
            }
        }
    }
}