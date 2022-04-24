using System;
using System.IO;
using TsMap2.Factory;
using TsMap2.Scs.FileSystem;

namespace TsMap2.Helper {
    public static class RawHelper {
        public static void SaveFile( string fileName, string path, byte[] data ) {
            string fullPath = Path.Combine( path, fileName );

            Directory.CreateDirectory( path );
            File.WriteAllBytes( fullPath, data );
        }

        public static byte[] LoadFile( string fileName, string path ) {
            string fullPath = Path.Combine( path, fileName );

            return !File.Exists( fullPath )
                       ? null
                       : File.ReadAllBytes( fullPath );
        }

        public static void SaveRawFile( RawType type, string fileName, byte[] stream ) {
            UberFile file = StoreHelper.Instance.Ubs.GetFile( fileName );

            if ( file != null ) {
                byte[] fileContent = file.Entry.Read();
                var    rawFactory  = new RawFactory( fileContent );
                rawFactory.Save( type, file.Path );
            }
        }

        public static string RawTypeToString( RawType type ) => Enum.GetName( typeof( RawType ), type );
    }
}