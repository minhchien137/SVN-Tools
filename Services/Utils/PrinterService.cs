using System.Data;
using System.Data.SqlClient;
using Dapper;
using PrinterServices.Objects;
using SVN_Tools.Models.Utils;
using SVN_Tools.Services.Configurations;

namespace SVN_Tools.Services.Utils
{
    public class PrinterService
    {
        string connectionString;
        DBConfiguration _dBConfiguration;

        public PrinterService(DBConfiguration dBConfiguration)
        {
            _dBConfiguration = dBConfiguration;
            connectionString = _dBConfiguration.GetConnectionString();

        }

        public async Task<PrinterData> ReadByID(string ID_Printer)
        {
            PrinterData dataUI = new PrinterData();
            string tableName = "SVN_Printer_Info_New";
            int timeOut = 1000;
            try
            {
                using (IDbConnection conn = new SqlConnection(connectionString))
                {
                    string sql = string.Empty;
                    var param = new object();
                    sql = "select * from " + tableName + " Where ID_Printer = @ID_Printer";
                    param = new { ID_Printer = ID_Printer };
                    var data = await conn.QueryFirstOrDefaultAsync<PrinterData>(sql, param, commandTimeout: timeOut, commandType: CommandType.Text);
                    dataUI = data;
                }
                return dataUI;
            }
            catch
            {
                return null;
            }
        }


        public async Task<List<PrinterData>> ReadList()
        {
            List<PrinterData> dataUI = new List<PrinterData>();
            string tableName = "SVN_Printer_Info_New";
            int timeOut = 1000;
            try
            {
                using (IDbConnection conn = new SqlConnection(connectionString))
                {
                    string sql = string.Empty;
                    var param = new object();
                    sql = "select * from " + tableName;
                    param = new object();
                    var data = await conn.QueryAsync<PrinterData>(sql, param, commandTimeout: timeOut, commandType: CommandType.Text);
                    dataUI = data.ToList();
                }
                return dataUI;
            }
            catch
            {
                return null;
            }
        }
    }

}