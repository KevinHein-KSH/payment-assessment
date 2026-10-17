using PaymentAssessment.Infrastructure.Payments.TwoC2P;
using PaymentAssessment.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using PaymentAssessment.Application.Caching;
using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
var redis = builder.Configuration.GetConnectionString("Redis")!;
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect($"{redis},abortConnect=false"));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<LookupService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<TwoC2POptions>()
                .Bind(builder.Configuration.GetSection("TwoC2P"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

builder.Services.AddHttpClient<TwoC2PClient>((sp, client) =>
{
    var o = sp.GetRequiredService<IOptions<TwoC2POptions>>().Value;
    client.BaseAddress = new Uri(o.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
var mySql = builder.Configuration.GetConnectionString("MySql")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySql, new MySqlServerVersion(new Version(8, 4, 0))));

var app = builder.Build();
app.Services.ApplyMigrations();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
