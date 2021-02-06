using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;

namespace AbpQueryFilterDemo
{
#pragma warning disable CS1584 // XML comment has syntactically incorrect cref attribute
#pragma warning disable CS1658 // Warning is overriding an error
    /// <summary>
    /// Used to filter entities by <see cref="IMultiTenant.TenantId"/>.
    /// <para>
    ///     This is used as a marker to enable/disable <see cref="IMultiTenant"/> filters specifically for the 
    ///     entitiy specified by <typeparamref name="TEntity"></typeparamref>.
    /// </para>
    /// <para>
    ///     <b>NOTE:</b> Please use this interface to mark your entities instead of the generic <see cref="IMultiTenant"/>
    /// </para>
    /// </summary>
    /// <remarks>
    ///     Example usage: <see cref="DataFilter.Disable{IMultiTenant{TEntity}}()"/>
    /// </remarks>
    public interface IMultiTenant<TEntity> : IMultiTenant where TEntity : class { }
#pragma warning restore CS1658 // Warning is overriding an error
#pragma warning restore CS1584 // XML comment has syntactically incorrect cref attribute
}
