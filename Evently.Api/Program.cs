using Evently.Api.Data;
using Evently.Api.Data.Mongo;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

var builder = WebApplication.CreateBuilder(args);

// MongoDB.Driver 3.x exige uma representação explícita para Guid
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Configuração do JWT
var security = SecuritySettings.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(security);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = security.JwtIssuer,
        ValidAudience = security.JwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(security.JwtKeyBytes)
    };
});

builder.Services.AddAuthorization();

// PostgreSQL EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MongoDB
builder.Services.AddSingleton<EventlyMongoContext>();

// Redis
builder.Services.AddSingleton<RedisService>();

// Outros serviços
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CouponService>();
builder.Services.AddScoped<PaymentWebhookService>();
builder.Services.AddScoped<PaymentConfirmationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Evently.Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";

app.UseCors(x =>
{
    if (app.Environment.IsDevelopment())
    {
        x.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader();
    }
    else
    {
        x.WithOrigins(frontendOrigin)
         .AllowAnyMethod()
         .AllowAnyHeader();
    }
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await Seeder.SeedAsync(app.Services);

app.Run();
