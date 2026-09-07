// Đặt file này trong thư mục Services
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Label;
using SVN_Tools.Models.Utils;

public class AstroLabelDataService : IAstroLabelDataService
{
    private readonly AppDbContext _context;

    public AstroLabelDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AstroLabelData> CreateAsync(AstroLabelData data)
    {
        _context.AstroLabelDatas.Add(data);
        await _context.SaveChangesAsync();
        return data;
    }

    public async Task<AstroLabelData> GetByPalletIdAsync(string palletID)
    {
        var data = await _context.AstroLabelDatas
            .Where(d => d.PalletID == palletID)
            .FirstOrDefaultAsync();
        return data;
    }

    public async Task<IEnumerable<AstroLabelData>> GetAllAsync()
    {
        return await _context.AstroLabelDatas.ToListAsync();
    }

    public async Task<AstroLabelData?> UpdateAsync(AstroLabelData data)
    {
        var existingData = await _context.AstroLabelDatas.FindAsync(data.PalletID); // Giả sử có Id
        if (existingData == null)
        {
            return null;
        }

        // Cập nhật các thuộc tính
        _context.Entry(existingData).CurrentValues.SetValues(data);
        await _context.SaveChangesAsync();
        return existingData;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var data = await _context.AstroLabelDatas.FindAsync(id);
        if (data == null)
        {
            return false;
        }

        _context.AstroLabelDatas.Remove(data);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AstroLabelData>> GetPackageListByPrefix(string prefix)
    {
        return await _context.AstroLabelDatas.Where(d => d.PackageID.StartsWith(prefix) && d.isDeleted == false).ToListAsync();
    }

    public async Task<bool> checkSerialExist(string prefix, string serial)
    {
        // Sử dụng AnyAsync() để kiểm tra sự tồn tại của bản ghi
        return await _context.AstroLabelDatas
            .AnyAsync(d => d.PackageID.StartsWith(prefix) && d.Serial.Contains(serial) && d.isDeleted == false);
    }

    public async Task<bool> DeletePackageId(string PackageID)
    {
        var existingData = await _context.AstroLabelDatas.Where(d => d.PackageID == PackageID).FirstOrDefaultAsync();

        if (existingData == null)
        {
            return false;
        }

        existingData.isDeleted = true;
        existingData.PackageID = PackageID + "_deleted" + DateTime.Now.ToString();

        // Cập nhật các thuộc tính
        _context.Entry(existingData).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountPalletID(string PalletID)
    {
        var count = await _context.AstroLabelDatas.CountAsync(d => d.PalletID == PalletID && d.isDeleted == false);
        return count;
    }

    public async Task<bool> PrintPallet(string palletID)
    {
        var printerName = "Pallet_Toast";
        var printer = await _context.PrinterInfos.Where(p => p.Name_Printer == printerName).FirstOrDefaultAsync();

        var template = printer.ZPL_Temp;

        var list = await _context.AstroLabelDatas.Where(p => p.PalletID == palletID && p.isDeleted == false).ToListAsync();

        // if (list.Count != 30)
        // {
        //     return false;
        // }

        string ser1 = string.Join("", list.Take(10).Select(r => r.Serial));


        string ser2 = string.Join("", list.Skip(10).Take(10).Select(r => r.Serial));


        string ser3 = string.Join("", list.Skip(20).Take(10).Select(r => r.Serial));

        ser1 = ser1.TrimEnd(',');
        ser2 = ser2.TrimEnd(',');
        ser3 = ser3.TrimEnd(',');

        var newTemp = template.Replace("{ser1}", ser1).Replace("{ser2}", ser2).Replace("{ser3}", ser3);

        var host = !string.IsNullOrWhiteSpace(printer.IP_Printer)
        ? printer.IP_Printer
        : printer.Name_Printer;

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Printer host is empty.");

        // Port phải là int
        if (!int.TryParse(printer.Port_Printer, out var port))
            port = 6101; // mặc định thường dùng cho ZPL/RAW

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(host, port);

            using (var stream = client.GetStream())
            {
                byte[] zplBytes = Encoding.UTF8.GetBytes(newTemp);

                int copies = 2;

                for (int i = 0; i < copies; i++)
                {
                    await stream.WriteAsync(zplBytes, 0, zplBytes.Length);
                    Task.Delay(50).Wait();
                }
            }
        }

        return true;
    }

    // public async Task<bool> PrintPalletWithBody(string palletID, LabelDataRequest request)
    // {
    //     var printerName = "Pallet_Toast";
    //     var printer = await _context.PrinterInfos.Where(p => p.Name_Printer == printerName).FirstOrDefaultAsync();

    //     var template = printer.ZPL_Temp;

    //     var list = await _context.AstroLabelDatas.Where(p => p.PalletID == palletID && p.isDeleted == false).ToListAsync();

    //     if (list.Count != 30)
    //     {
    //         return false;
    //     }

    //     string ser1 = string.Join("", list.Take(10).Select(r => r.Serial));


    //     string ser2 = string.Join("", list.Skip(10).Take(10).Select(r => r.Serial));


    //     string ser3 = string.Join("", list.Skip(20).Take(10).Select(r => r.Serial));

    //     ser1 = ser1.TrimEnd(',');
    //     ser2 = ser2.TrimEnd(',');
    //     ser3 = ser3.TrimEnd(',');
    //     string[] parts = request.lotId.Split('-');
    //     string toastPartNumber1 = request.toastPartNumber.Substring(0, 2);
    //     string toastPartNumber2 = request.toastPartNumber.Substring(2);

    //     var newTemp = template.Replace("{ser1}", ser1).Replace("{ser2}", ser2).Replace("{ser3}", ser3).Replace("{toastPartNumber}", request.toastPartNumber)
    //             .Replace("{toastPartNumber1}", toastPartNumber1)
    //             .Replace("{toastPartNumber2}", toastPartNumber2)
    //             .Replace("{modelNumber}", request.modelNumber)
    //             .Replace("{desc}", request.desc).Replace("{lotId}", request.lotId).Replace("{descFr}", request.descFr)
    //             .Replace("{poNumber}", request.poNumber)
    //             .Replace("{lotId1}", parts[0])
    //             .Replace("{lotId2}", parts[1])
    //             .Replace("{lotId3}", parts[2]);

    //     var host = !string.IsNullOrWhiteSpace(printer.IP_Printer)
    //     ? printer.IP_Printer
    //     : printer.Name_Printer;

    //     if (string.IsNullOrWhiteSpace(host))
    //         throw new InvalidOperationException("Printer host is empty.");

    //     // Port phải là int
    //     if (!int.TryParse(printer.Port_Printer, out var port))
    //         port = 6101; // mặc định thường dùng cho ZPL/RAW

    //     using (var client = new TcpClient())
    //     {
    //         await client.ConnectAsync(host, port);

    //         using (var stream = client.GetStream())
    //         {
    //             byte[] zplBytes = Encoding.UTF8.GetBytes(newTemp);

    //             int copies = 2;

    //             for (int i = 0; i < copies; i++)
    //             {
    //                 await stream.WriteAsync(zplBytes, 0, zplBytes.Length);
    //                 Task.Delay(50).Wait();
    //             }
    //         }
    //     }

    //     return true;
    // }

    public async Task<bool> PrintPalletWithBody(string palletID, LabelDataRequest request)
    {
        var printerName = "Pallet_Toast";
        var printer = await _context.PrinterInfos.Where(p => p.Name_Printer == printerName).FirstOrDefaultAsync();

        var template = printer.ZPL_Temp;

        var list = await _context.AstroLabelDatas.Where(p => p.PalletID == palletID && p.isDeleted == false).ToListAsync();

        if (list.Count == 0 || list.Count > 30)
            return false;

        string ser1 = string.Join("", list.Take(10).Select(r => r.Serial));
        string ser2 = string.Join("", list.Skip(10).Take(10).Select(r => r.Serial));
        string ser3 = string.Join("", list.Skip(20).Take(10).Select(r => r.Serial));

        ser1 = ser1.TrimEnd(',');
        ser2 = ser2.TrimEnd(',');
        ser3 = ser3.TrimEnd(',');

        if (list.Count <= 10)
        {
            template = template.Replace("{ser1}", ser1);
            template = RemoveZplBlock(template, "{ser2}");
            template = RemoveZplBlock(template, "{ser3}");
        }
        else if (list.Count <= 20)
        {
            template = template.Replace("{ser1}", ser1).Replace("{ser2}", ser2);
            template = RemoveZplBlock(template, "{ser3}");
        }
        else
        {
            template = template.Replace("{ser1}", ser1).Replace("{ser2}", ser2).Replace("{ser3}", ser3);
        }

        string[] parts = request.lotId.Split('-');
        string toastPartNumber1 = request.toastPartNumber.Substring(0, 2);
        string toastPartNumber2 = request.toastPartNumber.Substring(2);

        var newTemp = template
            .Replace("{toastPartNumber}", request.toastPartNumber)
            .Replace("{toastPartNumber1}", toastPartNumber1)
            .Replace("{toastPartNumber2}", toastPartNumber2)
            .Replace("{modelNumber}", request.modelNumber)
            .Replace("{desc}", request.desc)
            .Replace("{lotId}", request.lotId)
            .Replace("{descFr}", request.descFr)
            .Replace("{poNumber}", request.poNumber)
            .Replace("{lotId1}", parts[0])
            .Replace("{lotId2}", parts[1])
            .Replace("{lotId3}", parts[2]);

        var host = !string.IsNullOrWhiteSpace(printer.IP_Printer)
            ? printer.IP_Printer
            : printer.Name_Printer;

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Printer host is empty.");

        if (!int.TryParse(printer.Port_Printer, out var port))
            port = 6101;

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(host, port);

            using (var stream = client.GetStream())
            {
                byte[] zplBytes = Encoding.UTF8.GetBytes(newTemp);

                int copies = 2;

                for (int i = 0; i < copies; i++)
                {
                    await stream.WriteAsync(zplBytes, 0, zplBytes.Length);
                    Task.Delay(50).Wait();
                }
            }
        }

        return true;
    }

