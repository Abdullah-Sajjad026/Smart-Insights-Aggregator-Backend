using System.Linq.Expressions;

namespace SmartInsights.Application.Interfaces;

public interface IRepository<T> where T : class
{
    // Query
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    
    // Query with includes
    Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);
    Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<List<T>> FindAsync(
        Expression<Func<T, bool>> predicate, 
        params Expression<Func<T, object>>[] includes);
    
    // Commands
    Task<T> AddAsync(T entity);
    Task<List<T>> AddRangeAsync(List<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateRangeAsync(List<T> entities);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(List<T> entities);
    
    // Aggregate
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}
