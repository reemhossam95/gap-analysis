using ContentGapAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentGapAnalyzer.Infrastructure.Persistence.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();

        builder.Property(c => c.ChannelId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(c => c.ChannelId)
            .IsUnique()
            .HasDatabaseName("IX_Channels_ChannelId");

        // Alternate key matching existing AK_Channels_ChannelId constraint
        builder.HasAlternateKey(c => c.ChannelId)
            .HasName("AK_Channels_ChannelId");

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(c => c.ThumbnailUrl)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(c => c.SubscriberCount).IsRequired();
        builder.Property(c => c.TotalViews).IsRequired();
        builder.Property(c => c.VideoCount).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
        builder.Property(c => c.IsDeleted).IsRequired();

        // Navigation
        builder.HasMany(c => c.Videos)
            .WithOne(v => v.Channel)
            .HasForeignKey(v => v.ChannelId)
            .HasPrincipalKey(c => c.ChannelId)
            .HasConstraintName("FK_Videos_Channels_ChannelId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.AnalyticsSnapshots)
            .WithOne(a => a.Channel)
            .HasForeignKey(a => a.ChannelId)
            .HasPrincipalKey(c => c.ChannelId)
            .HasConstraintName("FK_AnalyticsSnapshots_Channels_ChannelId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.HistoricalStatistics)
            .WithOne(h => h.Channel)
            .HasForeignKey(h => h.ChannelId)
            .HasPrincipalKey(c => c.ChannelId)
            .HasConstraintName("FK_HistoricalStatistics_Channels_ChannelId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
