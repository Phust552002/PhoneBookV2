using Microsoft.EntityFrameworkCore;
using PhoneBook.Models;

public class HRMDbContext : DbContext
{
    public HRMDbContext(DbContextOptions<HRMDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().ToTable("Employees").HasKey(e => e.UserId);
        modelBuilder.Entity<Department>().ToTable("H0_Departments").HasKey(d => d.DepartmentId);
    }
}
