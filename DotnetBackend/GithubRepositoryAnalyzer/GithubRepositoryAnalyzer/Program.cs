using System.Text.Json.Serialization;
using GithubRepositoryAnalyzer;
using GithubRepositoryAnalyzer.Config.Extensions;
using GithubRepositoryAnalyzer.Domain.Extensions;
using GithubRepositoryAnalyzer.Domain.Services;
using GithubRepositoryAnalyzer.Dto;
using GithubRepositoryAnalyzer.Kernel.Extensions;
using MassTransit;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCustomOptions(builder.Configuration);
builder.Services.AddDatabaseServices();
builder.Services.AddCors();
builder.Services.AddControllers()
    .AddJsonOptions(
        options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
builder.Services.AddServices();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost:5002"));

builder.Services.AddStackExchangeRedisCache(
    options =>
    {
        options.Configuration = "localhost:5002";
    });

builder.Services.AddRedisCacheStorage<SearchRepositoryResult>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(policyBuilder => policyBuilder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.UseRouting();

app.UseEndpoints(
    endpoints =>
    {
        endpoints.MapControllers();
    });

using var scope = app.Services.CreateScope();
var databaseMigrateService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrateService>();
await databaseMigrateService.MigrateAsync();

app.Run();