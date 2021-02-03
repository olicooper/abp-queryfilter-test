using AbpQueryFilterDemo.Blogs;
using AbpQueryFilterDemo.Posts;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public static class AbpQueryFilterDemoDbContextModelCreatingExtensions
    {
        public static void ConfigureAbpQueryFilterDemo(this ModelBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Entity<Post>(b =>
            {
                b.ToTable(AbpQueryFilterDemoConsts.DbTablePrefix + "Posts", AbpQueryFilterDemoConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.Title).HasMaxLength(200).IsRequired();

                b.HasOne(x => x.Blog).WithMany(x => x.Posts)
                    .IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Blog>(b =>
            {
                b.ToTable(AbpQueryFilterDemoConsts.DbTablePrefix + "Blogs", AbpQueryFilterDemoConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            });
        }
    }
}