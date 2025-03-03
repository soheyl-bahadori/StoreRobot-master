using Microsoft.EntityFrameworkCore;
using WebHookReceiver;
using WebHookReceiver.Models;
using WebHookReceiver.Services;

Console.SetOut(new System.IO.StreamWriter("consolelog.txt"));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.C:\consolelog.txt

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<WebHookDbContext>(w => w.UseSqlite("Data source=WebHook.db"));
//builder.Services.AddCronJob<CheckDigiOrdersCronJob>(c =>
//{
//    c.TimeZoneInfo = TimeZoneInfo.Local;
//    c.CronExpression = @"*/5 * * * *";
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
