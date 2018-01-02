using DataTransformer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransformer.Data
{
    public interface IConntext
    {
        EbayArtikel GetEbayArtikelBySKU(Artikel a);
        Artikel GetArtikelBySKU(Artikel a);
        Artikel GetKategrieByCATID(Artikel a, int nummer);
    }
}



