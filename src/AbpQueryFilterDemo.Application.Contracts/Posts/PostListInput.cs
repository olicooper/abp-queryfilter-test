namespace AbpQueryFilterDemo.Posts
{
    public class PostListInput
    {
        public string Title { get; set; }
        public bool IncludeDetails { get; set; } = true;
    }
}
