using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Xml;

namespace XmlData
{
    public static class SqlUtils
    {
        public static void CheckNewRegisters(SqlConnection connection, SqlCommand command, XmlNode node, int registersAdded)
        {
            command.Parameters.Clear();

            command.Parameters.AddWithValue("@IdStation", node["cre_id"].InnerText);
            command.Parameters.AddWithValue("@Name", node["name"].InnerText);
            command.Parameters.AddWithValue("@Location", string.Format("{0},{1}",
                                                        node["location"]["x"].InnerText,
                                                        node["location"]["y"].InnerText));
            connection.Open();

            int result = command.ExecuteNonQuery();
            if (result > 0)
            {
                registersAdded++;
            }
            connection.Close();
        }

        public static bool CheckPriceRecordExist(SqlConnection connection, string queryTop, XmlAttribute type, XmlNode nodePrice, XmlNode nodePlace)
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
                            return false;
                        }
                        else
                        {
                            while (oReader.Read())
                            {
                                if (float.TryParse(nodePrice.InnerText, out var priceFormated))
                                {
                                    if (float.Parse(oReader["Price"].ToString()) != priceFormated)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                    connection.Close();
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }

        }

        public static void AddPriceRecord(string queryRecord, SqlConnection connection, XmlAttribute type, XmlNode nodePrice, XmlNode nodePlace, int recordsAdded)
        {
            using (SqlCommand command = new SqlCommand(queryRecord, connection))
            {
                if (float.TryParse(nodePrice.InnerText, out var priceFormated))
                {
                    DateTime myDateTime = DateTime.Now;
                    string sqlFormattedDate = myDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Price", priceFormated);
                    command.Parameters.AddWithValue("@Product", type.Value);
                    command.Parameters.AddWithValue("@Date", sqlFormattedDate);
                    command.Parameters.AddWithValue("@IdStation", nodePlace["cre_id"].InnerText);
                    int result = command.ExecuteNonQuery();
                    if (result > 0)
                    {
                        recordsAdded++;
                    }
                }
                connection.Close();
            }
        }
    }
}
