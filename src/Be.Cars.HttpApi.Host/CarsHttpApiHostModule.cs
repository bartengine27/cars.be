using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Volo.Abp.PermissionManagement;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Be.Cars.EntityFrameworkCore;
using Be.Cars.MultiTenancy;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using Be.Cars.HealthChecks;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Be.Cars;

[DependsOn(
    typeof(CarsHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(AbpIdentityAspNetCoreModule),
    typeof(CarsApplicationModule),
    typeof(CarsEntityFrameworkCoreModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule)
    )]
public class CarsHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
        }

        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureAuthentication(context, configuration);
        ConfigureSwagger(context, configuration);
        ConfigureCache(configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        ConfigureDistributedLocking(context, configuration);
        ConfigureCors(context, configuration);
        ConfigureExternalProviders(context);
        ConfigureHealthChecks(context);

        Configure<PermissionManagementOptions>(options =>
        {
            options.IsDynamicPermissionStoreEnabled = true;
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddCarsHealthChecks();
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["Angular"].RootUrl = configuration["App:AngularUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
            options.Applications["Angular"].Urls[AccountUrlNames.EmailConfirmation] = "account/email-confirmation";
        });
    }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = "Cars:";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<CarsDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}Be.Cars.Domain.Shared", Path.DirectorySeparatorChar)));
                options.FileSets.ReplaceEmbeddedByPhysical<CarsDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}Be.Cars.Domain", Path.DirectorySeparatorChar)));
                options.FileSets.ReplaceEmbeddedByPhysical<CarsApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}Be.Cars.Application.Contracts", Path.DirectorySeparatorChar)));
                options.FileSets.ReplaceEmbeddedByPhysical<CarsApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}Be.Cars.Application", Path.DirectorySeparatorChar)));
                options.FileSets.ReplaceEmbeddedByPhysical<CarsHttpApiModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}Be.Cars.HttpApi", Path.DirectorySeparatorChar)));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(CarsApplicationModule).Assembly);
        });
    }

    /// <summary>
    /// Configures the authentication for the application.
    /// </summary>
    /// <param name="context">The service configuration context.</param>
    /// <param name="configuration">The application configuration.</param>
    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
             {
                 // Set the authority to the issuer's URL. This is used to discover the issuer's public key for validating token signatures.
                 options.Authority = configuration["AuthServer:Authority"];
                 // Require HTTPS for metadata address when this is enabled.
                 options.RequireHttpsMetadata = configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata");
                 // Set the valid audience for tokens. Tokens with other audiences will be rejected.
                 options.Audience = "Pointedwords";

                 // Configure the token validation parameters.
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     //TODO https://github.com/dotnet/aspnetcore/issues/52075 - ValidateIssuerSigningKey should be true and SignatureValidator should not be set
                     // Validate the signing key. This is necessary to ensure that the token was issued by the trusted issuer.
                     // The signing key must match! its settings are retrieved from the AuthServer link (options.Authority), check the metadata at options.Authority/.well-known/openid-configuration
                     // and look for "jwks_uri": "https://localhost:5001/.well-known/jwks", open the jwks_uri link and look for the "keys" array
                     ValidateIssuerSigningKey = false,
                     SignatureValidator = (token, _) => new JsonWebToken(token),
                     ValidateIssuer = false,
                     // Validate the audience. This ensures the token was issued for your application.
                     //the audience is Pointedwords as defined above
                     ValidateAudience = true,
                     // Validate the lifetime of the token (its "nbf" or not before and "exp" or expiration claims).                     
                     ValidateLifetime = true,
                     // Allow a certain amount of clock skew in token expiration. This helps to mitigate clock synchronization issues between servers.
                     //5 minute tolerance for the expiration date
                     ClockSkew = TimeSpan.FromSeconds(5 * 60)
                 };

             });
        // Enable dynamic claims. This allows the application to add and remove claims from user identities as necessary for authorization.
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    /// <summary>
    /// Configure the Swagger service with OIDC authentication:
    /// <para>
    /// <list type="bullet">
    /// <item>AbpSwaggerOidcFlows.AuthorizationCode: The "authorization_code" flow is the default and suggested flow.Doesn't require a client secret when even there is a field for it.</item>
    /// <item>AbpSwaggerOidcFlows.Implicit: The deprecated "implicit" flow that was used for javascript applications.</item>
    /// <item>AbpSwaggerOidcFlows.Password: The legacy password flow which is also known as Resource Ownder Password flow. You need to provide a user name, password and client secret for it.</item>
    /// <item>AbpSwaggerOidcFlows.ClientCredentials: The "client_credentials" flow that is used for server to server interactions.</item>
    /// </list>
    /// <see cref="AbpSwaggerOidcFlows.AuthorizationCode"/> and <see cref="AbpSwaggerOidcFlows.ClientCredentials"/> are used in this case.
    /// </para>
    /// <para>
    /// Only one scope is enabled: Cars
    /// </para>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <remarks>
    /// The Swagger UI only supports one flow, therefore, the "authorization_code" flow is commented out to ease testing.
    /// </remarks>
    private static void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOidc(
            configuration["AuthServer:Authority"],
            scopes: new[] { "Cars" },
            //TODO enable the "authorization_code" flow when the Swagger UI supports multiple flows, for deployment or make configurable
            flows: new[] { /*AbpSwaggerOidcFlows.AuthorizationCode,*/ AbpSwaggerOidcFlows.ClientCredentials },
            // When deployed on K8s, should be metadata URL of the reachable DNS over internet like https://myauthserver.company.com
            discoveryEndpoint: configuration["AuthServer:Authority"],
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Cars API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }

    /// <summary>
    /// <para>
    /// Antiforgery tokens are used to prevent Cross-Site Request Forgery (CSRF) attacks and are particularly relevant in applications 
    /// where form data is submitted. These tokens rely on the application's data protection APIs to encrypt and decrypt the tokens. 
    /// When running in a distributed environment, such as behind a load balancer with multiple application instances, all instances must 
    /// share the same data protection keys to successfully encrypt and decrypt tokens.
    /// </para>
    /// <para>
    /// As we are using Redis as a distributed cache, we can use it to store the data protection keys, also in Development as NGINX is used as 
    /// a load balancer in Development (at least in the Proxmox setup).
    /// </para>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <param name="hostingEnvironment"></param>
    /// <remarks>
    /// If you see an "The antiforgery token could not be decrypted." error, check Redis and usage of NGINX for load balancing.
    /// </remarks>
    private void ConfigureDataProtection(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("Cars");
        //if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "Cars-Protection-Keys");
        }
    }

    private void ConfigureDistributedLocking(
            ServiceConfigurationContext context,
            IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim().RemovePostFix("/"))
                            .ToArray() ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureExternalProviders(ServiceConfigurationContext context)
    {
        context.Services
            .AddDynamicExternalLoginProviderOptions<GoogleOptions>(
                GoogleDefaults.AuthenticationScheme,
                options =>
                {
                    options.WithProperty(x => x.ClientId);
                    options.WithProperty(x => x.ClientSecret, isSecret: true);
                }
            )
            .AddDynamicExternalLoginProviderOptions<MicrosoftAccountOptions>(
                MicrosoftAccountDefaults.AuthenticationScheme,
                options =>
                {
                    options.WithProperty(x => x.ClientId);
                    options.WithProperty(x => x.ClientSecret, isSecret: true);
                }
            )
            .AddDynamicExternalLoginProviderOptions<TwitterOptions>(
                TwitterDefaults.AuthenticationScheme,
                options =>
                {
                    options.WithProperty(x => x.ConsumerKey);
                    options.WithProperty(x => x.ConsumerSecret, isSecret: true);
                }
            );
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseStaticFiles();
        app.UseAbpSecurityHeaders();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cars API");

            var configuration = context.GetConfiguration();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}
