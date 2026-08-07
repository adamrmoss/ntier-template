using System.Text;
using NTierTemplate.Api.Auth;
using NTierTemplate.Application;
using NTierTemplate.Application.Ioc;
using NTierTemplate.Application.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

namespace NTierTemplate.Api;

/// <summary>
/// Application entry point; configures the web host and services.
/// </summary>
public class Program
{
    /// <summary>
    /// Build and run the NTierTemplate API web application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

        builder.Services.AddControllers();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>()
            ?? new AppOptions();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowFrontend",
                policy => policy
                    .WithOrigins(
                        "http://localhost:8240",
                        appOptions.FrontendBaseUrl.TrimEnd('/')
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IPrincipalContainer, HttpPrincipalContainer>();
        builder.Services.AddNTierTemplateApplication(builder.Configuration);
        builder.Services.AddScoped<ITokenService, TokenService>();

        var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        await NTierTemplateStartupExtensions.EnsureDefaultRolesAsync(app.Services);

        app.UseForwardedHeaders();
        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}
