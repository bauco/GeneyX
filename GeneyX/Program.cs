using GeneyX;
using GeneyX.Services;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container
builder.Services.AddLogging(); // Add logging service
// Ex 2 Question 1: Bind Crawling configuration from appsettings.json
builder.Services.Configure<CrawlingConfiguration>(builder.Configuration.GetSection("CrawlingConfiguration"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScoped<IPublicationsService, PublicationsService>();
builder.Services.AddSingleton<IPublicationRepository, PublicationRepository>();
builder.Services.AddHostedService<PubMedBackgroundService>(); // Register the background servicebuilder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

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
