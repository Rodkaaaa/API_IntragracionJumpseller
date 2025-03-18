using API_IntragracionJumpseller.EndPoints.Productos;
using API_IntragracionJumpseller.Mapping;
using DataAccess.Data.Productos;
using DataAccess.DbAccess;
using Microsoft.OpenApi.Models;

string MyCors = "_MyCorss";


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

var contactInfo = new OpenApiContact()
{
    Name = "Frederick Cid",
    Email = "fcid@andesindustrial.cl",
    Url = new Uri("https://github.com/Rodkaaaa")
};

var License = new OpenApiLicense()
{
    Name = "Warning Private"
};

var info = new OpenApiInfo()
{
    Version = "v1",
    Title = "API integracion multivende",
    Description = "",
    Contact = contactInfo,
    License = License
};


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyCors, builder => { builder.WithOrigins("*").WithMethods("GET", "POST", "DELETE").AllowAnyHeader(); });
});

// Instance Services Data Connection

builder.Services.AddSingleton<ISqlDataAccess, SqlDataAccess>();
builder.Services.AddSingleton<IProductosData, ProductosData>();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc("v1", info);
        //options.TagActionsBy(p => p.RelativePath.Contains("v1") ? p.RelativePath.Split('/')[2] : p.RelativePath);
    }
  );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add EndPoints
var app = builder.Build();

app.ConfigurarProductosEndpoint(builder.Configuration, builder.Configuration["AGSettings:versionApi"]);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.AddMappingDapper();
app.UseHttpsRedirection();
app.UseCors(MyCors);
app.UseRouting();
app.Run();
