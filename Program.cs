using mchost.Server;
using mchost.Logging;

const bool TECHNICAL_PREVIEW = true;

Logger.Log("Welcome to mchost!");

var host = new ServerHost();

ServerHost.SetServerHost(host);

ServerHost.GetServerHost()?.StartServer("/usr/lib/jvm/java-17-openjdk/bin/java", "-server -Xmx1024M -Xms1024M -jar server.jar nogui");

bool quit = false;

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    quit = true;
};

SpinWait.SpinUntil(() => quit);
return;

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
