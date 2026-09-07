
using SVN_Tools.Models.Verify;
public interface IVerifyEmployeeDataService
{
    Task<VerifyEmployeeData> CreateAsync(VerifyEmployeeData data);

    Task<List<VerifyEmployeeData>> GetAllAsync();

    Task<VerifyEmployeeData> GetBySvnCode(string SVNCODE);
}