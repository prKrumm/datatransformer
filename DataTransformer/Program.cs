using DataTransformer.Model;
using DataTransformer.Controller;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataTransformer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start reading EbayNeu.txt!");
            string path = @"D:\Benutzer\Patrick\Desktop\JTL\EbayNeu.txt";

            string[] readText = File.ReadAllLines(path);
            ControllerImpl controller = new ControllerImpl();
            List<Artikel> artikelList = new List<Artikel>();

            

           

            foreach (string s in readText)
            {
                if (s.Equals(""))
                {

                }
                else
                {
                    Artikel a = new Artikel();
                    a.SKU = s;
                    artikelList.Add(a);
                    Console.WriteLine("Ausgelesene SKU: " + s);
                }
                
                
            }
            controller.TransformArtikel(artikelList);
            Console.WriteLine("Alle Artikel transformiert!");

        }
    }
}
