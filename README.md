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