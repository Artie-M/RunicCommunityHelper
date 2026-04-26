using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.EndpointManagers;

public class GoogleDocumentsEndpointManager
{
    public static GoogleDocumentsEndpointManager Singleton { get; private set; }

    static GoogleDocumentsEndpointManager()
    {
        Singleton = new GoogleDocumentsEndpointManager();
    }

    private WebApplication _app;
    
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/googledocuments", GetDocumentsAsync);
        app.MapGet("/api/googledocuments/{id:min(0)}", GetDocumentAsync);
        app.MapDelete("api/googledocuments/{id:min(0)}", DeleteDocumentAsync);
        app.MapPut("api/googledocuments/{id:min(0)}", PutDocumentAsync);
        app.MapPost("api/googledocuments/", PostDocumentAsync);
        _app = app;
    }

    [Authorize(Roles = "Admin")]
    private async Task GetDocumentsAsync(HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        httpContext.Response.Headers.ContentLanguage = "ru-RU";
        
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        await httpContext.Response.WriteAsJsonAsync(await db.GoogleDocuments.ToListAsync());
    }
    
    [Authorize(Roles = "Admin")]
    private async Task GetDocumentAsync(uint id, HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        var doc = await db.GoogleDocuments.FindAsync(id);
        
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        httpContext.Response.Headers.ContentLanguage = "ru-RU";
        if (doc == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        await httpContext.Response.WriteAsJsonAsync(doc);
    }
    
    [Authorize(Roles = "Admin")] 
    private async Task DeleteDocumentAsync(uint id, HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        
        var doc = await db.GoogleDocuments.FindAsync(id);
        if (doc == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        db.GoogleDocuments.Remove(doc);
        await db.SaveChangesAsync();
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    [Authorize(Roles = "Admin")] 
    private async Task PutDocumentAsync(uint id, HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        
        var document = await httpContext.Request.ReadFromJsonAsync<GoogleDocumentEntity>();
        if (document == null || id != document.Id)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var existingDocument = await db.GoogleDocuments.FindAsync(id);
        if (existingDocument == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        existingDocument.FileId = document.FileId;
        existingDocument.RequiredRole = document.RequiredRole;

        await db.SaveChangesAsync();
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    [Authorize(Roles = "Admin")]
    private async Task PostDocumentAsync(HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        
        var newDocument = await httpContext.Request.ReadFromJsonAsync<GoogleDocumentEntity>();
        if (newDocument == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await db.GoogleDocuments.AddAsync(newDocument);
        await db.SaveChangesAsync();
        
        httpContext.Response.StatusCode = StatusCodes.Status201Created;
        await httpContext.Response.WriteAsJsonAsync(newDocument);
    }
}