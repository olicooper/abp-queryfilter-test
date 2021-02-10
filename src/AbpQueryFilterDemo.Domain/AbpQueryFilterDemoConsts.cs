namespace AbpQueryFilterDemo
{
    public static class AbpQueryFilterDemoConsts
    {
        public const string DbTablePrefix = "App";

        public const string DbSchema = null;

        /// <summary>
        /// If <see langword="false"/>, custom filter logic will be bypassed and the default query filters will be configuired by ABP.
        /// Default: true
        /// You'll still be able to inspect the query in <see cref="CustomQueryTranslationPreprocessor.Process(Expression)"/>.
        /// </summary>
        public static bool UseCustomFiltering => true;
        /// <summary>
        /// Determines if the custom filters should be applied to navigation items or just the root entity for the query.
        /// Default: true
        /// </summary>
        public static bool ApplyFiltersToNavigations => UseCustomFiltering && true;
        /// <summary>
        /// Bypass the <see cref="RelationalQueryTranslationPreprocessor.Process(Expression)"/> method by setting this false. 
        /// This manually calls the methods found in <see cref="QueryTranslationPreprocessor.Process(Expression)"/> so you can see it being translated.
        /// Default: false
        /// </summary>
        public static bool ExposePreprocessorProcessMethods => false;

        /// <summary>
        /// This will stop the same query being executed for the 'Count()' operation which helps with debugging.
        /// Default: false
        /// </summary>
        public static bool ExecuteCountQuery => false;
    }
}
