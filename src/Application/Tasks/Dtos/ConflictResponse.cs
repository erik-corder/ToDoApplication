namespace Application.Tasks.Dtos;

public class ConflictResponse
{
    public string Message { get; set; } = "This task was updated by someone else.";
    public TaskDto Latest { get; set; } = default!;
}
