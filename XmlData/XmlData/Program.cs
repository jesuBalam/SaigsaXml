using System;
using System.Linq;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Timers;
using System.Threading;
using System.Configuration;

namespace XmlData
{
    class Program
    {       
        
        public static int registersAdded = 0;
        public static int recordsAdded = 0;
        
        static void Main(string[] args)
        {           
            while (true)
            {
                Console.WriteLine("*** Starting *** ");
                ExecuteMainTask();
                Thread.Sleep(60 * 60 * 1000 * Convert.ToInt32( ConfigurationManager.AppSettings["HoursRate"].ToString()));                
            }
        }

        

        private static void ExecuteMainTask()
        {
            Console.WriteLine("Please wait, downloading resources...");
            string dataPlaces = DownloadData(ConfigurationManager.AppSettings["UrlXmlPlaces"]);
            string dataPrices = DownloadData(ConfigurationManager.AppSettings["UrlXmlPrices"]);

            int index = 0;
            while (File.Exists(string.Format("{0}places{1}.xml", ConfigurationManager.AppSettings["RootPath"], index)))
            {
                index++;
            }
            File.WriteAllText(string.Format("{0}places{1}.xml", ConfigurationManager.AppSettings["RootPath"], index), dataPlaces);
            File.WriteAllText(string.Format("{0}prices{1}.xml", ConfigurationManager.AppSettings["RootPath"], index), dataPrices);
            Console.WriteLine("New Files created.");


            XmlDocument xmlPlaces = new XmlDocument();
            XmlDocument xmlPrices = new XmlDocument();
            
            List<PricesRegister> pricesRegisterList = new List<PricesRegister>();

            xmlPlaces.Load(string.Format("{0}places{1}.xml", ConfigurationManager.AppSettings["RootPath"], index));
            xmlPrices.Load(string.Format("{0}prices{1}.xml", ConfigurationManager.AppSettings["RootPath"], index));

            Console.WriteLine("Accessing DB...");
            SqlConnection connection = new SqlConnection(string.Format("Server={0};Integrated security=SSPI;database={1}", ConfigurationManager.AppSettings["ServerDatabase"], ConfigurationManager.AppSettings["Database"]));
            try
            {
                string querySearch = "IF NOT EXISTS (SELECT * FROM Stations WHERE IdStation=@IdStation) BEGIN INSERT INTO Stations(IdStation, Name, Location) VALUES (@IdStation, @Name, @Location) END";
                string queryTop = "SELECT TOP 1 * FROM PricesRecords WHERE StationID=(Select ID from Stations WHERE IdStation=@IdStation) AND Product=@Product ORDER BY Id DESC";
                string queryRecord = "INSERT INTO PricesRecords(Price, Product, Date, StationID) VALUES (@Price, @Product, @Date, (Select ID from Stations WHERE IdStation=@IdStation))";
                SqlCommand command = new SqlCommand(querySearch, connection);
                SqlCommand commandFind = new SqlCommand(queryTop, connection);
                SqlCommand commandRecord = new SqlCommand(queryRecord, connection);

                Console.WriteLine("Validating for new registers.");

                foreach (XmlNode nodePlace in xmlPlaces.DocumentElement.ChildNodes)
                {
                    if (nodePlace.NodeType == XmlNodeType.Element)
                    {
                        foreach (XmlAttribute attrPl in nodePlace.Attributes)
                        {
                            PricesRegister pricesRegister = new PricesRegister();
                            SqlUtils.CheckNewRegisters(connection, command, nodePlace, registersAdded);

                            foreach (XmlNode nodePrice in xmlPrices.DocumentElement.ChildNodes)
                            {
                                foreach (XmlAttribute attrPr in nodePrice.Attributes)
                                {
                                    if (attrPl.Value == attrPr.Value)
                                    {
                                        foreach (XmlNode child in nodePrice.ChildNodes)
                                        {
                                            foreach (XmlAttribute attrType in child.Attributes)
                                            {
                                                if (!SqlUtils.CheckPriceRecordExist(connection, queryTop, attrType, child, nodePlace))
                                                {
                                                    SqlUtils.AddPriceRecord(queryRecord, connection, attrType, child, nodePlace, recordsAdded);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine(string.Format("{0} registers added.", registersAdded));
                Console.WriteLine(string.Format("{0} records added.", recordsAdded));
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }

        public static string DownloadData(string uri)
        {
            string dataXml;
            using (var client = new System.Net.WebClient())
            {
                dataXml = client.DownloadString(uri);
            }
            return dataXml;
        }
    }
}
