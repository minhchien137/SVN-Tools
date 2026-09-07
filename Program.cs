using Microsoft.EntityFrameworkCore;
using Quartz;
using SVN_Tools.Jobs;
using SVN_Tools.Services.Configurations;
using SVN_Tools.Services.Helpers;
using SVN_Tools.Services.Utils;


var builder = WebApplication.CreateBuilder(args);
// var connectionString = builder.Configuration.GetConnectionString("DBConfiguration");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd", policy =>
    {
        policy
            .WithOrigins("http://10.10.99.10:8102", "http://10.10.99.10:8100", "http://localhost:5072") // sửa theo thực tế
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

// Install: dotnet add package Quartz.AspNetCore

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("FqcDailyReportJob");

    q.AddJob<FqcDailyReportJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("FqcDailyReportJob-trigger")
        .WithCronSchedule("0 0 21 * * ?", x => x.InTimeZone(
            TimeZoneInfo.FindSystemTimeZoneById(
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Bangkok")))
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddHttpClient("viindoo", client =>
{
    client.BaseAddress = new Uri("http://10.10.99.10:8101/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});


AppConfig appConfig = builder.Configuration.GetSection("AppConfig").Get<AppConfig>();
DBConfiguration dBConfiguration = builder.Configuration.GetSection("DBConfiguration").Get<DBConfiguration>();
QCInfoConfig qCInfoConfig = builder.Configuration.GetSection("QCInfoConfig").Get<QCInfoConfig>();
OperInfoConfig operInfoConfig = builder.Configuration.GetSection("OperInfoConfig").Get<OperInfoConfig>();
APIConfiguration aPIConfiguration = builder.Configuration.GetSection("APIConfiguration").Get<APIConfiguration>();
TOASTLabelConfiguration labelConfiguration = builder.Configuration.GetSection("TOASTLabelConfiguration").Get<TOASTLabelConfiguration>();
dBConfiguration.ProductMode = appConfig.ProductMode;

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ProdConnectionString")));
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(dBConfiguration);
builder.Services.AddSingleton(qCInfoConfig);
builder.Services.AddSingleton(operInfoConfig);
builder.Services.AddSingleton(aPIConfiguration);
builder.Services.AddSingleton(labelConfiguration);
builder.Services.AddSingleton<ToolsHelper>();
builder.Services.AddScoped<LabelService>();
builder.Services.AddScoped<PrinterService>();
builder.Services.AddScoped<QRtoZPLService>();
builder.Services.AddScoped<LabelInfoService>();
builder.Services.AddScoped<WHLabelInfoService>();
builder.Services.AddScoped<IAstroLabelDataService, AstroLabelDataService>();
builder.Services.AddScoped<IVerifyEmployeeDataService, VerifyEmployeeDataService>();
builder.Services.AddScoped<IIotVerifyEmployeeDataService, IotVerifyEmployeeDataService>();
builder.Services.AddScoped<WalterLogService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UsePathBase("/tools");

app.Use((context, next) =>
{
    context.Request.PathBase = "/tools";
    return next();
});

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowFrontEnd");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
