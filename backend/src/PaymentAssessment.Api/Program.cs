using PaymentAssessment.Infrastructure.Payments.TwoC2P;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

var app = builder.Build();

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
