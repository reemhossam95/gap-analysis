using ContentGapAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentGapAnalyzer.Infrastructure.Persistence.Configurations;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).UseIdentityColumn();

        builder.Property(v => v.VideoId)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(v => v.VideoId)
            .HasDatabaseName("IX_Videos_VideoId");

        builder.Property(v => v.ChannelId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(v => v.ChannelId)
            .HasDatabaseName("IX_Videos_ChannelId");

        builder.Property(v => v.Title)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(v => v.Description)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.ThumbnailUrl)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(v => v.ViewCount).IsRequired();
        builder.Property(v => v.LikeCount).IsRequired();
        builder.Property(v => v.CommentCount).IsRequired();
        builder.Property(v => v.WatchTimeMinutes).IsRequired();
        builder.Property(v => v.ClickThroughRate).IsRequired();
        builder.Property(v => v.AverageViewDuration).IsRequired();
        builder.Property(v => v.PublishedAt).IsRequired();

        builder.Property(v => v.Category)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
        builder.Property(v => v.IsDeleted).IsRequired();

        // Relationship to Channel — FK on ChannelId (string), not Id
        builder.HasOne(v => v.Channel)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.ChannelId)
            .HasPrincipalKey(c => c.ChannelId)
            .HasConstraintName("FK_Videos_Channels_ChannelId")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Relationship to GapReports
        builder.HasMany(v => v.GapReports)
            .WithOne(g => g.Video)
            .HasForeignKey(g => g.VideoId)
            .HasPrincipalKey(v => v.VideoId)
            .HasConstraintName("FK_GapReports_Videos_VideoId")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
