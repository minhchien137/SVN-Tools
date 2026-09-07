// Đặt file này trong thư mục Services
using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Label;

public class WHLabelInfoService
{
    private readonly AppDbContext _context;

    public WHLabelInfoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WHLabelInfo> CreateAsync(WHLabelInfo data)
    {
        _context.WHLabelInfos.Add(data);
        await _context.SaveChangesAsync();
        return data;
    }

    public async Task<string> GetPackageListByPrefix(string prefix)
    {
        var maxLabelId = await _context.WHLabelInfos
       .Where(x => x.Label_id.StartsWith(prefix))
       .OrderByDescending(x => x.Label_id)
       .Select(x => x.Label_id)
       .FirstOrDefaultAsync();

        string nextId;

        if (string.IsNullOrEmpty(maxLabelId))
        {
            // Nếu chưa có bản ghi nào bắt đầu bằng prefix
            nextId = prefix + "000001";
        }
        else
        {
            // Lấy phần số sau prefix
            var currentNumber = int.Parse(maxLabelId.Substring(prefix.Length));
            var nextNumber = currentNumber + 1;

            // Format về 6 chữ số, thêm số 0 ở trước nếu thiếu
            nextId = prefix + nextNumber.ToString("D6");
        }

        return nextId;
    }


}