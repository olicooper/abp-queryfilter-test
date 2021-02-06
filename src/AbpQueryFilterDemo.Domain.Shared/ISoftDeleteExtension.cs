using Volo.Abp;
using Volo.Abp.Data;

namespace AbpQueryFilterDemo
{
#pragma warning disable CS1584 // XML comment has syntactically incorrect cref attribute
#pragma warning disable CS1658 // Warning is overriding an error
    /// <summary>
    /// Used to standardize soft deleting entities.
    /// Soft-delete entities are not actually deleted,
    /// marked as IsDeleted = true in the database,
    /// but can not be retrieved to the application normally.
    /// <para>
    ///     This interface (<see cref="ISoftDelete{TEntity}"/>) is used as a marker to 
    ///     enable/disable <see cref="ISoftDelete"/> filters specifically for the 
    ///     entitiy specified by <typeparamref name="TEntity"></typeparamref>.
    /// </para>
    /// <para>
    ///     <b>NOTE:</b> Please use this interface to mark your entities 
    ///     instead of the generic <see cref="ISoftDelete"/>
    /// </para>
    /// </summary>
    /// <remarks>
    ///     Example usage: <see cref="DataFilter.Disable{ISoftDelete{TEntity}}()"/>
    /// </remarks>
    public interface ISoftDelete<TEntity> : ISoftDelete where TEntity : class { }
#pragma warning restore CS1658 // Warning is overriding an error
#pragma warning restore CS1584 // XML comment has syntactically incorrect cref attribute
}
