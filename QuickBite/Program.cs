using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Services;
using QuickBite.Modules.Operations.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AuthorizeFolder("/Admin/Orders", InternalPolicies.ReceiveOrders);
    options.Conventions.AuthorizeFolder("/Staff", InternalPolicies.ReceiveOrders);
    options.Conventions.AuthorizeFolder("/Admin/MenuItems", InternalPolicies.ManagerOnly);
    options.Conventions.AuthorizeFolder("/Admin/Staff", InternalPolicies.ManagerOnly);
    options.Conventions.AuthorizeFolder("/Admin/Reports", InternalPolicies.ManagerOnly);
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(InternalPolicies.ManagerOnly, policy =>
        policy.RequireRole(InternalRoles.Manager));
    options.AddPolicy(InternalPolicies.ReceiveOrders, policy =>
        policy.RequireRole(InternalRoles.Manager, InternalRoles.Staff));
    options.AddPolicy(InternalPolicies.OperateKitchen, policy =>
        policy.RequireRole(InternalRoles.Manager, InternalRoles.Kitchen));
    options.AddPolicy(InternalPolicies.DeliverOrders, policy =>
        policy.RequireRole(InternalRoles.Manager, InternalRoles.Shipper));
});
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<WorkSessionService>();
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

// Dọn ca làm còn treo từ lần chạy trước: nếu server tắt/restart khi đang có người
// online, các WorkSession của họ vẫn CheckOutAt = null. Đóng hết trước khi nhận request.
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
