using System.Linq.Expressions;
using ContentGapAnalyzer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentGapAnalyzer.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

public async Task<IReadOnlyList<T>> FindAsync(
    Expression<Func<T, bool>> predicate,
    CancellationToken cancellationToken = default) // الإبقاء على الديفولت
{
    // استخدام CancellationToken.None إذا كان الكانسيليشن توكن فارغاً
    var token = cancellationToken == default ? CancellationToken.None : cancellationToken;
    
    return await _dbSet.Where(predicate).ToListAsync(token);
}
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public IQueryable<T> Query() => _dbSet.AsQueryable();
}
