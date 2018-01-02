using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransformer.Model
{
    public class EbayArtikel
    {
        public string SKU { get; set; }
        public string EbayItemId { get; set; }
        public Int32 PrimaryCatID { get; set; }
        public string ArtikelTyp { get; set; }
        public Int64 StoreCategoryID { get; set; }
        public Int64 StoreCategoryID2 { get; set; }
        public string ListingDuration { get; set; }
        public Int32 ConditionID { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public decimal VersandInland { get; set; }
        public decimal VersandAusland { get; set; }
        public string Hersteller { get; set; }

        public override String ToString()
        {
            return "SKU: " + SKU + " EbayItemId: " + EbayItemId + " Titel: " + Title;
        }


        }
}
