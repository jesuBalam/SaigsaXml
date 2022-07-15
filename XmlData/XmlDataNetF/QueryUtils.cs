using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XmlDataNetF
{
    public static class QueryUtils
    {
        public static int indexSheet = 1;
        public static List<DataTable> dataTables = new List<DataTable>();
        public static void ReadFile()
        {
            string script = File.ReadAllText(@"C:\Users\enriq\OneDrive\Escritorio\SQLQuery27.sql");           
            string regexSemicolon = ";(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
            string regexGo = @"^\s*GO\s*$";
            IEnumerable<string> commandStrings = Regex.Split(script, regexSemicolon, RegexOptions.Multiline | RegexOptions.IgnoreCase);            
            SqlConnection connection = new SqlConnection(string.Format("Server={0};Integrated security=SSPI;database={1}", ConfigurationManager.AppSettings["ServerDatabase"], ConfigurationManager.AppSettings["Database"]));            
            connection.Open();
            foreach (string commandString in commandStrings)
            {
                Console.WriteLine("DD::" + commandString);
                if (!string.IsNullOrWhiteSpace(commandString.Trim()))
                {
                    using (var command = new SqlCommand(commandString, connection))
                    {
                        using (SqlDataReader oReader = command.ExecuteReader())
                        {
                            var dataReader = oReader;
                            var dataTable = new DataTable();                            
                            dataTable.Load(dataReader);
                            Console.WriteLine(dataTable.Columns[0]);
                            dataTables.Add(dataTable);
                        }
                    }
                }
            }
            WriteExcelFile(@"C:\Users\enriq\OneDrive\Escritorio\SQLQuery27.xlsx", dataTables);
            connection.Close();
        }

        private static void WriteExcelFile(string path, List<DataTable> tables)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                foreach(var table in tables)
                {
                    WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    worksheetPart.Worksheet = new Worksheet(sheetData);

                    Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    Sheet sheet = new Sheet()
                    { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = Convert.ToUInt32(indexSheet), Name = "Sheet" + indexSheet };

                    sheets.Append(sheet);
                    indexSheet++;

                    Row headerRow = new Row();

                    List<String> columns = new List<string>();
                    foreach (System.Data.DataColumn column in table.Columns)
                    {
                        columns.Add(column.ColumnName);

                        Cell cell = new Cell();
                        cell.DataType = CellValues.String;
                        cell.CellValue = new CellValue(column.ColumnName);
                        headerRow.AppendChild(cell);
                    }

                    sheetData.AppendChild(headerRow);

                    foreach (DataRow dsrow in table.Rows)
                    {
                        Row newRow = new Row();
                        foreach (String col in columns)
                        {
                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(dsrow[col].ToString());
                            newRow.AppendChild(cell);
                        }

                        sheetData.AppendChild(newRow);
                    }
                }
                

                workbookPart.Workbook.Save();
            }
        }
    }
}
