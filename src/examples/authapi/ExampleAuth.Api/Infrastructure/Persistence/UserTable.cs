using ExampleAuth.Api.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExampleAuth.Api.Infrastructure.Persistence;

public class UserTable : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.HasIndex(u => new { u.Login, u.Password }).IsUnique();

        builder.Property(u => u.Id).HasColumnName("id").IsRequired();
        builder.Property(u => u.Login).HasColumnName("login").HasMaxLength(250).IsRequired();
        builder.Property(u => u.Password).HasColumnName("password").HasMaxLength(300).IsRequired();
    }
}