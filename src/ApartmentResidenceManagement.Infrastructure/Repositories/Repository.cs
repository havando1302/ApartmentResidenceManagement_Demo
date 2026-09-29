using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(entity => EF.Property<int>(entity, "Id") == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }
}
