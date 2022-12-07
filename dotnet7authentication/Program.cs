using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;

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
    .AddCookie("cookie");
builder.Services.AddAuthorization(b =>
{
    b.AddPolicy("eu passport", pb =>
    {
        pb.RequireAuthenticatedUser().AddAuthenticationSchemes("cookie").RequireClaim("passport_type", "eur");
    });
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