using DataTransformer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using DataTransformer.Data;

namespace DataTransformer.Data
{
    public class JTLConntext:IConntext
    {

        public string ConnectionString { get; set; }

        public JTLConntext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public JTLConntext()
        {
        }

        private SqlConnection GetConnection()
        {

            try
            {
                SqlConnection connection = new SqlConnection(this.ConnectionString);               
                return connection;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            
           
        }

        public Artikel GetArtikelBySKU(Artikel a)
        {
            using (SqlConnection conn = GetConnection())
            {
                if (a != null)
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("select a.kArtikel,cArtNr,cAktiv,fEbayPreis, cName,cBeschreibung from tArtikel a,tArtikelBeschreibung b where cArtNr=@sku and a.kArtikel=b.kArtikel and kSprache=1 and kPlattform=1", conn);
                    cmd.Parameters.AddWithValue("@sku", a.SKU);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            a.kArtikel = reader.GetInt32(0);
                            a.SKU= reader.GetString(1);
                            a.cAktiv = reader.GetString(2);
                            a.EbayPreis = reader.GetDecimal(3);
                            a.Name=reader.GetString(4);
                            a.BeschreibungDeutsch = reader.GetString(5);


                            Console.WriteLine("SKU from DB: " + a.SKU);
                            Console.WriteLine("Name from DB: " + a.Name);
                        }
                    }

                }

            }

