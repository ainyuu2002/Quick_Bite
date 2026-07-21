using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Models;
using QuickBite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ConnectionTracker>();
// TODO (Dev C): thêm implementation OrderService/CartService vào Services/ rồi bỏ comment 2 dòng dưới
// builder.Services.AddScoped<OrderService>();
// builder.Services.AddScoped<CartService>();
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

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

app.UseAuthorization();

app.UseSession();

app.MapHub<OrderHub>("/orderHub");

// FR-10: order-track.js gọi sau khi reconnect để đồng bộ trạng thái mới nhất (SDS 3.4)
app.MapGet("/api/orders/{id:int}/status", async (int id, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    return order is null
        ? Results.NotFound()
        : Results.Json(new { orderId = order.Id, status = order.Status.ToString(), statusText = order.Status.ToDisplayText() });
});
app.MapRazorPages();

app.Run();
