using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired(false); // NULL para categorías del sistema

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Almacena el enum como string en BD ("Income", "Expense", "Both")
        // Más legible que almacenar números (0, 1, 2)
        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(c => c.Color)
            .HasColumnName("color")
            .HasMaxLength(7)
            .HasDefaultValue("#6B7280");

        builder.Property(c => c.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(c => c.IsDeleted);

        // Índices
        builder.HasIndex(c => new { c.UserId, c.Name, c.Type })
            .IsUnique()
            .HasDatabaseName("idx_categories_user_name_type")
            .HasFilter("deleted_at IS NULL");

        // Datos semilla — categorías del sistema que estarán disponibles
        // para todos los usuarios desde el inicio
        builder.HasData(GetDefaultCategories());
    }

    private static List<Category> GetDefaultCategories()
    {
        // Fecha estática fija — nunca cambia entre compilaciones
        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return new List<Category>
        {
            // Gastos
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "Alimentación",  Type = CategoryType.Expense,
                    Icon = "utensils",      Color = "#EF4444", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Name = "Transporte",    Type = CategoryType.Expense,
                    Icon = "car",           Color = "#F97316", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Name = "Vivienda",      Type = CategoryType.Expense,
                    Icon = "home",          Color = "#EAB308", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                    Name = "Salud",         Type = CategoryType.Expense,
                    Icon = "heart-pulse",   Color = "#22C55E", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    Name = "Entretenimiento", Type = CategoryType.Expense,
                    Icon = "tv",            Color = "#3B82F6", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                    Name = "Educación",     Type = CategoryType.Expense,
                    Icon = "book",          Color = "#8B5CF6", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                    Name = "Servicios",     Type = CategoryType.Expense,
                    Icon = "zap",           Color = "#14B8A6", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                    Name = "Otros gastos",  Type = CategoryType.Expense,
                    Icon = "circle-ellipsis", Color = "#6B7280", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            // Ingresos
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
                    Name = "Salario",       Type = CategoryType.Income,
                    Icon = "briefcase",     Color = "#22C55E", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"),
                    Name = "Freelance",     Type = CategoryType.Income,
                    Icon = "laptop",        Color = "#3B82F6", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"),
                    Name = "Inversiones",   Type = CategoryType.Income,
                    Icon = "trending-up",   Color = "#8B5CF6", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },

            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"),
                    Name = "Otros ingresos", Type = CategoryType.Income,
                    Icon = "circle-plus",   Color = "#6B7280", IsDefault = true,
                    CreatedAt = seedDate,   UpdatedAt = seedDate },
        };
    }
}