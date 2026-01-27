using Application;
using Application.Ports;
using BasketService.Api.Endpoints.Basket;
using CosmosDb;
using DiscountCodesApi;
using PricesApi;
using ProjectionWorker;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCosmosDb();
builder.Services.AddApplicationHandlers();
builder.Services.AddScoped<IPriceApi, FakePriceApi>();
builder.Services.AddScoped<IDiscountCodesApi, FakeDiscountCodesApi>();

builder.Services.AddHostedService<BasketProjectionWorker>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseHttpsRedirection();
app.MapBasketEndpoints();

app.Run();