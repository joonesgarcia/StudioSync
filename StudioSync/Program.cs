using StudioSync.Bookings;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// RFC 7807 Problem Details error responses (ADR-0002, item 8).
builder.Services.AddProblemDetails();

// Modules of the modular monolith (ADR-0001).
builder.Services.AddBookingsModule(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapBookingsEndpoints();

app.Run();