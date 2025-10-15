using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderTrace.Core.Entities;

namespace OrderTrace.Infrastructure.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Gateway)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.ResponseCode)
            .HasMaxLength(10);

        builder.Property(t => t.ResponseMessage)
            .HasMaxLength(200);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(t => t.Payment)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
