var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços dos Controladores que criamos
builder.Services.AddControllers();

// Configura o Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilita a interface gráfica do Swagger no ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // É esta linha que gera a página visual
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();