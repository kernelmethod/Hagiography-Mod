using System;

namespace Kernelmethod.Hagiography.Schemas {
    public class Tile {
        public string Path;
        public string RenderString;
        public string ColorString;
        public string DetailColor;
        public string TileColor = null;
        public bool HFlip = false;
        public bool VFlip = false;

        public override string ToString() {
            string[] components = {
                Path,
                RenderString,
                ColorString,
                DetailColor,
                TileColor ?? "",
                HFlip ? "1" : "0",
                VFlip ? "1" : "0"
            };
            return String.Join(';', components);
        }
    }
}
