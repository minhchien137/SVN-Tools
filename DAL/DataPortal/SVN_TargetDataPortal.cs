using Dapper;
using SVN_Tools.DAL.DTO;
using SVN_Tools.Models;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
namespace SVN_Tools.DAL.DataPortal
{
    public class SVN_TargetDataPortal
    {
        string connectionString;
        public SVN_TargetDataPortal(string connectionString)
        {
            this.connectionString = connectionString;
        }


        public async Task<List<SVN_target>> ReadList(string date, string storedProceduce = "SVN_Pro_CalTarget")
        {
            List<SVN_target> dataUI = new List<SVN_target>();
            int timeOut = 1000;
            try
            {
                using (IDbConnection conn = new SqlConnection(connectionString))
                {
                  //Gọi thủ tục tính toán kết quả theo tarhet
                    string storedProcedure = storedProceduce;
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("date_time", date);
                   
                    var datas = await conn.QueryAsync<SVN_target>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
                    return datas.ToList();
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

