# Be.Cars

## Contents

- [Be.Cars](#becars)
  - [Contents](#contents)
  - [About this solution](#about-this-solution)
    - [Pre-requirements](#pre-requirements)
    - [Authentication Server](#authentication-server)
      - [Client credentials flow](#client-credentials-flow)
    - [Cross-Site Request Forgery (CSRF) Protection](#cross-site-request-forgery-csrf-protection)
      - [Cross-Site Request Forgery (CSRF)](#cross-site-request-forgery-csrf)
      - [REDIS to Store Protection Keys in a Distributed Architecture](#redis-to-store-protection-keys-in-a-distributed-architecture)
    - [OpenTelemetry](#opentelemetry)
    - [Configurations](#configurations)
    - [Before running the application](#before-running-the-application)
      - [Generating a Signing Certificate](#generating-a-signing-certificate)
      - [Install Client-Side Libraries](#install-client-side-libraries)
      - [Create the Database](#create-the-database)
    - [Running the application](#running-the-application)
    - [Solution structure](#solution-structure)
  - [Deploying the application](#deploying-the-application)
    - [Additional resources](#additional-resources)

## About this solution

This is a layered startup solution based on [Domain Driven Design (DDD)](https://docs.abp.io/en/abp/latest/Domain-Driven-Design) practises. All the fundamental ABP modules are already installed. Check the [Application Startup Template](https://docs.abp.io/en/commercial/latest/startup-templates/application/index) documentation for more info.

### Pre-requirements

- [.NET 8.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
- [Node v18 or 20](https://nodejs.org/en)
- [Redis](https://redis.io/)

### Authentication Server

This solution is integrated with the `OpenID Connect` and `OAuth 2.0` [OpenIddict](https://documentation.openiddict.com/) authentication server, 
the [ABP](https://abp.io/) default.

`OpenIddict` serves as a versatile open-source framework for implementing `OpenID Connect` and `OAuth 2.0` in .NET or .NET Core applications. Its design is aimed at simplifying 
the complex processes involved in authentication and authorization, providing developers with a robust, extensible, and compliant solution. When integrated into 
the ABP.io framework � a modern web development platform that leverages the ASP.NET Core framework � OpenIddict not only enhances the security of applications 
but also ensures a streamlined implementation of industry-standard protocols for authenticating users and securing APIs.

For developers, the importance of secure, reliable, and scalable authentication mechanisms cannot be overstated. In today's 
digital landscape, where security breaches and data privacy concerns are rampant, leveraging a framework like `OpenIddict` within [ABP.io](https://apb.io) applications offers a 
formidable defense mechanism. It enables the development of applications that not only safeguard sensitive information but also provide a seamless user experience.

`OpenIddict`'s integration with `ABP.io` is particularly appealing for several reasons. Firstly, it abstracts much of the complexity associated with directly 
implementing `OpenID Connect` and `OAuth 2.0` protocols. Developers can thus focus more on the business logic and user experience aspects of their applications, 
rather than the intricate details of authentication flows. Secondly, `OpenIddict`'s flexibility allows for customization to meet specific security requirements, 
whether it's token expiration, scope management, or encryption methods. This adaptability is crucial for businesses with unique security needs.

Furthermore, the ABP.io framework's modular architecture complements `OpenIddict`'s capabilities, making it straightforward to add or modify authentication 
flows as application requirements evolve. ABP.io also provides additional layers of abstraction and pre-built modules that can accelerate development timelines, 
such as user management, role management, and multi-tenancy support, which are essential features for modern web applications.

For .NET Core developers, the synergy between `OpenIddict` and [ABP.io](https://abp.io) means an end-to-end solution for building secure, modern web applications and APIs. This 
combination not only addresses the technical demands of authentication and authorization but also aligns with best practices in software architecture, such as 
the separation of concerns, DRY (Don't Repeat Yourself) principles, and code modularity.

In the following we'll cover the setup and configuration steps for a client credentials flow using `OpenIddict` within the ABP.io framework as 
autheticating users is taken care off by default. 

#### Client credentials flow

#### Configure the server

This section will describes the setup of OpenIddict within an ABP.io application to secure an API using the Client Credentials flow. The Client Credentials grant is ideally suited for server-to-server communication, where an application acts on its own behalf rather than on behalf of an individual user. 

Integrating OpenIddict with ABP.io to facilitate the Client Credentials flow involves a series of configuration steps and customizations within the `Be.Cars.AuthServer` and `Be.Cars.Domain` project. 

First, ensure that your ABP.io project is set up and running: you can create a new ABP.io project using the ABP CLI, [see](https://abp.io/get-started). After successfully 
creating the project, you can proceed with the following steps to configure OpenIddict for the Client Credentials flow:

* Configure the OpenIddict Server in the `Be.Cars.AuthServer` project:

  Enable the Client Credentials flow in the `Be.Cars.AuthServer` project by adding the following code to the `PreConfigureServices` method in the 'Be.Cars.AuthServer.CarsAuthServerModule.cs` file:

```csharp
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();
        // https://docs.abp.io/en/abp/latest/Modules/OpenIddict
        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddServer(builder =>
            {

                builder.SetAuthorizationEndpointUris("/connect/authorize")
                .SetTokenEndpointUris("/connect/token")
                .SetUserinfoEndpointUris("/connect/userinfo")
                .AllowAuthorizationCodeFlow()
                .AllowRefreshTokenFlow()
                .AllowClientCredentialsFlow()
                // Note the password here and the password in the second part of the PreConfigureServices function call,
                // both have to be equal.
                .AddSigningCertificate(new X509Certificate2("openiddict.pfx", "d7fdd187-b031-48b1-bf66-2a89d8180917")); 
                //.DisableAccessTokenEncryption();
            });
            // Registers the OpenIddict token validation services in the DI container.
            builder.AddValidation(options =>
            {
                options.AddAudiences("Cars");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });
```

* Configure the Authentication Middleware (enabled by default in all relevant projects, i.e. `Be.Cars.AuthServer`, `Be.Cars.Blazor` and `Be.Cars.HttpApi.Host`)
* Configure the OpenIddict Server database in the `Be.Cars.Domain` project:

  With the server set up, you now need to register a client that will communicate with the API using the Client Credentials flow. This typically involves adding an 
  entry to table `[Cars].[dbo].[OpenIddictApplications]` in our ABP.io database. For a client credential flow, you will have to set up a client ID and a secret by 
  adding the following code in class `OpenIddictDataSeedContributor` in the `Be.Cars.Domain` project:

    ```csharp
    // Swagger Client
    var swaggerClientId = configurationSection["Cars_Swagger:ClientId"];
    if (!swaggerClientId.IsNullOrWhiteSpace())
    {
        var swaggerRootUrl = configurationSection["Cars_Swagger:RootUrl"]?.TrimEnd('/');

        await CreateApplicationAsync(
            name: swaggerClientId!,
            type: OpenIddictConstants.ClientTypes.Confidential,
            consentType: OpenIddictConstants.ConsentTypes.Implicit,
            displayName: "Swagger Application",
            secret: configurationSection["Cars_Swagger:ClientSecret"] ?? "1q2w3e*",
            grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.ClientCredentials, },
            scopes: commonScopes,
            redirectUri: $"{swaggerRootUrl}/swagger/oauth2-redirect.html",
            clientUri: swaggerRootUrl,
        );
    }
    ```

  In the code snippet above, the `CreateApplicationAsync` method is used to create a new client application with the specified parameters. The `type` parameter
  is set to `OpenIddictConstants.ClientTypes.Confidential` to indicate that the client is a confidential application. A confidential (OpenIddict) client:

  * Can Securely Store Secrets:

    Confidential clients are capable of securely storing a client secret. This is typically because they run in an environment where unauthorized access 
    to the client secret can be effectively restricted. Examples include server-side applications, where the secret is stored on the server.

  * Client Authentication:
  
    Due to their ability to securely store secrets, confidential clients authenticate to the authorization server using the client secret 
    (or other means like client assertions). This is used not only for the client credentials grant but also for other flows where client 
    authentication is required, such as the authorization code flow with a secret.

  * Use Cases: 
  
    Server-side web applications, backend services, and applications running on secure, trusted servers are considered confidential clients.

  Public clients, on the other hand,

  * Cannot Securely Store Secrets:

    Public clients run in environments where the confidentiality of information (like a client secret) cannot be guaranteed. 
    This typically includes clients running on the user's device, such as native mobile apps, desktop applications, and single-page web applications (SPAs).

  * Client Authentication:
  
    Because public clients cannot securely hold secrets, they do not authenticate to the authorization server using a client secret. Instead, public clients rely on other means for authorization flows, like using the "Proof Key for Code Exchange" (PKCE) enhancement with the authorization code flow for SPAs and mobile apps.
  
  * Use Cases:
  
    Mobile applications, desktop applications, and JavaScript web applications running in the browser are examples of public clients.

  The OAuth 2.0 and OpenID Connect specifications make this distinction to ensure that different types of clients use the most appropriate 
  and secure method for their environment:

  * Security:
  
    It prevents exposing secrets in environments where they cannot be protected effectively. This is crucial for maintaining the security integrity of the OAuth/OIDC ecosystem.

  * Adaptability:
  
    It allows the OAuth/OIDC framework to adapt to a wide range of application types and deployment scenarios by providing appropriate mechanisms for each type of client.

  In practice, when you encounter the requirement that "The 'client_secret' or 'client_assertion' parameter must be specified when using the client credentials grant,
  it implies that the authorization server (OpenIddict) expects the client to authenticate itself as a confidential client.

  * For a public client:
  
    You wouldn�t normally use the client credentials flow because it requires a client secret for authentication. Public clients typically use flows designed for 
    environments where secrets cannot be securely stored, such as the authorization code flow with PKCE.

  * For a confidential client:

    You must include the client_secret in requests to the token endpoint when using the client credentials grant (or other flows where client authentication is necessary), 
    as the server expects a form of client authentication that relies on the ability to securely store this secret.

  The `grantTypes` parameter specifies the `OpenIddictConstants.GrantTypes.AuthorizationCode` and `OpenIddictConstants.GrantTypes.ClientCredentials` grant types.

After executing the above steps, you should have a working OpenIddict server that supports the Client Credentials flow in your ABP.io application. You can test your setup by
executing the following command:

```bash
curl -X POST https://your_ip:your_port/connect/token \
  -k \
  -d "client_id=Cars_Swagger" \
  -d "client_secret=1q2w3e*" \
  -d "grant_type=client_credentials" \
  -d "scope=Cars"
```

The command above sends a POST request to the token endpoint of the OpenIddict server, providing the client ID, client secret, grant type, and scope as parameters. A typical 
response to this request will include an access token that can be used to authenticate requests to the API:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IkJCODNDOTI3QkJEOUQ4NjI3QUMxQ0QxNjAwNUFERDMzMUJEMjMzNDUiLCJ4NXQiOiJ1NFBKSjd2WjJHSjZ3YzBXQUZyZE14dlNNMFUiLCJ0eXAiOiJhdCtqd3QifQ.eyJvaV9wcnN0IjoiQ2Fyc19Td2FnZ2VyIiwiY2xpZW50X2lkIjoiQ2Fyc19Td2FnZ2VyIiwib2lfdGtuX2lkIjoiZGIyZTA4NjItNGQ0NC02MWJmLWZjMDAtM2ExMTkyNDNjM2MzIiwiYXVkIjoiQ2FycyIsInNjb3BlIjoiQ2FycyIsImp0aSI6Ijc3N2NlMTVlLTZhZDYtNDc4NC05NmNlLTU2NzllNGNkMmNjNCIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjQ0MzM5LyIsImV4cCI6MTcxMTU0OTU2OSwiaWF0IjoxNzExNTQ1OTY5fQ.qufPkcTlYKBa5pEk-QjwVpqdRtgwyRk-Ord-tLdRq6MkGySNKR1wLK-R1dp7ykRyVQoDl7SyjNNXHfekiuPbMTM7P8GBtHtNC5JMHdWE-42p0V28vxVqwPJzsKdM_OW8eggCBTTMLRJmG5eemffxH-IWk8SD9FFehR4CIWdKc5e35kYNzJtTcmCy18Jfv7HYUSUGQcjq-94m9nqJIaKNalVbvpKN_B_xWoS-n_VfMeKxjoyjOCRtL5zw5HGzdEHSQ6OBCG0XHbXPRQY2v6ZNXIveJOQb7fykbANZ60GVuBT6v4ilckUWKw-Y-K00zsoV5AcYFZTtuLEljvfqtX4vBw",
  "token_type": "Bearer",
  "expires_in": 3599
}
```

You can decode the `access_token` above using a JWT decoder to view the token's claims and verify its contents. For example the decoder [at](https://jwt.io/) can be used to decode the token:

```json
//header
{
  "alg": "RS256",
  "kid": "BB83C927BBD9D8627AC1CD16005ADD331BD23345",
  "x5t": "u4PJJ7vZ2GJ6wc0WAFrdMxvSM0U",
  "typ": "at+jwt"
}
//payload
{
  "oi_prst": "Cars_Swagger",
  "client_id": "Cars_Swagger",
  "oi_tkn_id": "db2e0862-4d44-61bf-fc00-3a119243c3c3",
  "aud": "Cars",
  "scope": "Cars",
  "jti": "777ce15e-6ad6-4784-96ce-5679e4cd2cc4",
  "iss": "https://localhost:44339/",
  "exp": 1711549569,
  "iat": 1711545969
}
```

With this access token, you can now call the API endpoints that require authentication. The token should be included in the `Authorization` header of the request:

```bash
curl -v -k -X 'GET'   'https://rest_api_ip_address:rest_api_port/api/identity/users'   -H 'accept: text/plain'   -H 'Authorization: Bearer ey...'
```

despite the successfull token generation, **the token is not yet used to authenticate the request**. This is because the `Be.Cars.HttpApi.Host` project (or client) is not yet configured to use the token.  

For integrating the client credentials grant type, particularly for the Swagger client in the `Be.Cars.HttpApi.Host` project, and specifying permissions for default Swagger calls such as Profile, Permissions, Role, Tenant, etc., you should be aware that ABP.io uses a permission - based authorization system. The exact permissions you need to grant depend on the actions you want to allow through the client credentials grant.

The client credentials grant type is typically used by clients(applications) to access resources about themselves rather than to access a user's resources. Therefore, you'd grant permissions that are appropriate for an application rather than a user. Here’s a general approach on how to assign permissions for services like Profile, Permissions, Role, Tenant, etc.:

* **Identify Permission Names**: Look into the ABP.io documentation, source code or ABP database to find the exact permission names. Permissions in ABP are usually defined in a [AbpPermissionGrants] table (for common functionalities like managing roles, tenants, or users, ABP modules define standard permissions).
* **Common Permissions**: For the default ABP.io calls (Profile, Permissions, etc.), we need permissions similar to the following (note that the exact permission names might vary based on your application setup and ABP version) :
    * **Profile**: Permissions related to user profile might not be accessible via client credentials since they typically require a user context. However, if you have services that allow profile updates without a user context, you’d look for permissions like:
      * `AbpIdentity.Users`
      * `AbpIdentity.Users.Create`
      * `AbpIdentity.Users.Delete`      
      * `AbpIdentity.Users.Update`
      * `AbpIdentity.Users.Update.ManageRoles`
    * **Permissions**: Managing permissions typically requires permissions like: 
      * `AbpIdentity.Roles.ManagePermissions`
      * `AbpIdentity.Users.ManagePermissions`
    * **Role**: For role management, you might need:
      * `AbpIdentity.Roles`
      * `AbpIdentity.Roles.Create`
      * `AbpIdentity.Roles.Delete`
      * `AbpIdentity.Roles.Update`
    * **Tenant**: For multi-tenancy support, permissions like 
      * `AbpTenantManagement.Tenants`
      * `AbpTenantManagement.Tenants.Create`
      * `AbpTenantManagement.Tenants.Delete`
      * `AbpTenantManagement.Tenants.ManageConnectionStrings`
      * `AbpTenantManagement.Tenants.ManageFeatures`
      * `AbpTenantManagement.Tenants.Update`
    * ...
    *
    
In your `OpenIddictDataSeedContributor` class in project `Be.Cars.Domain` , add or edit the following *Swagger Client* application configuration code:

```csharp
// Swagger Client
var swaggerClientId = configurationSection["Cars_Swagger:ClientId"];
if (!swaggerClientId.IsNullOrWhiteSpace())
{
    var swaggerRootUrl = configurationSection["Cars_Swagger:RootUrl"]?.TrimEnd('/');

    await CreateApplicationAsync(
        name: swaggerClientId!,
        type: OpenIddictConstants.ClientTypes.Confidential,
        consentType: OpenIddictConstants.ConsentTypes.Implicit,
        displayName: "Swagger Application",
        secret: configurationSection["Cars_Swagger:ClientSecret"] ?? "1q2w3e*",
        grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.ClientCredentials, },
        scopes: commonScopes,
        redirectUri: $"{swaggerRootUrl}/swagger/oauth2-redirect.html",
        clientUri: swaggerRootUrl,
        permissions: new List<string>
        {
            // taken form table [Pointedwords].[dbo].[AbpPermissionGrants] which is initialized for admin with all appropriate permissions
            //https://docs.abp.io/en/commercial/latest/startup-templates/microservice/synchronous-interservice-communication
            "AbpIdentity.Roles",
            "AbpIdentity.Roles.Create",
            "AbpIdentity.Roles.Delete",
            "AbpIdentity.Roles.ManagePermissions",
            "AbpIdentity.Roles.Update",
            "AbpIdentity.Users",
            "AbpIdentity.Users.Create",
            "AbpIdentity.Users.Delete",
            "AbpIdentity.Users.ManagePermissions",
            "AbpIdentity.Users.Update",
            "AbpIdentity.Users.Update.ManageRoles",
            "AbpTenantManagement.Tenants",
            "AbpTenantManagement.Tenants.Create",
            "AbpTenantManagement.Tenants.Delete",
            "AbpTenantManagement.Tenants.ManageConnectionStrings",
            "AbpTenantManagement.Tenants.ManageFeatures",
            "AbpTenantManagement.Tenants.Update",
            "FeatureManagement.ManageHostFeatures",
            "SettingManagement.Emailing",
            "SettingManagement.Emailing.Test",
            "SettingManagement.TimeZone",
        }
    );            
}
```

#### Configure the client 

In your client configuration, you will specify these permissions as scopes, in class `CarsHttpApiHostModule` project `Be.Cars.HttpApi.Host` :

```csharp
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
/// Only one scope is enabled: Pointedwords
/// </para>
/// </summary>
/// <param name="context"></param>
/// <param name="configuration"></param>
private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
{
    context.Services.AddAbpSwaggerGenWithOidc(
        configuration["AuthServer:Authority"],
        scopes: new[] { "Pointedwords" },            
        // "authorization_code"
        flows: new[] { /*AbpSwaggerOidcFlows.AuthorizationCode,*/ AbpSwaggerOidcFlows.ClientCredentials },
        // When deployed on K8s, should be metadata URL of the reachable DNS over internet like https://myauthserver.company.com
        discoveryEndpoint: configuration["AuthServer:Authority"],
        options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Pointedwords API", Version = "v1" });
            options.DocInclusionPredicate((docName, description) => true);
            options.CustomSchemaIds(type => type.FullName);
        }
    );
}
```

besides specifying the scope and supported scope, you probably also want to check tokens for its validity. This can be done by adding the following code to the `ConfigureServices` method in the `Be.Cars.HttpApi.Host` project:

```csharp
/// <summary>
/// Configures the authentication for the application.
/// </summary>
/// <param name="context">The service configuration context.</param>
/// <param name="configuration">The application configuration.</param>
private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
{
    context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
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
                    // Validate the signing key. This is necessary to ensure that the token was issued by the trusted issuer.
                    // The signing key must match! its settings are retrieved from the AuthServer link (options.Authority), check the metadata at options.Authority/.well-known/openid-configuration
                    // and look for "jwks_uri": "https://localhost:5001/.well-known/jwks", open the jwks_uri link and look for the "keys" array
                    ValidateIssuerSigningKey = true,
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
```

:fire: Due to a bug - at least, that's what it seems to be, [validation does not work](https://github.com/dotnet/aspnetcore/issues/52075) if no explicit key is given. As we don't want to explicitely configure a key, we disable the issuer validation. Hopefully, this will be fixed in the future, i.e. getting the key automatically from the authentication server.

For now, use the following code:

```csharp
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
```

#### Authorization code flow

For completeness, you can also test your authorization flow by executing the following command:

```bash
curl -X POST https://your_ip:your_port/connect/token \
  -k \
  -d "client_id=Cars_Swagger" \
  -d "client_secret=1q2w3e*" \
  -d "username=admin" \
  -d "password=1q2w3E*" \
  -d "grant_type=password" \
  -d "scope=Cars"
```

### Cross-Site Request Forgery (CSRF) Protection

In a typical setup of a load balanced REST API:

```mermaid
flowchart TD;        
    NGINX--load balances/offloads https-->RESTAPI1[REST API Cars node1];
    NGINX--load balances/offloads https-->RESTAPI2[REST API Cars node2];
    NGINX--load balances/offloads https-->RESTAPI3[REST API Cars node2];

    RESTAPI1[REST API Cars node1]--data protection-->Redis;
    RESTAPI2[REST API Cars node2]--data protection-->Redis;
    RESTAPI3[REST API Cars node2]--data protection-->Redis;

    Client--https-->NGINX
```

you may or may not use sticky sessions to handle the CSRF token. If you do not use sticky sessions, 
you may encounter the following error:

```json
{
  "code": 400,
  "message": "The anti-forgery cookie token and form field token do not match."
}
```

As the error message suggests, the anti-forgery cookie token and form field token do not match. This is because the client is sending the request to a different node. Therefore, as in the drawing above, use REDIS to store the data protection keys, i.e. share state between the different REST API nodes.

#### Cross-Site Request Forgery (CSRF)

Cross-Site Request Forgery (CSRF) is a type of security threat where an attacker tricks a user into executing unwanted actions on a web application in which they're authenticated. If the victim is a regular user, a successful CSRF attack can force them to perform state-changing requests like transferring funds, changing their email address, and so forth. If the victim has an administrative account, CSRF can compromise the entire web application. CSRF exploits the trust that a site has in the user's browser, and unlike Cross-Site Scripting (XSS), which exploits the trust a user has in a particular site, CSRF exploits the trust that a site has in the user's browser.

#### REDIS to Store Protection Keys in a Distributed Architecture

In a distributed web application architecture, particularly one that scales horizontally, requests from the same user can be routed to different servers across multiple requests due to load balancing. This poses a challenge for CSRF protection mechanisms that rely on keeping track of state, such as synchronizer tokens 
or double submit cookies, because the server handling a subsequent request might not have access to the tokens generated by another server on a previous request.

REDIS a fast in-memory data store that supports high availability and data persistence, offers an out of the box solution for storing CSRF protection keys in a distributed setup:

- Centralized Token Store: REDIS can act as a centralized store for CSRF tokens, making them accessible across all servers in the distributed environment. This ensures that any server can validate the CSRF token included in a subsequent request, regardless of which server generated the token.
- Performance: REDIS is designed for high performance, with operations typically completing in sub-millisecond times minimizing the impact on the overall response time of web applications.
- Scalability: As the web application grows, REDIS scales horizontally and vertically with minimal effort, ensuring that the CSRF protection mechanism can support increased load without becoming a bottleneck.
- Persistence and Reliability: REDIS offers various persistence options, ensuring that CSRF tokens and other session data are not lost even in the event of a server restart or failure. This persistence allows a more consistent user experience.
- Simplicity and Ease of Integration: Integrating REDIS as a session store or for CSRF token storage is straightforward with ABP.io. This ease of integration, combined with its wide support and robust client libraries, makes REDIS an almost obvious choice for our particular setup.

Using REDIS to store CSRF protection keys allows distributed web applications to maintain a high level of security against CSRF attacks while also ensuring scalability, performance, and reliability. This approach effectively addresses the challenges posed by the stateless nature of HTTP and the distributed architecture of modern web applications, providing a secure, user-friendly experience.

As mentioned earlier, the ABP.io framework provides built-in support for REDIS as a session store, making it very easy to enable REDIS as the storage mechanism for CSRF protection keys:

```csharp
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
```

In the code snippet above, the `ConfigureDataProtection` method is used to configure the data protection mechanism in the ABP.io application. 
The `AddDataProtection` method is used to add data protection services to the DI container. 

The `SetApplicationName` method is used to set the application name to "Cars". This name is used to isolate the keys generated by the data protection system from other applications.

:fire: Note that we disabled the

```csharp
if (!hostingEnvironment.IsDevelopment())
```

check to ensure that the REDIS store is used in all environments. This is not only because the CSRF protection mechanism should be consistent across all environments to ensure that the application is secure but also because `Proxmox` allows to easily setup several REST API nodes in a distributed manner, i.e. a production-like environment, [see](https://github.com/bartengine27/hostr).

:fire: On Windows you can replace [REDIS](https://redis.io/) with [MEMURAI](https://www.memurai.com/) or [Garnet](https://github.com/microsoft/garnet).  

### OpenTelemetry

OpenTelemetry is an open-source project that aims to provide a unified and vendor-agnostic instrumentation for collecting telemetry data (metrics, logs, and traces). OpenTelemetry helps enhancing the observability and operational efficiency of dotnet core applications, in particular ABP.io.

OpenTelemetry facilitates detailed monitoring and tracing of .NET Core web applications, allowing developers and operations teams to gain insights into the application's performance and behavior. This is particularly crucial for microservices architectures, like those often developed using ABP.io, where the complexity of interactions between services can make diagnosing issues challenging. By integrating OpenTelemetry, teams can **trace requests as they flow through the application and its services**, identifying bottlenecks, dependencies, and issues with ease.

The vendor-agnostic nature of OpenTelemetry means that it supports integration with a wide range of monitoring, logging, and tracing tools. This flexibility allows developers to choose the best tools for their specific needs without being locked into a single vendor. For ABP.io applications, which may leverage a variety of third-party services and databases, this means that developers can easily aggregate and correlate telemetry data across all components of the application ecosystem. As a result, teams can implement a comprehensive observability strategy that covers the entire stack, from front-end to back-end, regardless of the underlying technologies or platforms.

As an example of the integration support offered by OpenTelemetry, Prometheus/Grafana and Telegraf/InfluxDB/... are combined with OpenTelemetry and ABP in [this example web site](https://github.com/bartengine27/hostr). Prometheus, with its efficient time-series data storage and powerful query language, excels at collecting and storing metrics at scale. It integrates with OpenTelemetry to ingest a wide range of telemetry data. This integration allows for detailed monitoring of application metrics, including request rates, error rates, and system resource usage, among others. Grafana, on the other hand, provides a versatile visualization layer that connects with Prometheus to create intuitive and interactive dashboards. These dashboards enable developers and operations teams to visually analyze the metrics, identify trends, and spot anomalies in real-time. The combination of Prometheus for metric collection, OpenTelemetry for comprehensive telemetry data gathering, and Grafana for data visualization creates a cohesive observability setup. The Telegraf/InfluxDB/... setup at [this example web site](https://github.com/bartengine27/hostr) is an extra alternative illustrating the integration support offered by OpenTelemetry.  

You can add OpenTelemetry support in your `Be.Cars.HttpApi.Host` project by adding OpenTelemetry support in the `Program.cs` file (relevant lines are marked with `//add telemetry`):

```csharp
public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string otlpEndpoint = builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317");
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            //add telemetry
            .WriteTo.OpenTelemetry(otlpOptions =>
            {
                otlpOptions.Endpoint = otlpEndpoint;
                otlpOptions.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
            }
            )
            .CreateLogger();

        try
        {
            Log.Information("Starting Be.Cars.HttpApi.Host.");
            //add forward headers middleware, see https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-8.0
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<CarsHttpApiHostModule>();

            //add telemetry
            //https://community.abp.io/posts/asp.net-core-metrics-with-.net-8.0-1xnw1apc
            //https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics?view=aspnetcore-8.0
            //https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md
            builder.Services.AddSingleton<CustomMetrics>();
            builder.Services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                builder.AddMeter("Microsoft.AspNetCore.Hosting");
                //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                builder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                builder.AddMeter(CustomMetrics.Name);
                builder.AddView("http.server.request.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                    });                
                //TODO https://opentelemetry.io/docs/collector/
                //builder.AddOtlpExporter();
                builder.AddPrometheusExporter();
                builder.AddConsoleExporter();
            }
            );

            var app = builder.Build();
            app.UseForwardedHeaders();
            //add telemetry
            app.MapPrometheusScrapingEndpoint();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

Typical dependencies that you will need to install:

- AspNetCore.HealthChecks.UI
- AspNetCore.HealthChecks.UI.Client
- Microsoft.EntityFrameworkCore.InMemory
- AspNetCore.HealthChecks.UI.InMemory.Storage
- OpenTelemetry.Exporter.Console
- OpenTelemetry.Exporter.OpenTelemetryProtocol
- OpenTelemetry.Extensions.Hosting
- OpenTelemetry.Instrumentation.AspNetCore
- Serilog.Sinks.OpenTelemetry
- SharpAbp.Abp.OpenTelemetry.Exporter.Prometheus.AspNetCore

and in a similar vein for `Be.Cars.Blazor` in `Program.cs`:

```csharp
public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting web host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<CarsBlazorModule>();

            //add telemetry
            //https://community.abp.io/posts/asp.net-core-metrics-with-.net-8.0-1xnw1apc
            //https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics?view=aspnetcore-8.0
            //https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md
            builder.Services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder.AddAspNetCoreInstrumentation();
                    //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                    builder.AddMeter("Microsoft.AspNetCore.Hosting");
                    //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                    builder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                    builder.AddView("http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                        });
                    builder.AddPrometheusExporter();
                    builder.AddConsoleExporter();
                });

            var app = builder.Build();
            //add telemetry
            app.MapPrometheusScrapingEndpoint();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

:fire: The Blazor OpenTelemetry config is somewhat simplified comparted to the *REST endpoint*.  

### Configurations

The solution comes with a default configuration that works out of the box. However, you may consider to change the following configuration before running your solution:

- Check the `ConnectionStrings` in `appsettings.json` files under the `Be.Cars.AuthServer`, `Be.Cars.HttpApi.Host` and `Be.Cars.DbMigrator` projects and change it if you need.

  For example:
  
  ```json
    "ConnectionStrings": {
    "Default": "Server=your_machine_name;Database=Cars;Trusted_Connection=True;TrustServerCertificate=True"
  }
  ```

### Before running the application

#### Generating a Signing Certificate

In the production environment, you need to use a production signing certificate. ABP Framework sets up signing and encryption certificates in your application and expects an `openiddict.pfx` file in your application.

This certificate is already generated by ABP CLI, so most of the time you don't need to generate it yourself. However, if you need to generate a certificate, you can use the following command:

```bash
dotnet dev-certs https -v -ep openiddict.pfx -p d7fdd187-b031-48b1-bf66-2a89d8180917
```

> `d7fdd187-b031-48b1-bf66-2a89d8180917` is the password of the certificate, you can change it to any password you want.

It is recommended to use **two** RSA certificates, distinct from the certificate(s) used for HTTPS: one for encryption, one for signing.

For more information, please refer to: https://documentation.openiddict.com/configuration/encryption-and-signing-credentials.html#registering-a-certificate-recommended-for-production-ready-scenarios

> Also, see the [Configuring OpenIddict](https://docs.abp.io/en/abp/latest/Deployment/Configuring-OpenIddict#production-environment) documentation for more information.

#### Install Client-Side Libraries

Run the following command in the directory of your final application:

```bash
abp install-libs
```

> This command installs all NPM packages for MVC/Razor Pages and Blazor Server UIs and this command is already run by the ABP CLI, so most of the time you don't need to run this command manually.

#### Create the Database

Run `Be.Cars.DbMigrator` to create the initial database. This should be done in the first run. It is also needed if a new database migration is added to the solution later.

### Running the application

```bash
# note the HTTPS endpoint, the authentication server refuses to run on HTTP
set ASPNETCORE_URLS=https://*:5000/
set ASPNETCORE_ENVIRONMENT=Development
Be.Cars.AuthServer.exe
#dotnet run --project .\src\Be.Cars.AuthServer\Be.Cars.AuthServer.csproj
```

### Solution structure

This is a layered monolith application that consists of the following applications:

- `Be.Cars.DbMigrator`: A console application which applies the migrations and also seeds the initial data. It is useful on development as well as on production environment.
- `Be.Cars.AuthServer`: ASP.NET Core MVC / Razor Pages application that is integrated OAuth 2.0(`OpenIddict`) and account modules. It is used to authenticate users and issue tokens.
- `Be.Cars.HttpApi.Host`: ASP.NET Core API application that is used to expose the APIs to the clients.
- `Be.Cars.Blazor`: ASP.NET Core Blazor Server application that is the essential web application of the solution.

## Deploying the application

Deploying an ABP application is not different than deploying any .NET or ASP.NET Core application. However, there are some topics that you should care about when you are deploying your applications. You can check ABP's [Deployment documentation](https://docs.abp.io/en/abp/latest/Deployment/Index) and ABP Commercial's [Deployment documentation](https://docs.abp.io/en/commercial/latest/startup-templates/application/deployment?UI=MVC&DB=EF&Tiered=No) before deploying your application.

### Additional resources

You can see the following resources to learn more about your solution and the ABP Framework:

- [Web Application Development Tutorial](https://docs.abp.io/en/commercial/latest/tutorials/book-store/part-1)
- [Application Startup Template](https://docs.abp.io/en/commercial/latest/startup-templates/application/index)
- [LeptonX Theme Module](https://docs.abp.io/en/commercial/latest/themes/lepton-x/index)
- [LeptonX Blazor UI](https://docs.abp.io/en/commercial/latest/themes/lepton-x/blazor?UI=BlazorServer)

### TODO

- Use/evaluate [ReportGenerator](https://github.com/danielpalme/ReportGenerator)