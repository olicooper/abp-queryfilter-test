# Custom Query Filters Test Project

A test project devoted to replacing Entity Framework Core's global query filters with a more flexible and controllable solution. This is built on top of _ABP Framework_ and runs on .NET 5. P.S. It's a mess because it's a test :stuck_out_tongue:

A list of current issues can be [found below](#current-issues). **If you feel like helping out, please create a PR**

## The problem

Filters such as soft deletion can be configured on entites by using global query filters e.g. `ModelBuilder.Entity<Post>.HasQueryFilter(p => !p.IsDeleted)` which will ensure that only non-deleted posts are returned by appending the filter to every query.

There are times when you need to return deleted entities, and only way to achieve this (with the built-in query filter functionality) is to add a property to your `DbContext` which is evaluated at runtime. This causes databases providers to not index the query whilst also increasing the query complexity which can hurt performance.

A common example of when you might not want to have and entitiy automatically filtered is when it is a child of another entity. 

```csharp
class Blog
{
    public ICollection<Post> Posts { get; set; }
}

class Post
{
    public Blog Blog { get; set; }
}
```

Using the example above, if you had a `Blog` entity which contains a collection of `Posts` (`ICollection<Post> Posts`) and some of those posts are deleted, the `Blog.Posts` property will only contain the non-deleted `Post` entities due to gloabl query filters being applied to the `Posts`.

Similarly, if you want to return *ALL* `Posts` and you have the `Blog` as a property of the `Post` i.e. `Post.Blog`, only posts belonging to non-deleted blogs will be returned even if they themselves are not deleted!

Microsoft say this is to ensure referential integrity, but this is only applicable at a database level and will be enforced at that level anyway. It leads to unexpected results (which are hard to spot) and there are also bugs with the implementation (try running `Count()` on the IQueryable before returning the list - you'll get less results than count says you should have!). In my opinion, query filters are business rules, and sometimes you need to ignore those rules when manipulating data. Microsoft's current implementation is also too restrictive for any real-world application.

**The goal is simple:** <u>Ignore</u> the global query filters for specific entities by using ABP's `DataFilters` i.e. `DataFilter.Disable<ISoftDelete<Blog>>()` unless the IQueryable extension method `IQueryable.IgnoreAbpQueryFilters()` has been used, which stops the filters being applied on a per-query basis.

This solution should fix various issues which have been raised in the past - see [linked issues](#linked-issues). It could even be extended to address [current query filter limitations](https://docs.microsoft.com/en-us/ef/core/querying/filters#limitations), create dynamic filters and allow ABP to leverage [EF6 compiled models](https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-6.0/plan#compiled-models).

I am also planning on integrating [these changes](https://github.com/abpframework/abp/compare/dev...olicooper:pr/data-filtering-updates) to ABP's DataFilters to facilitate this project.

## The method

1. Disable the use of global query filters (the ABP `ModelBuilder.OnModelCreating()` query filter generation code is bypassed, so no calls to `Entity.UseQueryFilter()`)
2. Intercept the query compilation and append the appropriate data filters

### Usage example
```csharp
using (DataFilter.Enable<ISoftDelete())
using (DataFilter.Disable<ISoftDelete<Blog>>())
{
    // This should return a list of non-deleted posts with the 'Blog' populated
    // even if the Blog is marked as deleted.
    var postWithDeletedBlog = PostsRepository
        .Include(x => x.Blog)
        .ToListAsync();
}
```

## Current issues

Replacing query filters sounds simpler than it actually is and there are many hurdles to overcome.

The following known issues (non-exhaustive) are present in the solution:

* :x: Collection filtering doesn't currently work because EF performs multiple calls to the database when loading collections when `QuerySplittingBehavior.SplitQuery` is used.
* :x: Lazy/Eager/Implicit loading isn't considered nor is `IgnoreAutoIncludes`, entity tracking and [skip navigations](https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-5.0/plan#many-to-many-navigation-properties-aka-skip-navigations) etc.
* :x: Different DB providers implement things differently - Only Relational EF provider is currently implemented.
* :heavy_check_mark: Queries are cached so updating queries is difficult when filters change - ~~If filters change at runtime, they don't take effect.~~
* :x: Filters shouldn't be applied if the navigation items are not going to be loaded.


## Running the project

1. Ensure that MySQL is installed and that your `appsettings.json` is correct
2. Run the `DbMigrations` project to create the database and seed the blog/post data
3. Run the `Web` project to see the sample data. You can modify `Pages/index.js` to change which queries are run.

For more info about the ABP project, you can visit [docs.abp.io](https://docs.abp.io).

## Main project files

* Application
    * BlogAppService.cs
    * PostAppService.cs
* Domain
    * Data/AppDataSeedContributor.cs
    * Extensions/AbpQueryableExtensions.cs
* Domain.Shared
    * IMultiTenantExtension.cs
    * ISoftDeleteExtension.cs
* EntityframeworkCore
    * CustomAbpDbContext.cs
    * Repositories/PostRepository.cs
* Web
    * Pages/Index.cshtml
    * Pages/Index.js

## Useful links

### Docs
* https://docs.microsoft.com/en-us/ef/core/querying/filters/
* https://docs.microsoft.com/en-us/ef/core/querying/related-data/
* https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/
* https://docs.microsoft.com/en-us/ef/core/modeling/
* https://docs.microsoft.com/en-us/ef/core/providers/writing-a-provider/
* https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expressiontype?view=net-5.0
* https://docs.microsoft.com/en-us/dotnet/csharp/expression-trees-interpreting/
* https://docs.microsoft.com/en-us/archive/blogs/mattwar/linq-building-an-iqueryable-provider-series/

### Source code
* Entry point: [QueryCompilationContext.cs#L179-L210](https://github.com/dotnet/efcore/blob/0b3165096d6b55443fc06ae48404c2b037dd73e7/src/EFCore/Query/QueryCompilationContext.cs#L179-L210)
* QueryTranslationPreprocessor.Process method: [QueryTranslationPreprocessor.cs#L55-L69](https://github.com/dotnet/efcore/blob/46996600cb3f152e3e21ee4d07effdc516dbf4e9/src/EFCore/Query/QueryTranslationPreprocessor.cs#L55-L69)
* IQueryable `IgnoreQueryFilters()` parsing: [EntityFrameworkQueryableExtensions.cs#L2369-L2398](https://github.com/dotnet/efcore/blob/fcef1806e5990ffdbbd70eef094b58b3155a2571/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L2369-L2398) and action: [QueryableMethodNormalizingExpressionVisitor.cs#L217-L223](https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/EFCore/Query/Internal/QueryableMethodNormalizingExpressionVisitor.cs#L217-L223)
* IQueryable `Include()` parsing: [QueryableMethodNormalizingExpressionVisitor.cs#L80-L118](https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/EFCore/Query/Internal/QueryableMethodNormalizingExpressionVisitor.cs#L80-L118)
* NavigationExpandingExpressionVisitor `ApplyQueryFilter` method: [NavigationExpandingExpressionVisitor.cs#L1412-L1462](https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L1412-L1462)

### Linked issues
* https://github.com/dotnet/efcore/issues/11691
* https://github.com/dotnet/efcore/issues/21093#issuecomment-640108508
* https://github.com/dotnet/efcore/issues/8576
* https://github.com/abpframework/abp/issues/6680
* https://github.com/abpframework/abp/issues/7482
* https://github.com/abpframework/abp/issues/1181
* https://github.com/abpframework/abp/issues/5650