using FinanceApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

//Conectar infraestructura para IAutService
builder.Services.AddInfrastructure(builder.Configuration);

// Servicios básicos por ahora
// En pasos siguientes agregaremos JWT, EF Core, etc.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();