using System.Text;
using FreeBudget.Web.Api.Auth;
using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "WebUiCors";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(opt => opt.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
var authOptions = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
if (string.IsNullOrWhiteSpace(authOptions.JwtSigningKey))
    throw new InvalidOperationException(
        "Auth:JwtSigningKey is not configured. Provide a strong signing key via appsettings or environment.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITokenIssuer, LocalJwtTokenIssuer>();
builder.Services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.JwtIssuer,
            ValidAudience = authOptions.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Web.Api" }))
    .AllowAnonymous();

app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapUsersEndpoints();
app.MapTransactionsEndpoints();
app.MapReportsEndpoints();
app.MapCategorizationRulesEndpoints();
app.MapBankAccountsEndpoints();
app.MapGroupsEndpoints();
app.MapImportLayoutsEndpoints();
app.MapLedgerEndpoints();
app.MapSharingRulesEndpoints();

await app.RunAsync();
