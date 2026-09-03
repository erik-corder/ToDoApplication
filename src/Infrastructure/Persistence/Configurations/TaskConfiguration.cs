using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.OrganizationId).IsRequired();
        builder.Property(t => t.BoardId).IsRequired();
        builder.Property(t => t.ColumnId).IsRequired();
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(8000);
        builder.Property(t => t.Status).IsRequired().HasMaxLength(32);
        builder.Property(t => t.Priority).IsRequired().HasMaxLength(32);
        builder.Property(t => t.ReporterUserId).IsRequired();
        builder.Property(t => t.OrderIndex).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // SQL Server rowversion => auto-increment on insert/update, used as concurrency token.
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(t => new { t.BoardId, t.ColumnId, t.OrderIndex });

        // Global query filter for tenant isolation (per HLD). IgnoreQueryFilters() is forbidden in S3.
        builder.HasQueryFilter(t => t.OrganizationId == EF.Property<Guid>(EF.Functions, "__CurrentOrgId__"));
    }
}
