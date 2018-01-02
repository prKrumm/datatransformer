using DataTransformer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransformer.Controller
{
    public interface IController
    {
        Boolean TransformArtikel(List<Artikel> artikelList);
    }
}
