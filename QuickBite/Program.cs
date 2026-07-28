using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Services;
using QuickBite.Services.Events;
using QuickBite.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AuthorizeFolder("/Staff");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AuthorizeFolder("/Admin/MenuItems", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin/Staff", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin/Reports", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin/Blacklist", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin/Orders", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Admin/Kitchen", "KitchenAccess");
    options.Conventions.AuthorizeFolder("/Admin/Shipper", "ShipperAccess");
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(nameof(AccountRole.Admin), nameof(AccountRole.Manager)));
    options.AddPolicy("StaffAccess", policy =>
        policy.RequireRole(
            nameof(AccountRole.Admin),
            nameof(AccountRole.Manager),
            nameof(AccountRole.Staff)));
    options.AddPolicy("KitchenAccess", policy =>
        policy.RequireRole(
            nameof(AccountRole.Admin),
            nameof(AccountRole.Manager),
            nameof(AccountRole.Kitchen)));
    options.AddPolicy("ShipperAccess", policy =>
        policy.RequireRole(
            nameof(AccountRole.Admin),
            nameof(AccountRole.Manager),
            nameof(AccountRole.Shipper)));
});
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<WorkSessionService>();
builder.Services.AddScoped<IDiscountService, NoDiscountService>();
builder.Services.AddScoped<ICustomerInfoService, DefaultCustomerInfoService>();
builder.Services.AddScoped<IOrderEventPublisher, OrderEventPublisher>();
builder.Services.Configure<OrderingOptions>(
    builder.Configuration.GetSection(OrderingOptions.SectionName));
builder.Services.AddHostedService<OrderExpiryService>();
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/AccessDenied";
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var workSessions = scope.ServiceProvider.GetRequiredService<WorkSessionService>();
    await workSessions.CloseAllOpenAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<OrderHub>("/orderHub");

app.MapRazorPages();

app.Run();
