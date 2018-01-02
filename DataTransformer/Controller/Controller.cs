using System;
using System.Collections.Generic;
using System.Text;
using DataTransformer.Model;
using DataTransformer.Data;
using System.IO;
using System.Text.RegularExpressions;


namespace DataTransformer.Controller
{
    public class ControllerImpl : IController
    {
        List<Mapping> mappingList;
        StringBuilder alters;

        public ControllerImpl()
        {
            string pathMapping = @"D:\Benutzer\Patrick\Desktop\JTL\katMapping.csv";
            string mappingText = File.ReadAllText(pathMapping, Encoding.UTF7);
            List<Mapping> mappingList = new List<Mapping>();
            this.alters = new StringBuilder();
            foreach (string row in mappingText.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    Mapping mapping = new Mapping();
                    int i = 0;

                    //Execute a loop over the columns.  
                    foreach (string cell in row.Split(';'))
                    {
                        if (i == 0)
                        {
                            mapping.EbayKat = cell;
                        }
                        if (i == 1)
                        {
                            mapping.JTLKat = cell;
                        }
                        if (i == 2)
                        {
                            mapping.JTLKatE = cell.Replace('\r', ' ').Trim();
                        }
                        mappingList.Add(mapping);
                        i++;
                    }
                }
            }

            this.mappingList = mappingList;

        }


        public bool TransformArtikel(List<Artikel> artikelList)
        {
            List<Artikel> artikelWithEbayArtikel = new List<Artikel>();
            //Db connection
            IConntext conntext = new JTLConntext(Config.connString);
           for (int i = 0; i < artikelList.Count; i++)
           {
                //Artikel anhand SKU holen
               artikelList[i]=conntext.GetArtikelBySKU(artikelList[i]);
                
               EbayArtikel e = conntext.GetEbayArtikelBySKU(artikelList[i]);
                artikelList[i].ebayArtikel = e;
                if (e.EbayItemId == null)
                {
                    Console.WriteLine("Kein Ebay Artikel für SKU: "+artikelList[i].SKU);
                    alters.AppendLine("Kein Ebay Artikel für SKU: " + artikelList[i].SKU);
                }
                else
                {
                    artikelWithEbayArtikel.Add(artikelList[i]);
                }
                         
            }
            Console.WriteLine("Artikel mit Ebay Artikel: " + artikelWithEbayArtikel.Count);
            alters.AppendLine("--------------------------------------------------------------------------");
            alters.AppendLine("--------------------------------------------------------------------------");
            alters.AppendLine("Artikel Insgesamt: "+ artikelList.Count);
            alters.AppendLine("Artikel mit Ebay Artikel Insgesamt: " + artikelWithEbayArtikel.Count);


            //transform artikel liste
            #region Kategorien


            assignKategories(artikelWithEbayArtikel, conntext);
            mapToJTL(artikelWithEbayArtikel);
            cleanBeschreibung(artikelWithEbayArtikel);
            newPrice(artikelWithEbayArtikel);

            exportKategoriesBeschreibungPreisToCSV(artikelWithEbayArtikel);
            exportEbayItemNumbers(artikelWithEbayArtikel);
            exportMerkmale(artikelWithEbayArtikel);
            exportVersandKosten(artikelWithEbayArtikel);
            exportHersteller(artikelWithEbayArtikel);
            exportLogger();

          





            #endregion
            return false;
        }

        private void exportHersteller(List<Artikel> artikelList)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Artikelnummer;Hersteller");
            for (int i = 0; i < artikelList.Count; i++)
            {
                //Hersteller
                sb.AppendLine(artikelList[i].SKU + ";" + artikelList[i].ebayArtikel.Hersteller);               
            }

            string csvpath = @"D:\Benutzer\Patrick\Desktop\JTL\Hersteller.csv";

