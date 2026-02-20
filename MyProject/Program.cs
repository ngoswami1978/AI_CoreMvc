using MyProject.Repositories;
using MyProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ITableNameRepository, TableNameRepository>();
builder.Services.AddScoped<ITableNameService, TableNameService>();
builder.Services.AddAntiforgery(options => { options.HeaderName = "RequestVerificationToken"; });
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TableName}/{action=Index}/{id?}");

app.Run();
