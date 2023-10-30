using Microsoft.EntityFrameworkCore;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace SurvivalS2API.Models;

public sealed class Context : DbContext
{
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<ManualVote> ManualVotes { get; set; } = null!;
    public DbSet<Choice> Choices { get; set; } = null!;
    public DbSet<Night> Nights { get; set; } = null!;

    private static string DbPath { get; }
    
    static Context()
    {
        var path = Directory.GetCurrentDirectory();
        DbPath = Path.Join(path, "survival.db");
    }

    public Context()
    {
        Database.EnsureCreated();
    } 

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
            //.UseLazyLoadingProxies();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Vote>().HasKey(v => v.Id);
        modelBuilder.Entity<ManualVote>().HasKey(m => m.Id);
        modelBuilder.Entity<Choice>().HasKey(c => c.Rank);
        modelBuilder.Entity<Night>().HasKey(n => n.Id);
        
        modelBuilder.Entity<Player>().HasIndex(p => p.DiscordId).IsUnique();        
        modelBuilder.Entity<Player>().HasIndex(p => p.Name).IsUnique();
        
        modelBuilder.Entity<Vote>().HasOne(v => v.ByPlayer)
            .WithMany(p => p.VotesBy)
            .IsRequired();
        modelBuilder.Entity<Vote>().HasOne(v => v.ForPlayer)
            .WithMany(p => p.VotesFor)
            .IsRequired();
        modelBuilder.Entity<Vote>().HasOne(v => v.Choice)
            .WithMany()
            .HasForeignKey(v => v.ChoiceRank)
            .IsRequired();
        modelBuilder.Entity<Vote>().HasOne(v => v.OnNight)
            .WithMany(n => n.Votes)
            .IsRequired();

        modelBuilder.Entity<ManualVote>().HasOne(m => m.OnNight)
            .WithMany(n => n.ManualVotes)
            .IsRequired();
        modelBuilder.Entity<ManualVote>().HasOne(m => m.ForPlayer)
            .WithMany(p => p.ManualVotes)
            .IsRequired();


        modelBuilder.Entity<Player>().HasOne(p => p.DiedOnNight)
            .WithMany(n => n.Died)
            .HasForeignKey(p => p.DiedOn);

        base.OnModelCreating(modelBuilder);
    }
}