    private string RemoveZplBlock(string template, string placeholder)
    {
        // Tìm vị trí ^FT trước placeholder và ^FS sau placeholder rồi xóa cả block
        int fsIndex = template.IndexOf(placeholder);
        if (fsIndex < 0) return template;

        // Tìm ^FT gần nhất trước placeholder
        int startIndex = template.LastIndexOf("^FT", fsIndex);
        if (startIndex < 0) return template;

        // Tìm ^FS gần nhất sau placeholder
        int endIndex = template.IndexOf("^FS", fsIndex);
        if (endIndex < 0) return template;

        endIndex += 3; // bao gồm cả "^FS"

        return template.Remove(startIndex, endIndex - startIndex);
    }

    public async Task<List<AstroLabelData>> GetPallet(string palletID)
    {
        return await _context.AstroLabelDatas.Where(d => d.PalletID == palletID && d.isDeleted == false).ToListAsync();
    }


    public async Task<bool> DeleteBox(string serial)
    {
        var delete_box = await _context.AstroLabelDatas.Where(d => d.Serial == serial).FirstOrDefaultAsync(); // Tìm sản phẩm theo khóa chính

        if (delete_box != null)
        {
            _context.AstroLabelDatas.Remove(delete_box); // Đánh dấu đối tượng để xóa
            int changes = await _context.SaveChangesAsync(); // Lưu thay đổi vào CSDL
            return changes > 0;
        }

        return false;
    }


}


