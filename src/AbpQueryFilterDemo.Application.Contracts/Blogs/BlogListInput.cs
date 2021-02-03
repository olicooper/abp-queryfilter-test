namespace AbpQueryFilterDemo.Blogs
{
    public class BlogListInput
    {
        public string Name { get; set; }
        public bool IncludeDetails { get; set; } = true;
    }
}
