using System;
using System.Collections.Generic;

namespace Kanban.Application.Boards;

public sealed record SidebarBoardDto(Guid Id, string Name, DateTime? ArchivedAt);

public sealed record SidebarResponse(Guid WorkspaceId, IReadOnlyList<SidebarBoardDto> Boards);
