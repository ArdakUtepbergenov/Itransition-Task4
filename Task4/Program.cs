using Microsoft.EntityFrameworkCore;
using Resend;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

using (var db = new Database())
{
    db.Database.Migrate();
}

app.MapGet("/users", (HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    
    var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    int userId = int.Parse(userIdString);
    
    using var db = new Database();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == userId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    var users = db.Users.Select(u => new {u.Id, u.Username, u.Email, u.Status, u.LastLogin}).ToList();
    return Results.Ok(users);
});




app.MapPost("/api/login", async (LoginRequest request, HttpContext context) =>
{
    using var db = new Database();
    var user = db.Users.FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);

    if (user != null)
    {
        user.LastLogin = DateTime.UtcNow;
        db.SaveChanges();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        var principal = new ClaimsPrincipal(identity);
        await context.SignInAsync("MyCookieAuth", principal);

        return Results.Ok(new {success = true});
    }
    return Results.Ok(new {success = false});
});

async Task SendVerificationEmail(string toEmail, string token)
{
    IResend resend = ResendClient.Create(Environment.GetEnvironmentVariable("RESEND_API_KEY"));
    
    var link = $"https://itransition-task4-production.up.railway.app/api/verify?token={token}";
    
    await resend.EmailSendAsync(new EmailMessage()
    {
        From = "onboarding@resend.dev",
        To = toEmail,
        Subject = "Подтверждение регистрации",
        HtmlBody = $"<p>Перейдите по ссылке для подтверждения: <a href=\"{link}\">{link}</a></p>",
    });
}

app.MapPost("/api/delete", (int[] ids, HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    
    var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    int currentUserId = int.Parse(userIdString);

    using var db = new Database();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == currentUserId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    foreach (var id in ids)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            db.Users.Remove(user);
        }
        
    }
    db.SaveChanges();
    return Results.Ok();
});

app.MapPost("/api/deleteUnverified", (HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    
    var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    int currentUserId = int.Parse(userIdString);

    using var db = new Database();
    var unverifiedUsers = db.Users.Where(u => u.Status == "Unverified").ToList();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == currentUserId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    db.Users.RemoveRange(unverifiedUsers);

    db.SaveChanges();
    return Results.Ok();
});

app.MapPost("/api/block", (int[] ids, HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    
    var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    int currentUserId = int.Parse(userIdString);
    
    using var db = new Database();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == currentUserId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    
    foreach (var id in ids)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            user.Status = "Blocked";
        }
    }
    db.SaveChanges();
    return Results.Ok();
});

app.MapPost("/api/unblock", (int[] ids, HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    
    var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    int currentUserId = int.Parse(userIdString);
    
    using var db = new Database();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == currentUserId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    foreach (var id in ids)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            user.Status = "Active";
        }
        
    }
    db.SaveChanges();
    return Results.Ok();
    
});

app.MapPost("/api/register", async (RegisterRequest request, HttpContext context) =>
{
    using var db = new Database();
    

    if (request.Password != request.ConfirmPassword)
    {
        return Results.Ok(new {success = false, message = "Пароли не совпадают"});
    }

    try
    {
        
        var newUser = new User {Username = request.Username, Password = request.Password, Email = request.Email, Status = "Unverified", LastLogin = DateTime.UtcNow, VerificationToken = Guid.NewGuid().ToString(), SessionToken = Guid.NewGuid().ToString()};
db.Users.Add(newUser);
db.SaveChanges();
var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()) };
var identity = new ClaimsIdentity(claims, "MyCookieAuth");
var principal = new ClaimsPrincipal(identity);
await context.SignInAsync("MyCookieAuth", principal);
try
{
    await SendVerificationEmail(newUser.Email, newUser.VerificationToken);
}
catch (Exception ex)
{
    Console.WriteLine("EMAIL ERROR: " + ex.Message);
}
return Results.Ok(new {success = true, message = "Аккаунт зарегистрирован"});
    }
    catch (Exception)
    {
        return Results.Ok(new {success = false, message = "Этот email уже используется"});
    }
});

app.MapGet("/api/verify", (string token) =>
{
    using var db = new Database();
    var user = db.Users.FirstOrDefault(u => u.VerificationToken == token);
    
    if (user != null)
    {
        user.Status = "Active";
        db.SaveChanges();
        return Results.Ok("Email подтверждён!");
    }
    return Results.BadRequest("Неверная ссылка");
});

app.MapPost("/api/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("MyCookieAuth");
    return Results.Ok();
});

app.UseCors(policy => policy
    .WithOrigins("https://ardakutepbergenov.github.io")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
app.Run();

record LoginRequest(string Username, string Password);
record RegisterRequest(string Username, string Password, string ConfirmPassword, string Email);
record ActionRequest(string sessionToken, int[] ids);
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }

    public DateTime LastLogin { get; set; }
    public string Status { get; set; }
    public string VerificationToken { get; set; }
    public string SessionToken { get; set; }
}

public class Database : DbContext
{
    public DbSet<User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
}

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data source = mydb.db");
    }
}
