using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "WebUiCors";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(opt => opt.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<CurrentUserOptions>(builder.Configuration.GetSection("CurrentUser"));
builder.Services.AddSingleton<ICurrentUserResolver, SeededAdminCurrentUserResolver>();

builder.Services.AddHttpClient<IdentityClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Identity"]
        ?? throw new InvalidOperationException("Services:Identity not configured")));
builder.Services.AddHttpClient<TransactionsClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Transactions"]
        ?? throw new InvalidOperationException("Services:Transactions not configured")));
builder.Services.AddHttpClient<LedgerClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Ledger"]
        ?? throw new InvalidOperationException("Services:Ledger not configured")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Web.Api" }));

app.MapMeEndpoints();
app.MapTransactionsEndpoints();
app.MapReportsEndpoints();
app.MapCategorizationRulesEndpoints();

await app.RunAsync();
