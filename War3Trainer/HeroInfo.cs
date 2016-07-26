using System;
using System.Collections.Generic;
using System.Text;

namespace War3Trainer
{
    public class HeroInfo
    {
        public HeroInfo()
        {
            Address = 0;
            Name = "N/A";
            X = 0;
            Y = 0;
            HP = 0;
            MP = 0;
        }
        public UInt32 Address { get; set; }  
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int HP { get; set; }
        public int MP { get; set; }
    }
}
