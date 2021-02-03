# IgnoreAbpQueryFilter Test Project

Test project devoted to extending EntityFramework's `GlobalQueryFilters` functionality. It's a mess because it's a test :P

## Example
```csharp
(await ReadOnlyRepository.WithDetailsAsync(x => x.Blog))
    // This should allow deleted 'Blog' entities to be returned
    .IgnoreAbpQueryFilter(x => x.Blog);
```

## Running the project

1. Ensure that MySQL is installed and that your `appsettings.json` is correct
2. Run the `DbMigrations` project to create the database and seed the blog/post data
3. Run the `Web` project to see the sample data

## Main project files

* Application
    * BlogAppService.cs
    * PostAppService.cs
* Domain
    * Data/AppDataSeedContributor.cs
    * Extensions/AbpQueryableExtensions.cs
* EntityframeworkCore
    * AbpEntityFrameworkQueryableExtensions.cs
    * CustomAbpDbContext.cs
* Web
    * Pages/Index.cshtml
    * Pages/Index.js


## Useful links

### Docs
* https://docs.microsoft.com/en-us/ef/core/querying/filters
* https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/
* https://docs.microsoft.com/en-us/ef/core/modeling/

### Source code
* Entry point: [QueryCompilationContext.cs#L179-L210](https://github.com/dotnet/efcore/blob/0b3165096d6b55443fc06ae48404c2b037dd73e7/src/EFCore/Query/QueryCompilationContext.cs#L179-L210)
* QueryTranslationPreprocessor.Process method: [QueryTranslationPreprocessor.cs#L55-L69](https://github.com/dotnet/efcore/blob/46996600cb3f152e3e21ee4d07effdc516dbf4e9/src/EFCore/Query/QueryTranslationPreprocessor.cs#L55-L69)
* IQueryable `IgnoreQueryFilters()` parsing: [EntityFrameworkQueryableExtensions.cs#L2369-L2398](https://github.com/dotnet/efcore/blob/fcef1806e5990ffdbbd70eef094b58b3155a2571/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L2369-L2398) and action: [QueryableMethodNormalizingExpressionVisitor.cs#L217-L223](https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/EFCore/Query/Internal/QueryableMethodNormalizingExpressionVisitor.cs#L217-L223)
* IQueryable `Include()` parsing: [QueryableMethodNormalizingExpressionVisitor.cs#L80-L118](https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/EFCore/Query/Internal/QueryableMethodNormalizingExpressionVisitor.cs#L80-L118)
* NavigationExpandingExpressionVisitor `ApplyQueryFilter` method: [NavigationExpandingExpressionVisitor.cs#L1412-L1462](https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L1412-L1462)