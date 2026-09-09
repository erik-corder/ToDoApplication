using System;
using System.Collections.Generic;

namespace Kanban.Application.Boards;

public sealed record BoardColumnDto(Guid Id, string Name, int OrderIndex);

public sealed record CreateBoardResponse(Guid BoardId, IReadOnlyList<BoardColumnDto> Columns);
