using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransformer.Model
{
    public class Kategorie
    {
        public Int64 StoreCategoryIDLevel1 { get; set; }
        public Int64 StoreCategoryIDLevel2 { get; set; }
        public Int64 StoreCategoryIDLevel3 { get; set; }
        public Int64 StoreCategoryIDLevel4 { get; set; }
        public string Level1 { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }
        public string Level4 { get; set; }
    }
}
