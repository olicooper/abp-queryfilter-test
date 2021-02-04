namespace AbpQueryFilterDemo.Posts
{
    public class PostListInput
    {
        public string Title { get; set; }

        /// <summary>
        /// Should the Post include the related Blog entity. This will perform a join query.
        /// </summary>
        public bool IncludeDetails { get; set; } = true;
        /// <summary>
        /// Disables the 'IsDeleted' query filter.
        /// </summary>
        public bool IgnoreSoftDelete { get; set; } = false;
        /// <summary>
        /// Use LINQ query syntax rather than method syntax 
        /// <para>
        ///     This only works with <see cref="IncludeDetails"/> == <see langword="true" />
        /// </para>
        /// </summary>
        public bool UseQuerySyntax { get; set; } = false;
    }
}
