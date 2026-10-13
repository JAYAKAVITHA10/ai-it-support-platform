using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ITSupport.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ITSupport.API.Data;
using ITSupport.API.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseAuthorization();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic health checks
app.MapGet("/", () => Results.Ok(new
{
    message = "AI IT Support API is running",
    status = "Success"
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "ITSupport.API"
}));

app.MapGet("/api/health/database", async (AppDbContext db) =>
{
    try
    {
        bool connected = await db.Database.CanConnectAsync();

        return connected
            ? Results.Ok(new { status = "Connected", database = "ITSupportDB" })
            : Results.Problem("Database connection failed.", statusCode: 503);
    }
    catch
    {
        return Results.Problem("Database connection failed.", statusCode: 503);
    }
});




app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    CancellationToken cancellationToken) =>
{
    var email = request.Email.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(request.Password) ||
        request.Password.Length < 8)
    {
        return Results.BadRequest(new
        {
            message = "Name, valid email, and a password of at least 8 characters are required."
        });
    }

    var emailExists = await db.Users
        .AnyAsync(u => u.Email == email, cancellationToken);

    if (emailExists)
    {
        return Results.Conflict(new
        {
            message = "An account with this email already exists."
        });
    }

    var user = new User
    {
        FullName = request.FullName.Trim(),
        Email = email,
        Role = "Requester",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    user.PasswordHash = passwordHasher.HashPassword(
        user, request.Password);

    db.Users.Add(user);

    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(new
        {
            message = "The account could not be created. The email may already be registered."
        });
    }

    return Results.Created(
        $"/api/users/{user.UserId}",
        new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Role
        });
})
.AllowAnonymous();

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var email = request.Email.Trim().ToLowerInvariant();

    var user = await db.Users
        .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    if (user is null || !user.IsActive)
    {
        return Results.Unauthorized();
    }

    var verification = passwordHasher.VerifyHashedPassword(
        user, user.PasswordHash, request.Password);

    if (verification == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    var key = configuration["Jwt:Key"]!;
    var issuer = configuration["Jwt:Issuer"]!;
    var audience = configuration["Jwt:Audience"]!;

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var signingKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(key));

    var credentials = new SigningCredentials(
        signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: credentials);

    return Results.Ok(new
    {
        accessToken = new JwtSecurityTokenHandler().WriteToken(token),
        tokenType = "Bearer",
        expiresInMinutes = 60,
        user = new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Role
        }
    });
})
.AllowAnonymous();



// GET: list tickets, optionally filter by status

app.MapGet("/api/tickets", async (
    string? status,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdText, out var userId))
        return Results.Unauthorized();

    var query = db.Tickets.AsNoTracking().AsQueryable();

    // Requesters can only see tickets they created.
    if (principal.IsInRole("Requester"))
    {
        query = query.Where(t => t.CreatedByUserId == userId);
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(t => t.Status == status);
    }

    var tickets = await query
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync(cancellationToken);

    return Results.Ok(tickets);
})
.RequireAuthorization();


// GET: one ticket by ID
app.MapGet("/api/tickets/{id:int}", async (
    int id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var ticket = await db.Tickets
        .AsNoTracking()
        .SingleOrDefaultAsync(t => t.TicketId == id, cancellationToken);

    if (ticket is null)
        return Results.NotFound();

    if (principal.IsInRole("Requester"))
    {
        var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdText, out var userId))
            return Results.Unauthorized();

        if (ticket.CreatedByUserId != userId)
            return Results.Forbid();
    }

    return Results.Ok(ticket);
})
.RequireAuthorization();


 // POST: create a ticket
app.MapPost("/api/tickets", async (
    CreateTicketRequest request,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    // Get the authenticated user's ID from the JWT token.
    var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdText, out var userId))
        return Results.Unauthorized();

    // Verify that the logged-in user exists and is active.
    bool creatorExists = await db.Users
        .AnyAsync(u => u.UserId == userId && u.IsActive, cancellationToken);

    if (!creatorExists)
        return Results.Unauthorized();

    // Validate ticket details.
    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest(new
        {
            message = "Title and description are required."
        });
    }

    // Verify the assigned user, if provided.
    if (request.AssignedToUserId.HasValue)
    {
        bool assigneeExists = await db.Database
            .SqlQuery<int>(
                $"SELECT COUNT(*) AS [Value] FROM dbo.Users WHERE UserId = {request.AssignedToUserId.Value} AND IsActive = 1")
            .SingleAsync(cancellationToken) > 0;

        if (!assigneeExists)
        {
            return Results.BadRequest(new
            {
                message = "AssignedToUserId does not match an active user."
            });
        }
    }

    // Create the ticket using the authenticated user's ID.
    var ticket = new Ticket
    {
        Title = request.Title.Trim(),
        Description = request.Description.Trim(),
        Category = string.IsNullOrWhiteSpace(request.Category)
            ? "Unclassified"
            : request.Category.Trim(),
        Priority = request.Priority,
        Status = "Open",
        CreatedByUserId = userId,
        AssignedToUserId = request.AssignedToUserId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/tickets/{ticket.TicketId}", ticket);
})
.RequireAuthorization();


// PUT: update a ticket
app.MapPut("/api/tickets/{id:int}", async (
    int id,
    UpdateTicketRequest request,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var ticket = await db.Tickets
        .FirstOrDefaultAsync(t => t.TicketId == id, cancellationToken);

    if (ticket is null)
    {
        return Results.NotFound(new { message = $"Ticket {id} was not found." });
    }

    if (request.AssignedToUserId.HasValue)
    {
        
bool assigneeExists = await db.Database
    .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM dbo.Users WHERE UserId = {request.AssignedToUserId.Value}")
    .SingleAsync(cancellationToken) > 0;


        if (!assigneeExists)
        {
            return Results.BadRequest(new
            {
                message = "AssignedToUserId does not match an existing user."
            });
        }
    }

    ticket.Title = request.Title.Trim();
    ticket.Description = request.Description.Trim();
    ticket.Priority = request.Priority;
    ticket.Status = request.Status;
    ticket.Category = string.IsNullOrWhiteSpace(request.Category)
        ? "Unclassified"
        : request.Category.Trim();
    ticket.AssignedToUserId = request.AssignedToUserId;
    ticket.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(ticket);
})
.RequireAuthorization(policy => policy.RequireRole("Agent", "Admin"));

// DELETE: delete a ticket
app.MapDelete("/api/tickets/{id:int}", async (
    int id,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var ticket = await db.Tickets
        .FirstOrDefaultAsync(t => t.TicketId == id, cancellationToken);

    if (ticket is null)
    {
        return Results.NotFound(new { message = $"Ticket {id} was not found." });
    }

    db.Tickets.Remove(ticket);
    await db.SaveChangesAsync(cancellationToken);

    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();
