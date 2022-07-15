using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XmlDataNetF
{
    public enum TypeQuery
    {
        Update,
        Insert,
        None
    }
    public static class SqlUtils
    {
        public static int registersAdded = 0;
        public static int recordsAdded = 0;
        public static int recordsCompAdded = 0;
        public static void CheckNewRegisters(SqlConnection connection, SqlCommand command, XmlNode node)
        {
            command.Parameters.Clear();

            command.Parameters.AddWithValue("@None", DBNull.Value);
            command.Parameters.AddWithValue("@IdStation", node["cre_id"].InnerText);
            command.Parameters.AddWithValue("@Name", node["name"].InnerText);
            command.Parameters.AddWithValue("@LocationX", node["location"]["x"].InnerText);
            command.Parameters.AddWithValue("@LocationY",node["location"]["y"].InnerText);            

            connection.Open();

            int result = command.ExecuteNonQuery();
            if (result > 0)
            {
                registersAdded+=1;
            }
            connection.Close();
        }

        public static TypeQuery CheckPriceRecordExist(SqlConnection connection, string queryTop, XmlAttribute type, XmlNode nodePrice, XmlNode nodePlace)
        {
            try
            {
                using (SqlCommand commandF = new SqlCommand(queryTop, connection))
                {
                    commandF.Parameters.Clear();
                    commandF.Parameters.AddWithValue("@IdStation", nodePlace["cre_id"].InnerText);
                    commandF.Parameters.AddWithValue("@Product", type.Value);

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    using (SqlDataReader oReader = commandF.ExecuteReader())
                    {                        
                        if (!oReader.HasRows)
                        {
                            return TypeQuery.Insert;
                        }
                        else
                        {
                            while (oReader.Read())
                            {
                                if (float.TryParse(nodePrice.InnerText, out var priceFormated))
                                {
                                    if (float.Parse(oReader["Val_PrecioVigente"].ToString()) != priceFormated)
                                    {
                                        return TypeQuery.Update;
                                    }
                                }
                            }
                        }
                    }
                    connection.Close();
                }

                return TypeQuery.None;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return TypeQuery.None;
            }

        }

        public static void AddPriceRecord(string queryRecord,string queryRecordComp, string queryUpdate, SqlConnection connection, XmlAttribute type, XmlNode nodePrice, XmlNode nodePlace)
        {
            using (SqlCommand command = new SqlCommand(queryRecord, connection))
            {
                if (float.TryParse(nodePrice.InnerText, out var priceFormated))
                {
                    DateTime myDateTime = DateTime.Now;
                    string sqlFormattedDate = myDateTime.ToString("AAAA-MM-DD HH:mm:ss.mmm");
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@None", DBNull.Value);
                    command.Parameters.AddWithValue("@IdStation", nodePlace["cre_id"].InnerText);
                    command.Parameters.AddWithValue("@Price", priceFormated);
                    command.Parameters.AddWithValue("@Product", type.Value);
                    command.Parameters.AddWithValue("@Date", myDateTime);
                    
                    command.Parameters.AddWithValue("@ProductId", type.Value.Trim().ToLower() == "premium" ? 1 
                                                                 : type.Value.Trim().ToLower() == "regular" ? 2
                                                                 : 3);
                    int result = command.ExecuteNonQuery();
                    if (result > 0)
                    {
                        recordsAdded+=1;
                    }
                }
                
            }
            //Historico competencias
            using (SqlCommand command = new SqlCommand(queryRecordComp, connection))
            {
                if (float.TryParse(nodePrice.InnerText, out var priceFormated))
                {
                    DateTime myDateTime = DateTime.Now;
                    string sqlFormattedDate = myDateTime.ToString("AAAA-MM-DD HH:mm:ss.mmm");
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@None", DBNull.Value);
                    command.Parameters.AddWithValue("@Price", priceFormated);
                    command.Parameters.AddWithValue("@Product", type.Value);
                    command.Parameters.AddWithValue("@Date", myDateTime);
                    command.Parameters.AddWithValue("@IdStation", nodePlace["cre_id"].InnerText);
                    command.Parameters.AddWithValue("@ProductId", type.Value.Trim().ToLower() == "premium" ? 1
                                                                 : type.Value.Trim().ToLower() == "regular" ? 2
                                                                 : 3);
                    int result = command.ExecuteNonQuery();
                    if (result > 0)
                    {
                        recordsCompAdded += 1;
                    }
                }

            }
            using (SqlCommand command = new SqlCommand(queryUpdate, connection))
            {
                if (float.TryParse(nodePrice.InnerText, out var priceFormated))
                {
                    DateTime myDateTime = DateTime.Now;
                    string sqlFormattedDate = myDateTime.ToString("AAAA-MM-DD HH:mm:ss.mmm");
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@None", DBNull.Value);
                    command.Parameters.AddWithValue("@Price", priceFormated);
                    command.Parameters.AddWithValue("@Product", type.Value);
                    command.Parameters.AddWithValue("@Date", myDateTime);
                    command.Parameters.AddWithValue("@IdStation", nodePlace["cre_id"].InnerText);
                    command.Parameters.AddWithValue("@ProductId", type.Value.Trim().ToLower() == "premium" ? 1
                                                                 : type.Value.Trim().ToLower() == "regular" ? 2
                                                                 : 3);
                    int result = command.ExecuteNonQuery();
                    //if (result > 0)
                    //{
                    //    recordsAdded++;
                    //}
                }
                connection.Close();
            }            
        }
    }
}
