using System;
using System.Collections.Generic;

namespace Application.Tasks.Dtos;

public class BoardDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<BoardColumnDto> Columns { get; set; } = new();
}

public class BoardColumnDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public List<TaskDto> Tasks { get; set; } = new();
}
