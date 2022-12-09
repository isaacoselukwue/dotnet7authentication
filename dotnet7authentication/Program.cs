using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddDataProtection();
//builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie("cookie").AddCookie("local").AddCookie("patreon-cookie")
    .AddOAuth("external-patreon", o =>
    {
        o.SignInScheme = "patreon-cookie";
        o.ClientId = "id";
        o.ClientSecret = "secret";
        o.AuthorizationEndpoint = "https://oauth.mocklab.io/oauth/authorize";
        o.TokenEndpoint = "https://oauth.mocklab.io/oauth/token";
        o.UserInformationEndpoint = "https://oauth.mocklab.io/userinfo";
        o.CallbackPath = "/cb-patreon";
        o.Scope.Add("profile");
        o.SaveTokens = true;
    });
builder.Services.AddAuthorization(b =>
{
    //b.AddPolicy("eu passport", pb =>
    //{
    //    pb.RequireAuthenticatedUser()
    //    .AddAuthenticationSchemes("cookie")
    //    .AddRequirements()
    //    .RequireClaim("passport_type", "eur");
    //});
});

var app = builder.Build();

//app.Use((ctx, next) =>
//{
//    var idp = ctx.RequestServices.GetRequiredService<IDataProtectionProvider>();
//    var protector = idp.CreateProtector("auth-cookie");
//    var authCookie = ctx.Request.Headers.Cookie.FirstOrDefault(x => x.StartsWith("auth="));
//    string protectedPayload = authCookie.Split("=").Last();
//    var payload = protector.Unprotect(protectedPayload);
//    var parts = payload.Split(":");
//    var key = parts[0];
//    var value = parts[1];

//    var claims = new List<Claim>();
//    claims.Add(new Claim(key, value));
//    var identity = new ClaimsIdentity(claims);
//    ctx.User = new System.Security.Claims.ClaimsPrincipal(identity);


//    return next();
//});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/username", (HttpContext ctx) =>
{
    
    return ctx.User.FindFirst("usr").Value;
}).WithOpenApi();

app.MapGet("/unsecure", (HttpContext ctx) =>
{
    return ctx.User.FindFirst("usr")?.Value ?? "empty";
});
app.MapGet("/sweden", (HttpContext ctx) =>
{
    //if (!ctx.User.Identities.Any(x => x.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme))
    //{
    //    ctx.Response.StatusCode = 401;
    //    return "";
    //}
    //if (!ctx.User.HasClaim("passport_type", "eur"))
    //{
    //    ctx.Response.StatusCode = 403;
    //    return "";
    //}
    return "allowed";
}).RequireAuthorization("eu passport");
app.MapGet("/login", async (HttpContext ctx) =>
{
    //await auth.SignIn();
    var claims = new List<Claim>();
    claims.Add(new Claim("usr", "isaac"));
    var identity = new ClaimsIdentity(claims, "cookie");
    var user = new System.Security.Claims.ClaimsPrincipal(identity);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
    };
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user, authProperties);
    return "ok";
}).WithOpenApi();

app.MapGet("/login2", async (HttpContext ctx) =>
{
    var claims = new List<Claim>();
    claims.Add(new Claim("usr", "isaac"));
    claims.Add(new Claim("passport_type", "eur"));
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var user = new ClaimsPrincipal(identity);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);
}).WithOpenApi().AllowAnonymous();

app.MapGet("/login-local", async (ctx) =>
{
    var claims = new List<Claim>();
    claims.Add(new Claim("user", "isaac"));
    var identity = new ClaimsIdentity(claims, "cookie");
    var user = new ClaimsPrincipal(identity);
    await ctx.SignInAsync("local", user);
});

app.MapGet("/login-patreon", async (ctx) =>
{

    await ctx.ChallengeAsync("eternal-paetron", new AuthenticationProperties()
    {
        RedirectUri = "/"
    });
});


app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
//public class AuthService
//{
//    private readonly IDataProtectionProvider _idp;
//    private readonly IHttpContextAccessor _accessor;
//    public AuthService(IDataProtectionProvider idp, IHttpContextAccessor accessor)
//    {
//        _idp = idp;
//        _accessor = accessor;
//    }
//    public async Task SignIn()
//    {
//        var protector = _idp.CreateProtector("auth-cookie");
//        _accessor.HttpContext.Response.Headers["set-cookie"] = $"auth={protector.Protect("usr:isaac")}";
//    }
//}

public class VisitorAuthHandler : CookieAuthenticationHandler
{
    public VisitorAuthHandler(IOptionsMonitor<CookieAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {

    }
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await base.HandleAuthenticateAsync();
        if (result.Succeeded)
        {
            return result;
        }
        // return base.HandleAuthenticateAsync();
        var claims = new List<Claim>();
        claims.Add(new Claim("user", "isaac"));
        var identity = new ClaimsIdentity(claims, "visitor");
        var user = new ClaimsPrincipal(identity);
        await Context.SignInAsync("visitor", user);
        return AuthenticateResult.Success(new AuthenticationTicket(user, "visitor"));
    }
}