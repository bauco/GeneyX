using GeneyX;
using GeneyX.Services;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container
builder.Services.AddLogging(); // Add logging service
// Bind CrawlingOptions from appsettings.json
builder.Services.Configure<CrawlingOptions>(builder.Configuration.GetSection("CrawlingOptions"));
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
