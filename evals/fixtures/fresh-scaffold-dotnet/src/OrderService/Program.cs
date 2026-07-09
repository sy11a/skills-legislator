var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "OrderService is running");
app.Run();
