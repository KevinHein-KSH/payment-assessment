using Swashbuckle.AspNetCore.Filters;
using PaymentAssessment.Api.Swagger;
using PaymentAssessment.Infrastructure.Payments.TwoC2P;
using PaymentAssessment.Infrastructure.Persistence;
using PaymentAssessment.Infrastructure.Caching;
using PaymentAssessment.Application.Locking;
using PaymentAssessment.Infrastructure.Locking;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using PaymentAssessment.Application.Caching;
using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Application.Checkout;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Application.Payments;

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
builder.Services.AddSingleton<IDistributedLock, RedisDistributedLock>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<NotificationService>();

// shortcut: only the local Next.js dev origin is allowed; move to config if the frontend is deployed
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.ExampleFilters();
    options.OperationFilter<IdempotencyKeyExampleFilter>();
});
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>(); // request/response examples in Swagger/SwaggerExamples.cs
builder.Services.AddOptions<TwoC2POptions>()
                .Bind(builder.Configuration.GetSection("TwoC2P"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

builder.Services.AddHttpClient<IPaymentProvider, TwoC2PPaymentProvider>((sp, client) =>
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

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();


// Lets WebApplicationFactory<Program> in the test project reach the entry point
public partial class Program;
