﻿using System.Drawing;
using Serilog;
using TsMap2.Model.Ts;
using TsMap2.Scs.FileSystem.Map;

namespace TsMap2.Job.Parse.Overlays {
    public class ParseOverlaysOthersJob : ThreadJob {
        protected override void Do() {
            Log.Information( "[Job][OverlayOther] Parsing..." );

            foreach ( ScsMapMapOverlayItem overlay in Store().Map.MapOverlays ) {
                Bitmap b = overlay.Overlay?.GetBitmap();

                if ( !overlay.Valid || overlay.Hidden || b == null ) continue;

                var ov = new TsMapOverlayItem( overlay.X, overlay.Z, overlay.OverlayName, overlay.ZoomLevelVisibility, TsMapOverlayType.Overlay, b );
                Store().Map.Overlays.Overlay.Add( ov );
            }

            Log.Information( "[Job][OverlayOther] Others: {0}", Store().Map.Overlays.Overlay.Count );
        }
    }
}