using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;

public class Thing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";

    [Timestamp]
    public byte[] ConcurrencyStamp { get; set; } = Guid.NewGuid().ToByteArray();
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
    : base(options)
    {
    }

    public DbSet<Thing> Things => Set<Thing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsSqlite())
        {
            // This is required, otherwise you'd get NOT NULL constraint failures
            modelBuilder.Entity<Thing>()
                        .Property(p => p.ConcurrencyStamp)
                        .HasDefaultValueSql("randomblob(16)");

        }

        base.OnModelCreating(modelBuilder);
    }
}

public class Tests
{
    private static TestDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=file::memory:?cache=shared;")
            .Options;
        var context = new TestDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void VerifyConcurrencyTokenFailure()
    {
        using var context = CreateInMemoryDb();

        // Create thing
        var addThing = new Thing()
        {
            Name = "First"
        };
        context.Things.Add(addThing);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        Assert.NotNull(addThing.ConcurrencyStamp);
        Assert.NotEqual(Array.Empty<byte>(), addThing.ConcurrencyStamp);

        // Ensure that concurrency token is enforced by EF
        Assert.Throws<DbUpdateConcurrencyException>(() =>
        {
            var updateThing = new Thing()
            {
                Id = addThing.Id,
                Name = "Second",
                ConcurrencyStamp = Guid.NewGuid().ToByteArray()
            };
            context.Update(updateThing);
            context.SaveChanges();
        });
    }

    [Fact]
    public void UpdateStampAutomatically()
    {
        // It's notable that even MS has given up on this one, as Sqlite and
        // many other backends don't support ROWVERSION, so they just update the
        // ConcurrencyStamp manually on update like here:
        // https://github.com/dotnet/aspnetcore/blob/d9660d157627af710b71c636fa8cb139616cadba/src/Identity/EntityFrameworkCore/src/UserStore.cs#L185-L187

        using var context = CreateInMemoryDb();

        var job = new Thing()
        {
            Name = "First"
        };
        context.Things.Add(job);
        context.SaveChanges();

        // Each update should change the timestamp automagically
        var timestamp1 = new Guid(job.ConcurrencyStamp);
        job.Name = "Second";
        context.SaveChanges();

        var timestamp2 = new Guid(job.ConcurrencyStamp);
        Assert.NotEqual(timestamp1, timestamp2);
    }
}