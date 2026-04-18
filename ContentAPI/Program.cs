using ContentAPI.Extentions;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using OpenApi = Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var key = "Bearer";

        var scheme = new OpenApi.OpenApiSecurityScheme
        {
            Type = OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token here."
        };

        document.Components ??= new OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes[key] = scheme;

        document.SecurityRequirements.Add(new OpenApi.OpenApiSecurityRequirement
        {
            [new OpenApi.OpenApiSecurityScheme
            {
                Reference = new OpenApi.OpenApiReference
                {
                    Type = OpenApi.ReferenceType.SecurityScheme,
                    Id = key
                }
            }] = Array.Empty<string>()
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddScoped<ISavedContentService, SavedContentService>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
    };
});

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 100;
        config.QueueLimit = 0;
    });
});

builder.Services.AddHttpClient("ProxyApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7002");
});



var app = builder.Build();


// ---MIDDLEWARE CONFIGURATION-----

app.UseCustomExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
