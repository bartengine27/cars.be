# Be.Cars

## About this solution

This is a layered startup solution based on [Domain Driven Design (DDD)](https://docs.abp.io/en/abp/latest/Domain-Driven-Design) practises. All the fundamental ABP modules are already installed. Check the [Application Startup Template](https://docs.abp.io/en/commercial/latest/startup-templates/application/index) documentation for more info.

### Pre-requirements

* [.NET 8.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Node v18 or 20](https://nodejs.org/en)
* [Redis](https://redis.io/)

### Authentication Server

This solution is integrated with the `OpenID Connect` and `OAuth 2.0` [OpenIddict](https://documentation.openiddict.com/) authentication server, 
the [ABP](https://abp.io/) default.

`OpenIddict` serves as a versatile open-source framework for implementing `OpenID Connect` and `OAuth 2.0` in .NET or .NET Core applications. Its design is aimed at simplifying 
the complex processes involved in authentication and authorization, providing developers with a robust, extensible, and compliant solution. When integrated into 
the ABP.io framework — a modern web development platform that leverages the ASP.NET Core framework — OpenIddict not only enhances the security of applications 
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

This section will guide .NET Core developers through the process of setting up OpenIddict within an ABP.io application to secure an API using the 
Client Credentials flow. The Client Credentials grant is ideally suited for server-to-server communication, 
where an application acts on its own behalf rather than on behalf of an individual user. 

Integrating OpenIddict with ABP.io to facilitate the Client Credentials flow involves a series of configuration steps and customizations within 
the `Be.Cars.AuthServer` and `Be.Cars.Domain` project. 

First, ensure that your ABP.io project is set up and running: you can create a new ABP.io project using the ABP CLI, [see](https://abp.io/get-started). After successfully 
creating the project, you can proceed with the following steps to configure OpenIddict for the Client Credentials flow:

* Configure the OpenIddict Server in the `Be.Cars.AuthServer` project:

  Enable the Client Credentials flow in the `Be.Cars.AuthServer` project by adding the following code to the `PreConfigureServices` method in the `CarsAuthServerModule.cs` file:

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
                //type: OpenIddictConstants.ClientTypes.Public,
                type: OpenIddictConstants.ClientTypes.Confidential,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "Swagger Application",
                secret: configurationSection["Cars_BlazorServerTiered:ClientSecret"] ?? "1q2w3e*",
                grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.ClientCredentials, },
                scopes: commonScopes,
                redirectUri: $"{swaggerRootUrl}/swagger/oauth2-redirect.html",
                clientUri: swaggerRootUrl,
                logoUri: "/images/clients/swagger.svg"
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
  
    You wouldn’t normally use the client credentials flow because it requires a client secret for authentication. Public clients typically use flows designed for 
    environments where secrets cannot be securely stored, such as the authorization code flow with PKCE.

  * For a confidential client:

    You must include the client_secret in requests to the token endpoint when using the client credentials grant (or other flows where client authentication is necessary), 
    as the server expects a form of client authentication that relies on the ability to securely store this secret.

  The `grantTypes` parameter specifies the `OpenIddictConstants.GrantTypes.AuthorizationCode` and `OpenIddictConstants.GrantTypes.ClientCredentials` grant types.

After executing the above steps, you should have a working OpenIddict server that supports the Client Credentials flow in your ABP.io application. You can test your setup by
executing the following command:

```bash
curl -X POST https://192.168.1.63:5000/connect/token \
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

You can decode the `access_token` using a JWT decoder to view the token's claims and verify its contents. For example the decoder [at](https://jwt.io/) can be used to decode the token:

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

For completeness, you can also test your password flow by executing the following command:

```bash
curl -X POST https://192.168.1.63:5000/connect/token \
  -k \
  -d "client_id=Cars_Swagger" \
  -d "client_secret=1q2w3e*" \
  -d "username=admin" \
  -d "password=1q2w3E*" \
  -d "grant_type=password" \
  -d "scope=Cars"
```

### Configurations

The solution comes with a default configuration that works out of the box. However, you may consider to change the following configuration before running your solution:

* Check the `ConnectionStrings` in `appsettings.json` files under the `Be.Cars.AuthServer`, `Be.Cars.HttpApi.Host` and `Be.Cars.DbMigrator` projects and change it if you need.

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

### Solution structure

This is a layered monolith application that consists of the following applications:

* `Be.Cars.DbMigrator`: A console application which applies the migrations and also seeds the initial data. It is useful on development as well as on production environment.
* `Be.Cars.AuthServer`: ASP.NET Core MVC / Razor Pages application that is integrated OAuth 2.0(`OpenIddict`) and account modules. It is used to authenticate users and issue tokens.
* `Be.Cars.HttpApi.Host`: ASP.NET Core API application that is used to expose the APIs to the clients.
* `Be.Cars.Blazor`: ASP.NET Core Blazor Server application that is the essential web application of the solution.

## Deploying the application

Deploying an ABP application is not different than deploying any .NET or ASP.NET Core application. However, there are some topics that you should care about when you are deploying your applications. You can check ABP's [Deployment documentation](https://docs.abp.io/en/abp/latest/Deployment/Index) and ABP Commercial's [Deployment documentation](https://docs.abp.io/en/commercial/latest/startup-templates/application/deployment?UI=MVC&DB=EF&Tiered=No) before deploying your application.

### Additional resources

You can see the following resources to learn more about your solution and the ABP Framework:

* [Web Application Development Tutorial](https://docs.abp.io/en/commercial/latest/tutorials/book-store/part-1)
* [Application Startup Template](https://docs.abp.io/en/commercial/latest/startup-templates/application/index)
* [LeptonX Theme Module](https://docs.abp.io/en/commercial/latest/themes/lepton-x/index)
* [LeptonX Blazor UI](https://docs.abp.io/en/commercial/latest/themes/lepton-x/blazor?UI=BlazorServer)
