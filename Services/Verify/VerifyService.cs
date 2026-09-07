// Đặt file này trong thư mục Services
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Verify;

public class VerifyEmployeeDataService : IVerifyEmployeeDataService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public VerifyEmployeeDataService(AppDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<VerifyEmployeeData> CreateAsync(VerifyEmployeeData data)
    {
        _context.VerifyEmployeeDatas.Add(data);
        await _context.SaveChangesAsync();
        return data;

    }

    public async Task<List<VerifyEmployeeData>> GetAllAsync()
    {
        return await _context.VerifyEmployeeDatas.ToListAsync();
    }

    public async Task<VerifyEmployeeData> GetBySvnCode(string SVNCODE)
    {
        var data = await _context.VerifyEmployeeDatas
            .Where(d => d.SVNCODE == SVNCODE)
            .FirstOrDefaultAsync();
        return data;
    }


}