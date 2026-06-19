using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Resend;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();

using (var db = new Database())
{
    db.Database.Migrate();
}

app.MapGet("/users", (int? userId) =>
{
    if (userId == null)
    {
        return Results.Unauthorized();
    }
    using var db = new Database();
    var users = db.Users.Select(u => new {u.Id, u.Username, u.Email, u.Status, u.LastLogin}).ToList();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == userId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
    return Results.Unauthorized();
    }

    return Results.Ok(users);
});

app.MapPost("/api/login", (LoginRequest request) =>
{
    using var db = new Database();

    var user = db.Users.FirstOrDefault(u=> u.Username == request.Username && u.Password == request.Password);
    

    if(user!=null)
    {
        user.LastLogin = DateTime.Now;
        db.SaveChanges();
        return Results.Ok(new {success = true, userId = user.Id});
    } else
    {
        return Results.Ok(new {success = false});
    }
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

app.MapPost("/api/delete", (ActionRequest request) =>
{
    using var db = new Database();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == request.userId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    foreach (var id in request.ids)
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

app.MapPost("/api/deleteUnverified", (ActionRequest request) =>
{
    using var db = new Database();
    var unverifiedUsers = db.Users.Where(u => u.Status == "Unverified").ToList();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == request.userId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    db.Users.RemoveRange(unverifiedUsers);

    db.SaveChanges();
    return Results.Ok();
});

app.MapPost("/api/block", (ActionRequest request) =>
{
    using var db = new Database();
    
    var currentUser = db.Users.FirstOrDefault(u => u.Id == request.userId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    
    foreach (var id in request.ids)
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

app.MapPost("/api/unblock", (ActionRequest request) =>
{
    using var db = new Database();
    var currentUser = db.Users.FirstOrDefault(u => u.Id == request.userId);
    if (currentUser == null || currentUser.Status == "Blocked")
    {
        return Results.Unauthorized();
    }
    foreach (var id in request.ids)
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

app.MapPost("/api/register", async (RegisterRequest request) =>
{
    using var db = new Database();

    if (request.Password != request.ConfirmPassword)
    {
        return Results.Ok(new {success = false, message = "Пароли не совпадают"});
    }

    try
    {
        
        var newUser = new User {Username = request.Username, Password = request.Password, Email = request.Email, Status = "Unverified", LastLogin = DateTime.Now, VerificationToken = Guid.NewGuid().ToString()};
db.Users.Add(newUser);
db.SaveChanges();
SendVerificationEmail(newUser.Email, newUser.VerificationToken);
return Results.Ok(new {success = true, message = "Аккаунт зарегистрирован", userId = newUser.Id});
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

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.Run();

record LoginRequest(string Username, string Password);
record RegisterRequest(string Username, string Password, string ConfirmPassword, string Email);
record ActionRequest(int userId, int[] ids);
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }

    public DateTime LastLogin { get; set; }
    public string Status { get; set; }
    public string VerificationToken { get; set; }
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