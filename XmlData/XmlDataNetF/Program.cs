using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace XmlDataNetF
{
    class Program
    {
        //public static int registersAdded = 0;
        //public static int recordsAdded = 0;

        static void Main(string[] args)
        {
            //Console.WriteLine("Press any key to start");
            //Console.ReadKey();
            //QueryUtils.ReadFile();
            //Console.ReadKey();
            while (true)
            {


                Console.WriteLine("*** Starting *** ");
                SqlUtils.recordsAdded = 0;
                SqlUtils.registersAdded = 0;
                SqlUtils.recordsCompAdded = 0;
                ExecuteMainTask();
                Thread.Sleep(60 * 60 * 1000 * Convert.ToInt32(ConfigurationManager.AppSettings["HoursRate"].ToString()));
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
            

            xmlPlaces.Load(string.Format("{0}places{1}.xml", ConfigurationManager.AppSettings["RootPath"], index));
            xmlPrices.Load(string.Format("{0}prices{1}.xml", ConfigurationManager.AppSettings["RootPath"], index));

            Console.WriteLine("Accessing DB...");            
            try
            {
                SqlConnection connection = new SqlConnection(string.Format("Server={0};Integrated security=SSPI;database={1}", ConfigurationManager.AppSettings["ServerDatabase"], ConfigurationManager.AppSettings["Database"]));
                //string querySearch = "IF NOT EXISTS (SELECT * FROM Stations WHERE IdStation=@IdStation) BEGIN INSERT INTO Stations(IdStation, Name, Location) VALUES (@IdStation, @Name, @Location) END";

                string querySearch = "IF NOT EXISTS (SELECT * FROM CatEstaciones WHERE NroCre=@IdStation) BEGIN INSERT INTO CatEstaciones(NroCre, CtEs_Nombre, CtEs_X,CtEs_Y, CtEs_Brand, CtEs_Category, CtEs_Direccion, IdBrand, ID_MARCA, CtEFs_Num, CtMun_Num, GIES ) VALUES (@IdStation, @Name, @LocationX, @LocationY, @None, @None, @None, @None, @None, @None, @None, @None) END";
                //string queryTop = "SELECT TOP 1 * FROM LastRecordPrices WHERE StationID=(Select ID from Stations WHERE IdStation=@IdStation) AND Product=@Product ORDER BY Id DESC";
                string queryTop = "SELECT TOP 1 * FROM Validacion WHERE NroCre=(Select NroCre from CatEstaciones WHERE NroCre=@IdStation) AND Val_SubProducto=@Product ORDER BY Val_FechaAplicacion DESC";
                //string queryRecord = "INSERT INTO PricesRecords(Price, Product, Date, StationID) VALUES (@Price, @Product, @Date, (Select ID from Stations WHERE IdStation=@IdStation))";                
                string queryRecord = "INSERT INTO Historico(Hist_FechaCaptura, NroCre, Hist_Producto, Hist_SubProducto, Hist_Marca, Hist_PrecioVigente, Hist_FechaAplicacion, CtPrd_ID) VALUES (@Date, (Select NroCre from CatEstaciones WHERE NroCre=@IdStation), @None, @Product, @None,  @Price, @Date, @ProductId)";
                //string queryRecordLast = "INSERT INTO LastRecordPrices(Price, Product, Date, StationID) VALUES (@Price, @Product, @Date, (Select ID from Stations WHERE IdStation=@IdStation))";
                string queryRecordComp = "IF EXISTS (SELECT * FROM Competencias WHERE Comp_NroCreComp=@IdStation) BEGIN INSERT INTO HistoricoCompetencias(HistC_FechaCaptura, NroCre, HistC_Producto, HistC_SubProducto, HistC_Marca, HistC_PrecioVigente, HistC_FechaAplicacion, CtPrd_ID) VALUES (@Date, (Select NroCre from CatEstaciones WHERE NroCre=@IdStation), @None, @Product, @None,  @Price, @Date, @ProductId) END";
                string queryRecordLast = "INSERT INTO Validacion(Val_FechaCaptura, NroCre, Val_Producto, Val_SubProducto, Val_Marca, Val_PrecioVigente, Val_FechaAplicacion, CtPrd_ID) VALUES (@Date,(Select NroCre from CatEstaciones WHERE NroCre=@IdStation), @None, @Product, @None, @Price, @Date, @ProductId)";
                //string queryUpdate = "UPDATE LastRecordPrices SET Price=@Price, Product=@Product, Date=@Date, StationID=(Select ID from Stations WHERE IdStation=@IdStation) WHERE StationID=(Select ID from Stations WHERE IdStation=@IdStation) AND Product=@Product ";
                string queryUpdate = "UPDATE Validacion SET Val_FechaCaptura=@Date, NroCre=(Select NroCre from CatEstaciones WHERE NroCre=@IdStation), Val_Producto=@None,  Val_SubProducto=@Product, Val_Marca=@None, Val_PrecioVigente=@Price, Val_FechaAplicacion=@Date, CtPrd_ID=@ProductId WHERE NroCre=(Select NroCre from CatEstaciones WHERE NroCre=@IdStation) AND Val_SubProducto=@Product";
                
                SqlCommand command = new SqlCommand(querySearch, connection);
                SqlCommand commandFind = new SqlCommand(queryTop, connection);
                SqlCommand commandRecord = new SqlCommand(queryRecord, connection);
                SqlCommand commandUpdate = new SqlCommand(queryUpdate, connection);

                Console.WriteLine("Validating for new registers and adding data to database. Please wait");
                int count = 0;

                using (var progress = new ProgressBar())
                {

                    foreach (XmlNode nodePlace in xmlPlaces.DocumentElement.ChildNodes)
                    {
                        if (nodePlace.NodeType == XmlNodeType.Element)
                        {
                            foreach (XmlAttribute attrPl in nodePlace.Attributes)
                            {                            
                                SqlUtils.CheckNewRegisters(connection, command, nodePlace);

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
                                                    TypeQuery typeQuery = SqlUtils.CheckPriceRecordExist(connection, queryTop, attrType, child, nodePlace);
                                                    if (typeQuery!=TypeQuery.None)
                                                    {
                                                        SqlUtils.AddPriceRecord(queryRecord, queryRecordComp, typeQuery == TypeQuery.Insert ? queryRecordLast : queryUpdate, connection, attrType, child, nodePlace);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            count++;
                            progress.Report((double)count / xmlPlaces.DocumentElement.ChildNodes.Count);
                            //Console.WriteLine("Element " +count + "/"+ xmlPlaces.DocumentElement.ChildNodes.Count);
                        }
                    }
                }
                Console.WriteLine(string.Format("{0} registers added.", SqlUtils.registersAdded));
                Console.WriteLine(string.Format("{0} records added.", SqlUtils.recordsAdded));
                Console.WriteLine(string.Format("{0} recordsComp added.", SqlUtils.recordsCompAdded));
                Console.WriteLine("Waiting for next execution");
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                Console.ReadLine();
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
