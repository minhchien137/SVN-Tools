using Microsoft.EntityFrameworkCore;
using SVN_Tools.Models.Label;
using SVN_Tools.Models.Utils;
using SVN_Tools.Models.Verify;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AstroLabelData> AstroLabelDatas { get; set; }
    public DbSet<VerifyEmployeeData> VerifyEmployeeDatas { get; set; }
    public DbSet<IotVerifyEmployeeData> IotVerifyEmployeeDatas { get; set; }
    public DbSet<WHLabelInfo> WHLabelInfos { get; set; }

    public DbSet<PrinterInfo> PrinterInfos { get; set; }

    public DbSet<ProjectPasswordModel> ProjectPasswordModels { get; set; }

    public DbSet<SVN_WALTER_END_LINE_LOG> SVN_WALTER_END_LINE_LOGs { get; set; }

    public DbSet<SVNToastSerialInfo> SVNToastSerialInfos { get; set; }

    public DbSet<SVNToastScanRule> SVNToastScanRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SVNToastSerialInfo>()
            .HasKey(x => x.SerialNumber);
    }

}