            using (StreamWriter swriter = new StreamWriter(csvpath, false, Encoding.UTF8))
            {
                swriter.Write(sb.ToString());
            }

        }

        private void exportLogger()
        {
            string csvpath = @"D:\Benutzer\Patrick\Desktop\JTL\Log.csv";

            using (StreamWriter swriter = new StreamWriter(csvpath, false, Encoding.UTF8))
            {
                swriter.Write(alters.ToString());
            }
        }

        private void exportVersandKosten(List<Artikel> artikelList)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Artikelnummer;Attributwert;Attributname");
            for (int i = 0; i < artikelList.Count; i++)
            {
                //attr4 -> VersandInland
                sb.AppendLine(artikelList[i].SKU + ";" + artikelList[i].ebayArtikel.VersandInland + ";attr4");
                //attr5 -> VersandAusland
                sb.AppendLine(artikelList[i].SKU + ";" + artikelList[i].ebayArtikel.VersandAusland + ";attr5");

            }

            string csvpath = @"D:\Benutzer\Patrick\Desktop\JTL\Attribute.csv";

            using (StreamWriter swriter = new StreamWriter(csvpath, false, Encoding.UTF8))
            {
                swriter.Write(sb.ToString());
            }
        }

        private void exportKategoriesBeschreibungPreisToCSV(List<Artikel> artikelList)
        {
            StringBuilder sb = new StringBuilder();
            //Artikelnummer;Kategorie Level 1;Kategorie Level 2;Kategorie Level 3;Kategorie Level 4
            sb.AppendLine("Artikelnummer;Kategorie Level 1;Kategorie Level 2;Kategorie Level 3;Kategorie Level 4");
            for (int i = 0; i < artikelList.Count; i++)
            {
                for(int j = 0; j < artikelList[i].katListe.Count; j++)
                {
                    //Deutsch
                    if (j == 0)
                    {
                        sb.AppendLine(artikelList[i].SKU + ";" + "ShopAktiv");
                    }
                   
                    sb.AppendLine(artikelList[i].SKU + ";" + "ASHOPDeutsch"+";" + artikelList[i].katListe[j].Level2 + ";" + artikelList[i].katListe[j].Level3 + ";" + artikelList[i].katListe[j].Level4);

                    //Englisch
                    for(int k = 0; k < mappingList.Count; k++)
                    {
                        if (artikelList[i].katListe[j].Level4.Equals(mappingList[k].JTLKat))
                        {
                            String level3 = artikelList[i].katListe[j].Level3;
                            String level2= artikelList[i].katListe[j].Level2;
                            if (artikelList[i].katListe[j].Level3.Equals("Sonstige"))
                            {
                                level3 = "Other";
                                
                            }
                            if(artikelList[i].katListe[j].Level2.Equals("Alle Automarken"))
                            {
                                level2 = "All Car Brands";
                            }
                            sb.AppendLine(artikelList[i].SKU + ";" + "ASHOPEnglisch" + ";" + level2 + ";" + level3 + ";" + mappingList[k].JTLKatE);
                            break;
                        }
                    }

                }
                
             


            }
            StringBuilder beschreibungen = new StringBuilder();
            beschreibungen.AppendLine("\"Artikelnummer\";\"Beschreibung\"");

            StringBuilder preise = new StringBuilder();
            preise.AppendLine("\"Artikelnummer\";\"Preis\"");

            //Beschreibung
            //"Artikelnummer;Kategorie Level 1;Kategorie Level 2;Kategorie Level 3;Kategorie Level 4;Beschreibung;vk brutto"
            for (int i = 0; i < artikelList.Count; i++)
            {
                // beschreibungen.AppendLine("\""+artikelList[i].SKU + "\";"+ "\""+artikelList[i].BeschreibungDeutsch+ "\"");
                beschreibungen.AppendLine("\""+artikelList[i].SKU+ "\";\"" + artikelList[i].BeschreibungDeutsch+"\"" );
                //sb.AppendLine(artikelList[i].SKU + ";" + ";" + ";" + ";" + ";");
                //sb.Append(artikelList[i].BeschreibungDeutsch);
                preise.AppendLine(artikelList[i].SKU + ";"+ artikelList[i].EbayPreis);
            }



            string csvpath = @"D:\Benutzer\Patrick\Desktop\JTL\Kategorien.csv";
            string csvpath2 = @"D:\Benutzer\Patrick\Desktop\JTL\Beschreibungen.csv";
            string csvpath3 = @"D:\Benutzer\Patrick\Desktop\JTL\Preise.csv";

            using (StreamWriter swriter = new StreamWriter(csvpath,false,Encoding.UTF8))
            {
                swriter.Write(sb.ToString());
            }
            using (StreamWriter swriter = new StreamWriter(csvpath2, false, Encoding.UTF8))
            {
                swriter.Write(beschreibungen.ToString());
            }
            using (StreamWriter swriter = new StreamWriter(csvpath3, false, Encoding.UTF8))
            {
                swriter.Write(preise.ToString());
            }


        }

        private void exportEbayItemNumbers(List<Artikel> artikelList)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < artikelList.Count; i++)
            {
                sb.Append(artikelList[i].ebayArtikel.EbayItemId + ",");
            }
            string csvpath = @"D:\Benutzer\Patrick\Desktop\JTL\DreamrobotItems.csv";

            using (StreamWriter swriter = new StreamWriter(csvpath, false, Encoding.UTF8))
            {
                swriter.Write(sb.ToString());
            }
        }

        private void exportMerkmale(List<Artikel> artikelList)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bestandseinheit; Merkmalname;Merkmalwertname");
            for (int i = 0; i < artikelList.Count; i++)
            {
                //Bestandseinheit;Merkmalname;id;Merkmalwertname
                switch (artikelList[i].ebayArtikel.ConditionID)
                {
                    case 1500:
                        sb.AppendLine(artikelList[i].SKU+";"+ "Zustand;" + "Neu: Sonstige (siehe Artikelbeschreibung)");
                        break;
                    case 1000:
                        sb.AppendLine(artikelList[i].SKU + ";" + "Zustand;" + "Neu");
                        break;
                    case 2500:
                        sb.AppendLine(artikelList[i].SKU + ";" + "Zustand;" + "Generalüberholt");
                        break;
                    case 3000:
                        sb.AppendLine(artikelList[i].SKU + ";" + "Zustand;" + "Gebraucht");
                        sb.AppendLine(artikelList[i].SKU + ";" + "Gebrauchsspuren;" + "altersbedingte Gebrauchsspuren");
                        break;
                    case 7000:
                        sb.AppendLine(artikelList[i].SKU + ";" + "Zustand;" + "Als Ersatzteil / defekt");
                        break;
                }
                



            }
            //ArtikelTyp
            //S10198; Artikeltyp;Türgriffe, innen
            for (int i = 0; i < artikelList.Count; i++)
            {
                sb.AppendLine(artikelList[i].SKU + ";" + "Artikeltyp;" + artikelList[i].ebayArtikel.ArtikelTyp);
            }
            string csvpath = @"D:\Benutzer\Patrick\Desktop\JTL\Merkmale.csv";

            using (StreamWriter swriter = new StreamWriter(csvpath, false, Encoding.UTF8))
            {
                swriter.Write(sb.ToString());
            }

        }

        private void assignKategories(List<Artikel> artikelList, IConntext conntext)
        {
            
            for (int i = 0; i < artikelList.Count; i++)
            {
                Console.WriteLine("Kategorien zuweisen zu: "+artikelList[i].SKU+" "+ artikelList[i].Name);
                #region Deutsch Kat1
                //Deutsch Kat1
                //Categorie 1 dann Sonstige_Sonstige
                if (artikelList[i].ebayArtikel.StoreCategoryID.Equals(1))
                {
                    artikelList[i].ebayArtikel.StoreCategoryID = 24011046014;
                    Console.WriteLine("StoreCategoryID " + artikelList[i].ebayArtikel.StoreCategoryID +" zu 24011046014 (Sonstige_Sonstige) geändert");

                }
                conntext.GetKategrieByCATID(artikelList[i], 1);

                #endregion
                #region Deutsch Kat2
                if (artikelList[i].ebayArtikel.StoreCategoryID2.Equals(0))
                {
                    //bleibt bei 0

                }
                //Categorie 1 dann Sonstige_Sonstige
                if (artikelList[i].ebayArtikel.StoreCategoryID2.Equals(1))
                {                   
                    artikelList[i].ebayArtikel.StoreCategoryID2 = 24011046014;
                    Console.WriteLine("StoreCategoryID2 " + artikelList[i].ebayArtikel.StoreCategoryID2 + " zu 24011046014 (Sonstige_Sonstige) geändert");
                }

                conntext.GetKategrieByCATID(artikelList[i], 2);
                #endregion



            }

        }


      

        private void mapToJTL(List<Artikel> artikelList)
        {
            //Über alle Artikel iterieren
            for(int i = 0; i < artikelList.Count; i++)
            {
                //über alle Kategorien je Artikel iterieren
                for (int j = 0; j < artikelList[i].katListe.Count; j++)
                {
                    //mapping
                    //Über alle mapping felder iterieren
                    for(int k = 0; k < mappingList.Count; k++)
                    {
                        if (artikelList[i].katListe[j].Level4.Equals(mappingList[k].EbayKat))
                        {
                            artikelList[i].katListe[j].Level4 = mappingList[k].JTLKat;
                        }
                        if (artikelList[i].katListe[j].Level3.Equals(mappingList[k].EbayKat))
                        {
                            artikelList[i].katListe[j].Level3 = mappingList[k].JTLKat;
                        }
                        if (artikelList[i].katListe[j].Level2.Equals(mappingList[k].EbayKat))
                        {
                            artikelList[i].katListe[j].Level2 = mappingList[k].JTLKat;
                        }

                    }
                }
            }

        }

        private void cleanBeschreibung(List<Artikel> artikelList)
        {
            for (int i = 0; i < artikelList.Count; i++)
            {
                artikelList[i].BeschreibungDeutsch=Regex.Replace(artikelList[i].BeschreibungDeutsch, "<font.*?>", "");
                artikelList[i].BeschreibungDeutsch= Regex.Replace(artikelList[i].BeschreibungDeutsch, "</font>", "");

                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, @"align=""center""", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, @"style="".*?""", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, @"id="".*?""", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, @"class="".*?""", "");



                //artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<o:p>", "");
                //artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "</o:p>", "");

                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<b>", "");
               // artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<b .*?>", "");
                //artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<b  .*?>", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "</b>", "");

                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, ":", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<p  >", "<p>");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<p >", "<p>");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "<p   >", "<p>");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "\"", "");


                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "Sehr geehrter Kunde!", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "Sollte einer unserer Artikel nicht Ihren Erwartungen entsprechen, bitten wir Sie uns nicht gleich negativ zu bewerten.", "");

                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "Sollte", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "einer unserer Artikel nicht Ihren Erwartungen entsprechen, bitten wir ", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "Sie uns nicht gleich negativ zu bewerten. Wir werden mit", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "Sicherheit eine zufriedenstellende Lösung für Sie finden.", "");
                artikelList[i].BeschreibungDeutsch = Regex.Replace(artikelList[i].BeschreibungDeutsch, "Sie uns nicht gleich negativ zu bewerten.", "");
                //artikelList[i].BeschreibungDeutsch=artikelList[i].BeschreibungDeutsch.Replace("<div*>", "");

                if(artikelList[i].BeschreibungDeutsch.Contains("Kunde")| artikelList[i].BeschreibungDeutsch.Contains("Erwartungen") | artikelList[i].BeschreibungDeutsch.Contains("bewerten") | artikelList[i].BeschreibungDeutsch.Contains("zufriedenstellende") | artikelList[i].BeschreibungDeutsch.Contains("Sollte"))
                {
                    alters.AppendLine("Beschreibung noch nicht sauber: " + artikelList[i].BeschreibungDeutsch);
                }
            }
        }

        private void newPrice(List<Artikel> artikelList)
        {
            
            for (int i = 0; i < artikelList.Count; i++)
            {
                decimal alterpreis = artikelList[i].EbayPreis;
                Console.WriteLine("ArtikelNr: " + artikelList[i].SKU + " Alter Preis: " +artikelList[i].EbayPreis);
                // (10 bis 20}
                if (artikelList[i].EbayPreis <= 20&& artikelList[i].EbayPreis > 10)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 1;
                }
                // (20 bis 30}
                if (artikelList[i].EbayPreis <= 30 && artikelList[i].EbayPreis > 20)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 2;
                }
                // (30 bis 40}
                if (artikelList[i].EbayPreis <= 40 && artikelList[i].EbayPreis > 30)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 3;
                }
                // (40 bis 50}
                if (artikelList[i].EbayPreis <= 50 && artikelList[i].EbayPreis > 40)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 4;
                }
                // (50 bis 60}
                if (artikelList[i].EbayPreis <= 60 && artikelList[i].EbayPreis > 50)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 5;
                }
                // (60 bis 70}
                if (artikelList[i].EbayPreis <= 70 && artikelList[i].EbayPreis > 60)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 6;
                }
                // (70 bis 80}
                if (artikelList[i].EbayPreis <= 80 && artikelList[i].EbayPreis > 70)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 7;
                }
                // (80 bis 100}
                if (artikelList[i].EbayPreis <= 100 && artikelList[i].EbayPreis > 80)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 8;
                }
                // (100 bis 120}
                if (artikelList[i].EbayPreis <= 120 && artikelList[i].EbayPreis > 100)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 8;
                }
                // (120 bis 150}
                if (artikelList[i].EbayPreis <= 150 && artikelList[i].EbayPreis > 120)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 10;
                }
                // (150 bis 200}
                if (artikelList[i].EbayPreis <= 200 && artikelList[i].EbayPreis > 150)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 10;
                }
                // (200 bis 250}
                if (artikelList[i].EbayPreis <= 250 && artikelList[i].EbayPreis > 200)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 15;
                }
                // (250 bis 350}
                if (artikelList[i].EbayPreis <= 350 && artikelList[i].EbayPreis > 250)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 15;
                }
                // (350 bis 450}
                if (artikelList[i].EbayPreis <= 450 && artikelList[i].EbayPreis > 350)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 15;
                }
                // (450 bis 600}
                if (artikelList[i].EbayPreis <= 600 && artikelList[i].EbayPreis > 450)
                {
                    artikelList[i].EbayPreis = artikelList[i].EbayPreis - 20;
                }
                decimal differenz = artikelList[i].EbayPreis / alterpreis;
                Console.WriteLine("ArtikelNr: "+artikelList[i].SKU+" Neuer Preis: " + artikelList[i].EbayPreis+" Differenz "+differenz);
                decimal a= 0.89m;
                if (differenz < a){
                    alters.AppendLine("ACHTUNG PREIS: SKU: " + " ArtikelNr: " + artikelList[i].SKU + " Neuer Preis: " + artikelList[i].EbayPreis + " Differenz " + differenz);
                }
            }

        }

    }

   
}
