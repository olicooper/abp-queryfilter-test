# ORIGINAL COUNT
```
.Call System.Linq.Queryable.Count(.Call System.Linq.Queryable.Where(
        .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
        '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    ($__ef_filter__p_0 || !.Call Microsoft.EntityFrameworkCore.EF.Property(
        $b,
        "IsDeleted")) && ($__ef_filter__p_1 || (System.Nullable`1[System.Guid]).Call Microsoft.EntityFrameworkCore.EF.Property(
        $b,
        "TenantId") == $__ef_filter__CurrentTenantId_2)
}
```

# CUSTOM COUNT
```
.Call System.Linq.Queryable.Count(.Call System.Linq.Queryable.Where(
        .Call System.Linq.Queryable.Where(
            .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
            '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
        '(.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    !$b.IsDeleted
}

.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    $b.TenantId == null
}
```

# ORIGINAL
```
.Call System.Linq.Queryable.Select(
    .Call System.Linq.Queryable.Where(
        .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
        '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
    '(.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    ($__ef_filter__p_0 || !.Call Microsoft.EntityFrameworkCore.EF.Property(
        $b,
        "IsDeleted")) && ($__ef_filter__p_1 || (System.Nullable`1[System.Guid]).Call Microsoft.EntityFrameworkCore.EF.Property(
        $b,
        "TenantId") == $__ef_filter__CurrentTenantId_2)
}

.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>(AbpQueryFilterDemo.Blogs.Blog $b)
{
    .Extension<Microsoft.EntityFrameworkCore.Query.IncludeExpression>
}
```

# CUSTOM 
```
.Call System.Linq.Queryable.Select(
    .Call System.Linq.Queryable.Where(
        .Call System.Linq.Queryable.Where(
            .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
            '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
        '(.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
    '(.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    !$b.IsDeleted
}

.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    $b.TenantId == null
}

.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>(AbpQueryFilterDemo.Blogs.Blog $b)
{
    .Extension<Microsoft.EntityFrameworkCore.Query.IncludeExpression>
}
```

# Custom Posts subquery
```
.Call System.Linq.Enumerable.Any(
    $x.Posts,
    .Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>)

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>(AbpQueryFilterDemo.Posts.Post $c) {
    !$c.IsDeleted
}
```

## oringal 2
```
.Call System.Linq.Queryable.Where(
    .Call Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(
        .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
        '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Object]>)),
    '(.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Object]>(AbpQueryFilterDemo.Blogs.Blog $x) {
    $x.Posts
}

.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $x) {
    .Call System.Linq.Enumerable.All(
        $x.Posts,
        .Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>)
}

.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>(AbpQueryFilterDemo.Posts.Post $x) {
    !$x.IsDeleted
}
```

## Custom 07-02 @ 12:30
With collection filtering, with call to AsQueryable()
```
.Call System.Linq.Queryable.Select(
    .Call System.Linq.Queryable.Where(
        .Call System.Linq.Queryable.Where(
            .Call System.Linq.Queryable.Where(
                .Call System.Linq.Queryable.Where(
                    .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
                    '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
                '(.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
            '(.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
        '(.Lambda #Lambda4<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
    '(.Lambda #Lambda5<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    !$b.IsDeleted
}

.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    !$b.IsDeleted
}

.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    $b.TenantId == null
}

.Lambda #Lambda4<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    .Call System.Linq.Queryable.Any(
        .Call System.Linq.Queryable.Where(
            .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
            '(.Lambda #Lambda6<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>)),
        '(.Lambda #Lambda7<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>))
}

.Lambda #Lambda5<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>(AbpQueryFilterDemo.Blogs.Blog $b)
{
    .Extension<Microsoft.EntityFrameworkCore.Query.IncludeExpression>
}

.Lambda #Lambda6<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>(AbpQueryFilterDemo.Posts.Post $p) {
    .Call Microsoft.EntityFrameworkCore.EF.Property(
        $b,
        "Id") != null && .Call System.Object.Equals(
        (System.Object).Call Microsoft.EntityFrameworkCore.EF.Property(
            $b,
            "Id"),
        (System.Object).Call Microsoft.EntityFrameworkCore.EF.Property(
            $p,
            "BlogId"))
}

.Lambda #Lambda7<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>(AbpQueryFilterDemo.Posts.Post $p) {
    !$p.IsDeleted
}
```

## Custom (fixed) 07-02 @ 14:22
With collection filtering On ISoftDelet AND IMultiTenant, with call to AsQueryable()
```
.Call System.Linq.Queryable.Select(
    .Call System.Linq.Queryable.Where(
        .Call System.Linq.Queryable.Where(
            .Call System.Linq.Queryable.Where(
                .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
                '(.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
            '(.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
        '(.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>)),
    '(.Lambda #Lambda4<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>))

.Lambda #Lambda1<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    !$b.IsDeleted
}

.Lambda #Lambda2<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    $b.TenantId == null
}

.Lambda #Lambda3<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,System.Boolean]>(AbpQueryFilterDemo.Blogs.Blog $b) {
    .Call System.Linq.Queryable.Any(
        .Call System.Linq.Queryable.Where(
            .Extension<Microsoft.EntityFrameworkCore.Query.QueryRootExpression>,
            '(.Lambda #Lambda5<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>)),
        '(.Lambda #Lambda6<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>))
}

.Lambda #Lambda4<System.Func`2[AbpQueryFilterDemo.Blogs.Blog,AbpQueryFilterDemo.Blogs.Blog]>(AbpQueryFilterDemo.Blogs.Blog $b)
{
    .Extension<Microsoft.EntityFrameworkCore.Query.IncludeExpression>
}

.Lambda #Lambda5<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>(AbpQueryFilterDemo.Posts.Post $p) {
    .Call Microsoft.EntityFrameworkCore.EF.Property(
        $b,
        "Id") != null && .Call System.Object.Equals(
        (System.Object).Call Microsoft.EntityFrameworkCore.EF.Property(
            $b,
            "Id"),
        (System.Object).Call Microsoft.EntityFrameworkCore.EF.Property(
            $p,
            "BlogId"))
}

.Lambda #Lambda6<System.Func`2[AbpQueryFilterDemo.Posts.Post,System.Boolean]>(AbpQueryFilterDemo.Posts.Post $p) {
    !$p.IsDeleted
}
```