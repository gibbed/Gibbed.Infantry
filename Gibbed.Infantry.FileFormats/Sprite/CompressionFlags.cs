using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Infantry.FileFormats.Sprite
{
    [Flags]
    public enum CompressionFlags : uint
    {
        None = 0,
        DupeRowsVertically = 1 << 0,
        DupeRowsHorizontally = 1 << 1,
        ColumnsAreHalfRotation = 1 << 2,
        ColumnsAreQuarterRotation = 1 << 3,
        NoPixels = 1 << 4,
        RowsAreHalfRotation = 1 << 5,
        RowsAreQuarterRotation = 1 << 6,
        NoCompression = 1 << 7,
    }
}