            return a;
        }

        //Kategorie 1 oder 2
        public Artikel GetKategrieByCATID(Artikel a,int nummer)
        {
            
            Kategorie kategorie= new Kategorie();
            using (SqlConnection conn = GetConnection())
            {
                if (a != null)
                {
                    conn.Open();                                          
                    SqlCommand cmd = new SqlCommand("select CategoryID,ParentCategory,Name from ebay_shop_category where CategoryID=@catid", conn);                  
                    if (nummer == 1)
                    {
                        //Kategorie 1
                        cmd.Parameters.AddWithValue("@catid", a.ebayArtikel.StoreCategoryID);
                    }else
                    {
                        //Kategorie 2
                        cmd.Parameters.AddWithValue("@catid", a.ebayArtikel.StoreCategoryID2);
                    }
                        


                    //Level4
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {                           
                            kategorie.StoreCategoryIDLevel4=reader.GetInt64(0);
                            kategorie.Level4 = reader.GetString(2);
                            kategorie.StoreCategoryIDLevel3 = reader.GetInt64(1);
                            

                            Console.WriteLine("Kategorie Id4: " + kategorie.StoreCategoryIDLevel4);
                            Console.WriteLine("Name: " + kategorie.Level4);
                        }
                    }

                    //Level 3
                    SqlCommand cmd2 = new SqlCommand("select CategoryID,ParentCategory,Name from ebay_shop_category where CategoryID=@catid2", conn);
                    cmd2.Parameters.AddWithValue("@catid2", kategorie.StoreCategoryIDLevel3);
                    using (SqlDataReader reader = cmd2.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            kategorie.StoreCategoryIDLevel3 = reader.GetInt64(0);
                            kategorie.Level3 = reader.GetString(2);
                            kategorie.StoreCategoryIDLevel2 = reader.GetInt64(1);


                            Console.WriteLine("Kategorie Id3: " + kategorie.StoreCategoryIDLevel4);
                            Console.WriteLine("Name: " + kategorie.Level3);
                        }
                    }

                    //Level 2
                    SqlCommand cmd3 = new SqlCommand("select CategoryID,ParentCategory,Name from ebay_shop_category where CategoryID=@catid3", conn);
                    cmd3.Parameters.AddWithValue("@catid3", kategorie.StoreCategoryIDLevel2);
                    using (SqlDataReader reader = cmd3.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            kategorie.StoreCategoryIDLevel2 = reader.GetInt64(0);
                            kategorie.Level2 = reader.GetString(2);
                            kategorie.StoreCategoryIDLevel1 = reader.GetInt64(1);


                            Console.WriteLine("Kategorie Id2: " + kategorie.StoreCategoryIDLevel4);
                            Console.WriteLine("Name: " + kategorie.Level2);
                        }
                    }
                    
                }

            }
            if (kategorie.Level4 == null)
            {
                //not use this category
                Console.WriteLine("SKU " + a.SKU + " hat keine Kategorie bei Nummer: " + nummer);
            }else
            {
               a.katListe.Add(kategorie);
            }
           

            return a;
        }

        public EbayArtikel GetEbayArtikelBySKU(Artikel a)
        {
            EbayArtikel ebayArtikel = new EbayArtikel();
            using (SqlConnection conn = GetConnection())
            {
                if (a != null)
                {
                    conn.Open();
                    
                    SqlCommand cmd=new SqlCommand("  Select SKU, ItemID,PrimaryCategoryId, StoreCategoryId, StoreCategory2Id,ListingDuration, ConditionID,Title,StartPrice,c.CategoryName from ebay_item e,ebay_shop_category s,ebay_xx_categories c where e.SiteID=77 AND e.StoreCategoryId=s.CategoryID AND c.CategoryId=e.PrimaryCategoryId AND e.SKU=@sku AND ItemID !='' AND c.SiteID=77 AND ListingDuration='GTC' order by SKU", conn);
                    cmd.Parameters.AddWithValue("@sku", a.SKU);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.RecordsAffected > 1)
                            {
                                Console.WriteLine("Mehrere Ebay Artikel pro Artikel. Anzahl: "+reader.RecordsAffected);
                                //artikel hat mehrere ebay Artikel
                                //Falls Name und Preis gleich, sollte es passen
                                if (reader.GetString(7).Equals(a.Name)&& reader.GetDecimal(8).Equals(a.EbayPreis))
                                {
                                    ebayArtikel = readerToArticle(reader);
                                }
                                else
                                {
                                    Console.WriteLine("Namen oder Preis sind unterschiedlich! Reader: " + reader.GetString(7) + " Artikel: " + a.Name + " Decimal Ebay: " + reader.GetDecimal(8) + " Artikel: " + a.EbayPreis);
                                }


                            }
                            else
                            {
                                if (reader.GetString(7).Equals(a.Name) && reader.GetDecimal(8).Equals(a.EbayPreis))
                                {
                                    ebayArtikel = readerToArticle(reader);
                                }
                                else
                                {
                                    Console.WriteLine("Namen oder Preis sind unterschiedlich! Reader: " + reader.GetString(7) + " Artikel: " + a.Name + " Decimal Ebay: " + reader.GetDecimal(8) + " Artikel: " + a.EbayPreis);
                                }
                            }
                            
                        }
                    }

                    //Versandkosten
                    if (ebayArtikel != null&&ebayArtikel.EbayItemId!=null)
                    {


                        SqlCommand cmd2 = new SqlCommand("select ItemID, max(ShippingServiceCost) from ebay_item e, ebay_ShippingServiceOptions s where e.kItem=s.kItem AND e.ItemID!='' AND e.SiteID=77 AND ItemID=@itemid group by ItemID  order by ItemID", conn);
                        cmd2.Parameters.AddWithValue("itemid", ebayArtikel.EbayItemId);
                        using (SqlDataReader reader = cmd2.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ebayArtikel.VersandInland =reader.GetDecimal(1);
                                   
                            }
                        }

                        SqlCommand cmd3 = new SqlCommand("select ItemID, max(ShippingServiceCost) from ebay_item e, ebay_InternationalShippingServiceOption s where e.kItem=s.kItem AND e.ItemID!='' AND e.SiteID=77 AND ItemID=@itemid group by ItemID  order by ItemID", conn);
                        cmd3.Parameters.AddWithValue("itemid", ebayArtikel.EbayItemId);
                        using (SqlDataReader reader = cmd3.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ebayArtikel.VersandAusland = reader.GetDecimal(1);

                            }
                        }

                    }

                    //Hersteller
                    if (ebayArtikel != null && ebayArtikel.EbayItemId != null)
                    {


                        SqlCommand cmd4 = new SqlCommand("select i.sku,s.cName,s.cValue from ebay_item i, ebay_specific s where i.kItem=s.kItem and s.cName='Hersteller' " +
                            "AND i.ItemID!='' AND i.SiteID=77 AND i.sku=@sku2", conn);
                        cmd4.Parameters.AddWithValue("sku2", ebayArtikel.SKU);
                        using (SqlDataReader reader = cmd4.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ebayArtikel.Hersteller = reader.GetString(2);
                            }
                        }

                        

                    }





                }

            }

            return ebayArtikel;
        }

        private EbayArtikel readerToArticle(SqlDataReader reader)
        {
            EbayArtikel ebayArtikel = new EbayArtikel();
            ebayArtikel.SKU = reader.GetString(0);
            ebayArtikel.EbayItemId = reader.GetString(1);
            ebayArtikel.PrimaryCatID=reader.GetInt32(2);
            ebayArtikel.StoreCategoryID = reader.GetInt64(3);
            ebayArtikel.StoreCategoryID2 = reader.GetInt64(4);
            ebayArtikel.ListingDuration = reader.GetString(5);
            ebayArtikel.ConditionID=reader.GetInt32(6);
            ebayArtikel.Title= reader.GetString(7);
            ebayArtikel.Price = reader.GetDecimal(8);
            ebayArtikel.ArtikelTyp = reader.GetString(9);
            Console.WriteLine(ebayArtikel.ToString());
            return ebayArtikel;
        }

        
    }
}
