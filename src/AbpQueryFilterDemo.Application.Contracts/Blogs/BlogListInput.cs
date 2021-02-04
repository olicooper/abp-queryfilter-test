namespace AbpQueryFilterDemo.Blogs
{
    public class BlogListInput
    {
        public string Name { get; set; }

        /// <summary>
        /// Should the Post include the related Blog entity. This will perform a join query.
        /// </summary>
        public bool IncludeDetails { get; set; } = true;
        /// <summary>
        /// Disables the 'IsDeleted' query filter.
        /// </summary>
        public bool IgnoreSoftDelete { get; set; } = false;
    }
}
