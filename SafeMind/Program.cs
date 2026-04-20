using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<SafeMindDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSignalR();


builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SafeMindDbContext>()
    .AddUserValidator<CustomUserValidator>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";
});
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SafeMind.Services.BookService>();
builder.Services.AddScoped<SafeMind.Services.BookSessionService>();
builder.Services.AddScoped<SafeMind.Services.SlotsService>();
builder.Services.AddScoped<SafeMind.Services.ConfirmService>();
builder.Services.AddSingleton<SafeMind.Services.IDeterministicHasher, SafeMind.Services.DeterministicHasher>();
builder.Services.AddScoped<SafeMind.Services.MySessionService>();
builder.Services.AddScoped<SafeMind.Services.DiaryService>();
builder.Services.AddScoped<SafeMind.Services.ArticleService>();
builder.Services.AddScoped<SafeMind.Services.ChatService>();
builder.Services.AddScoped<SafeMind.Services.GoalService>();
builder.Services.AddScoped<SafeMind.Services.AdminService>();
builder.Services.AddScoped<SafeMind.Services.RatingService>();
builder.Services.AddHostedService<SafeMind.Services.SessionCleanupService>();
builder.Services.AddScoped<SafeMind.Services.EmailSender>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/";
    options.AccessDeniedPath = "/";
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.Redirect("/Identity/Account/Login");
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.Redirect("/Error/403");
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.MapHub<SafeMind.Hubs.ChatHub>("/chathub");

await SafeMind.Services.DbInitializer.SeedAsync(app.Services);

app.Run();
