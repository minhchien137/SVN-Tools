using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Label;
using SVN_Tools.Models.Utils;
using SVN_Tools.Services.Configurations;

namespace SVN_Tools.Services.Utils
{
    public class LabelInfoService
    {
        string _connectionString;
        DBConfiguration _dBConfiguration;

        private readonly AppDbContext _context;

        public LabelInfoService(DBConfiguration dBConfiguration, AppDbContext context)
        {
            _dBConfiguration = dBConfiguration;
            _connectionString = _dBConfiguration.GetConnectionString();
            _context = context;
        }

        public async Task<WhoopLabel[]> ReadByUpcNumber(string upc_number)
        {
            // WhoopLabel dataUI = new WhoopLabel();
            string tableName = "SVN_Whoop_Label_Info";
            int timeOut = 1000;
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionString))
                {
                    string sql = string.Empty;
                    var param = new object();
                    sql = "select * from " + tableName + " Where upc_number = @upc_number";
                    param = new { upc_number = upc_number };
                    var data = await conn.QueryAsync<WhoopLabel>(sql, param, commandTimeout: timeOut, commandType: CommandType.Text);

                    return data.ToArray();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // public async Task<Boolean> CheckToastOneSerialNumber(string serialNumber)
        // {
        //     string tableName = "SVN_Whoop_Label_Info";
        //     int timeOut = 1000;
        //     if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Length != 13)
        //     {
        //         return false;
        //     }

        //     try
        //     {
        //         using (IDbConnection conn = new SqlConnection(_connectionString))
        //         {
        //             string checkSql = $"SELECT COUNT(1) FROM {tableName} WHERE upc_number = @upc_number";
        //             var checkParam = new { upc_number = serialNumber };
        //             var exists = await conn.ExecuteScalarAsync<bool>(checkSql, checkParam, commandTimeout: timeOut);
        //             if (exists)
        //             {
        //                 return false;
        //             }
        //             else
        //             {
        //                 string insertSql = $"INSERT INTO {tableName} (upc_number) VALUES (@upc_number)";
        //                 var insertParam = new { upc_number = serialNumber };
        //                 int rowsAffected = await conn.ExecuteAsync(insertSql, insertParam, commandTimeout: timeOut);
        //                 return rowsAffected > 0;
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         return false;
        //     }
        // }


        // FQC + FCT + In tem 1 seri
        public async Task<bool> CheckToastOneSerialNumber(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Length != 13)
                return false;

            try
            {
                // 1️⃣ Tìm serial trong bảng SVN_Toast_Serial_Info
                var item = await _context.SVNToastSerialInfos
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(x => x.SerialNumber == serialNumber);

                if (item == null)
                {
                    // Serial chưa có trong DB => chưa qua FCT => không cho in
                    return false;
                }

                // // 2️⃣ Kiểm tra FCT_status
                if (item.FCTStatus == "OK")
                {
                    // Đã qua FCT => hợp lệ
                    return true;
                }

                // Chưa có FCT_status => chưa qua FCT
                return true;
            }
            catch (Exception ex)
            {
                // Có thể log lỗi ra nếu cần
                return false;
            }
        }


    }
}