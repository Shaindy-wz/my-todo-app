using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// --- 1. שירותים ---
// השארתי את ה-Connection String המדויק שלך שעובד
var connectionString = "server=127.0.0.1;database=TodoDb;user=todo;password=A2bAiND%L7.29e!;";
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 2. Middleware ---
app.Urls.Add("http://localhost:5232");
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// עדכון בסיס נתונים ויצירת משתמשים
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
    try
    {
        // הוספת עמודת DueDate אם לא קיימת
        try { context.Database.ExecuteSqlRaw("ALTER TABLE items ADD DueDate DATETIME NULL;"); } catch { }

        // הוספת עמודת UserId אם לא קיימת - חשוב מאוד לסינון!
        try { context.Database.ExecuteSqlRaw("ALTER TABLE items ADD UserId INT NOT NULL DEFAULT 1;"); } catch { }

        Console.WriteLine("✅ Database schema is up to date.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("ℹ️ Database update note: " + ex.Message);
    }

    if (context.Database.CanConnect() && !context.Users.Any())
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("123");
        context.Users.Add(new User { Username = "admin", Password = hashedPassword });
        context.SaveChanges();
        Console.WriteLine("✅ Admin user created.");
    }
}

// --- 3. פונקציית עזר לבדיקת Token ושליפת ה-ID של המשתמש ---
// הפונקציה מחזירה את ה-ID של המשתמש מתוך ה-Token (למשל מ-"user-token-5" היא תוציא 5)
int? GetUserIdFromToken(HttpContext context)
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer user-token-"))
        return null;

    string idPart = authHeader.Replace("Bearer user-token-", "");
    if (int.TryParse(idPart, out int userId))
        return userId;

    return null;
}

// --- 4. נתיבים (Routes) ---

// כניסה (Login)
app.MapPost("/login", async (ToDoDbContext context, User user) => {
    var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
    if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.Password))
        return Results.Unauthorized();

    // מחזיר טוקן שמכיל את ה-ID של המשתמש
    return Results.Ok(new { token = "user-token-" + dbUser.Id });
});

// הרשמה (Register)
app.MapPost("/register", async (ToDoDbContext context, User newUser) => {
    if (await context.Users.AnyAsync(u => u.Username == newUser.Username))
        return Results.BadRequest("User already exists");

    newUser.Password = BCrypt.Net.BCrypt.HashPassword(newUser.Password);
    context.Users.Add(newUser);
    await context.SaveChangesAsync();
    return Results.Ok(new { message = "User registered successfully" });
});

// קבלת משימות - מסונן לפי המשתמש המחובר!
app.MapGet("/tasks", async (ToDoDbContext context, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();

    var tasks = await context.Items
        .Where(t => t.UserId == userId)
        .ToListAsync();
    return Results.Ok(tasks);
});

// הוספת משימה - שומרת את ה-UserId של מי שהוסיף
app.MapPost("/tasks", async (ToDoDbContext context, Item item, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();

    try
    {
        item.UserId = userId.Value; // הצמדת המשימה למשתמש המחובר
        context.Items.Add(item);
        await context.SaveChangesAsync();
        return Results.Created($"/tasks/{item.Id}", item);
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ ERROR ADDING TASK: " + ex.Message);
        return Results.Problem("Database Error: " + ex.Message);
    }
});

// מחיקת משימה - מוודא שהמשתמש מוחק רק את שלו
app.MapDelete("/tasks/{id}", async (ToDoDbContext context, int id, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();

    var item = await context.Items.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (item is null) return Results.NotFound();

    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
});

// עדכון משימה - מוודא שהמשתמש מעדכן רק את שלו
app.MapPut("/tasks/{id}", async (ToDoDbContext context, int id, Item updatedItem, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();

    var item = await context.Items.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (item is null) return Results.NotFound();

    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;
    item.DueDate = updatedItem.DueDate;

    await context.SaveChangesAsync();
    return Results.Ok(item);
});

app.Run();