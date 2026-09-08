var builder = WebApplication.CreateBuilder(args);

// YARP will be configured in Phase 2
// builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Gateway" }));

app.Run();
