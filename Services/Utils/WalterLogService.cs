// Services/WalterLogService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Utils;

namespace SVN_Tools.Services.Utils
{
    public interface IWalterLogService
    {
        /// <summary>
        /// Lưu một log và trả về Id vừa tạo.
        /// </summary>
        Task<int> LogAsync(string content, CancellationToken ct = default);
    }
    public class WalterLogService : IWalterLogService
    {
        private readonly AppDbContext _context;
        public WalterLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> LogAsync(string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required.", nameof(content));

            var entity = new SVN_WALTER_END_LINE_LOG
            {
                // DateTime = DateTime.UtcNow,       // hoặc DateTime.Now nếu cần giờ local
                Content = content.Trim()
            };

            _context.SVN_WALTER_END_LINE_LOGs.Add(entity);
            await _context.SaveChangesAsync(ct);

            return entity.Id;
        }
    }
}