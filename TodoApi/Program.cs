using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// --- 1. הגדרת מסד הנתונים ---
// כאן שמתי את מחרוזת החיבור של Clever Cloud ששלחת לי
var connectionString = "server=b6hiidir7yoggurpa6jd-mysql.services.clever-cloud.com;database=b6hiidir7yoggurpa6jd;user=ugu0do5xka3k4qsf;password=m14AwwOjxZPOaAAC41nB;port=3306";

// הגדרת גרסה ידנית כדי למנוע קריסות בחיבור ראשוני
var serverVersion = new MySqlServerVersion(new Version(8, 0, 31)); 

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, serverVersion)
);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 2. Middleware ---
// שים לב: מחקנו את השורה של localhost כי היא גורמת לשגיאה ב-Render
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// עדכון אוטומטי של בסיס הנתונים (Migrations/Tables)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
    try
    {
        // הוספת עמודות אם הן חסרות
        try { context.Database.ExecuteSqlRaw("ALTER TABLE items ADD DueDate DATETIME NULL;"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE items ADD UserId INT NOT NULL DEFAULT 1;"); } catch { }

        if (context.Database.CanConnect() && !context.Users.Any())
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("123");
            context.Users.Add(new User { Username = "admin", Password = hashedPassword });
            context.SaveChanges();
            Console.WriteLine("✅ Admin user created.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("ℹ️ Database Initialization Note: " + ex.Message);
    }
}

// --- 3. פונקציית עזר לטוקן ---
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

// --- 4. נתיבים (API Routes) ---

app.MapPost("/login", async (ToDoDbContext context, User user) => {
    var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
    if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.Password))
        return Results.Unauthorized();
    return Results.Ok(new { token = "user-token-" + dbUser.Id });
});

app.MapPost("/register", async (ToDoDbContext context, User newUser) => {
    if (await context.Users.AnyAsync(u => u.Username == newUser.Username))
        return Results.BadRequest("User already exists");
    newUser.Password = BCrypt.Net.BCrypt.HashPassword(newUser.Password);
    context.Users.Add(newUser);
    await context.SaveChangesAsync();
    return Results.Ok(new { message = "User registered successfully" });
});

app.MapGet("/tasks", async (ToDoDbContext context, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();
    return Results.Ok(await context.Items.Where(t => t.UserId == userId).ToListAsync());
});

app.MapPost("/tasks", async (ToDoDbContext context, Item item, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();
    item.UserId = userId.Value;
    context.Items.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/tasks/{item.Id}", item);
});

app.MapDelete("/tasks/{id}", async (ToDoDbContext context, int id, HttpContext httpContext) => {
    var userId = GetUserIdFromToken(httpContext);
    if (userId == null) return Results.Unauthorized();
    var item = await context.Items.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (item is null) return Results.NotFound();
    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
});

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