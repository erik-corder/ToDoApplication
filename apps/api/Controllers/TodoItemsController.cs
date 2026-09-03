using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoApplication.Api.Data;
using ToDoApplication.Api.Models;

namespace ToDoApplication.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TodoItemsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TodoItem>>> GetAll()
    {
        var items = await _db.TodoItems.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoItem>> GetById(Guid id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    public record CreateTodoItemRequest(string Title);

    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create(CreateTodoItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        var item = new TodoItem { Title = request.Title.Trim() };
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    public record UpdateTodoItemRequest(string? Title, bool? IsCompleted);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTodoItemRequest request)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (request.Title is not null)
        {
            item.Title = request.Title.Trim();
        }
        if (request.IsCompleted is not null)
        {
            item.IsCompleted = request.IsCompleted.Value;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _db.TodoItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
