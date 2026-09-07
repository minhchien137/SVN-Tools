// Đặt file này trong thư mục Services
using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Verify;

public class IotVerifyEmployeeDataService : IIotVerifyEmployeeDataService
{
    private readonly AppDbContext _context;

    public IotVerifyEmployeeDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IotVerifyEmployeeData> GetByEmpCode(string emp_code)
    {
        var data = await _context.IotVerifyEmployeeDatas
                    .Where(d => d.emp_code == emp_code)
                    .FirstOrDefaultAsync();
        return data;
    }


}