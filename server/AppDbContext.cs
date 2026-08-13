using Microsoft.EntityFrameworkCore;
using BookIllustrator.Models;

namespace BookIllustrator;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<GeneratedImage> GeneratedImages => Set<GeneratedImage>();
}