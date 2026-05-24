using Microsoft.EntityFrameworkCore;

namespace IntercomService.Data;

public sealed class IntercomDbContext(DbContextOptions<IntercomDbContext> options) : DbContext(options)
{
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();

    public DbSet<MemberEntity> Members => Set<MemberEntity>();

    public DbSet<TeamMemberEntity> TeamMembers => Set<TeamMemberEntity>();

    public DbSet<TopicEntity> Topics => Set<TopicEntity>();

    public DbSet<TransportEventEntity> TransportEvents => Set<TransportEventEntity>();

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    public DbSet<OAuthStateEntity> OAuthStates => Set<OAuthStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamEntity>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(x => x.TeamId);
        });

        modelBuilder.Entity<MemberEntity>(e =>
        {
            e.ToTable("Members");
            e.HasKey(x => x.MemberId);
            e.HasIndex(x => new { x.Issuer, x.Subject }).IsUnique();
        });

        modelBuilder.Entity<TeamMemberEntity>(e =>
        {
            e.ToTable("TeamMembers");
            e.HasKey(x => new { x.TeamId, x.MemberId });
            e.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
        });

        modelBuilder.Entity<TopicEntity>(e =>
        {
            e.ToTable("Topics");
            e.HasKey(x => x.TopicId);
            e.HasIndex(x => x.TeamId);
        });

        modelBuilder.Entity<TransportEventEntity>(e =>
        {
            e.ToTable("TransportEvents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TeamId, x.Seq }).IsUnique();
            e.HasIndex(x => new { x.TeamId, x.ClientEventId }).IsUnique();
            e.HasIndex(x => new { x.TeamId, x.TopicId, x.Seq });
        });

        modelBuilder.Entity<RefreshTokenEntity>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
        });

        modelBuilder.Entity<OAuthStateEntity>(e =>
        {
            e.ToTable("OAuthStates");
            e.HasKey(x => x.State);
        });
    }
}
