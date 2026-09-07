
using SVN_Tools.Models.Verify;
public interface IIotVerifyEmployeeDataService
{
    Task<IotVerifyEmployeeData> GetByEmpCode(string emp_code);
}