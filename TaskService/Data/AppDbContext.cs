using Microsoft.EntityFrameworkCore;

namespace TaskApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);