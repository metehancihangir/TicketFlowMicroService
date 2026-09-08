var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuthService" }));

app.MapControllers();

app.Run();
