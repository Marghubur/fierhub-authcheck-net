# \# \*\*Integration Guide – fierhub\_authcheck\_net\*\*

# 

# This document explains the steps required to integrate \*\*fierhub\_authcheck\_net\*\* into your application for generating JWT tokens.

# 

# ---

# 

# \## \*\*Step 1 – Add Configuration in `appsettings.json`\*\*

# 

# Add the following section inside your `appsettings.json` file:

# 

# ```json

# "FireHub": {

# &nbsp; "TokenManager": "https://www.bottomhalf.in/bt/s3/ExternalTokenManager/generateToken",

# &nbsp; "Token": "YourPrivateRepoToken",

# &nbsp; "TokenRepositoryUrl": "TokenRepoURL",

# &nbsp; "DatabaseConfiguration": true

# }

# ```

# 

# ---

# 

# \## \*\*Step 2 – Register Required Services in `Program.cs`\*\*

# 

# In your `Program.cs` file, register the following services:

# 

# ```csharp

# builder.Services.AddHttpClient();

# 

# FierHubRegistry.Builder(builder.Services, builder.Environment, builder.Configuration)

# &nbsp;   .RegisterFierAuth();

# ```

# 

# ---

# 

# \## \*\*Step 3 – Register Middleware (After CORS Middleware)\*\*

# 

# Add the following middleware calls after your CORS configuration:

# 

# ```csharp

# app.UseMiddleware<RequestMiddleware>();

# app.UseAuthentication();

# app.UseAuthorization();

# ```

# 

# ---

# 

# \## \*\*Step 4 – Inject FireHub Service Interface Where Needed\*\*

# 

# Wherever you want to generate a JWT token, inject the `IFierHubService` dependency:

# 

# ```csharp

# private readonly IFierHubService \_fierHubService;

# ```

# 

# ---

# 

# \## \*\*Step 5 – Generate JWT Token\*\*

# 

# Call the following method to generate a JWT token.

# It returns an `ApiResponse` object, where the \*\*ResponseBody\*\* contains the JWT token.

# 

# ```csharp

# var response = await \_fierHubService.GenerateToken(claims);

# ```

# 

# ---

# 

# ✅ \*\*Now your application is ready to generate and use JWT tokens with fierhub\_authcheck\_net.\*\*

# 

# ---



