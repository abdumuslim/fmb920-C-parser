﻿using System.Windows.Media;

namespace Teltonika.DataParser.Client.Models
{
    public class BitValue
    {
        public string FirstPartBits { get; set; }
        public string SecondPartBits { get; set; }
        public Brush FirstPartBitsTextColor { get; set; }
        public Brush SecondPartBitsTextColor { get; set; }
    }
}
