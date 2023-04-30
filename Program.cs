using mchost.Server;
using mchost.Logging;

var headlessHost = new ServerHost();
ServerHost.SetServerHost(headlessHost);
var ccm = new mchost.CustomCommand.CustomCommandManager();
ccm.Execute(".help", "Cai1Hsu");
ccm.Execute(".set num 123", "Cai1Hsu");
ccm.Execute(".set home \"123,-999,123\"", "Cai1Hsu");
ccm.Execute(".set home a boy named Bob", "Cai1Hsu");
ccm.Execute(".unknown", "Cai1Hsu");
ccm.Execute(".aLineEndWith ", "Cai1Hsu");
return;

const bool TECHNICAL_PREVIEW = true;

Logger.Log("Welcome to mchost!");

var host = new ServerHost();

ServerHost.SetServerHost(host);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (TECHNICAL_PREVIEW || app.Environment.IsDevelopment())
{
    Logger.Log("Running in development mode");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
