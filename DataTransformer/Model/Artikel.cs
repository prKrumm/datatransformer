using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransformer.Model
{
    public class Artikel
    {

        public Artikel()
        {
            List<Kategorie> katList = new List<Kategorie>();
            katListe = katList;
        }

        public Int32 kArtikel { get; set; }
        public string SKU { get; set; }
        public string cAktiv { get; set; }
        public decimal EbayPreis { get; set; }
        public string Name { get; set; }
        public string BeschreibungDeutsch { get; set; }
        public EbayArtikel ebayArtikel { get; set; }
        public List<Kategorie> katListe { get; set; }
        public string weitereArtNr { get; set; }
        public string ArtNr { get; set; }





    }
}
