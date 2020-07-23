﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;

namespace SlimBook.DataAccess.Repository
{
	public class RepositoryAsync<T> : IRepositoryAsync<T> where T : class
	{
		private readonly ApplicationDbContext _db;

		internal DbSet<T> dbSet;

		public RepositoryAsync(ApplicationDbContext db)
		{
			_db = db;
			this.dbSet = _db.Set<T>();
		}

		public async Task AddAsync(T entity)
		{
			await _db.AddAsync(entity);
		}

		public async Task<T> GetAsync(int id)
		{
			return await dbSet.FindAsync(id);
		}

		public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = null)
		{
			IQueryable<T> query = dbSet;
			if (filter != null)
			{
				query = query.Where(filter);
			}
			if (includeProperties != null)
			{
				foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					query = query.Include(includeProperty);
				}
			}
			if (orderBy != null)
			{
				return await orderBy(query).ToListAsync();
			}
			return await query.ToListAsync();
		}

		public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter = null, string includeProperties = null)
		{
			IQueryable<T> query = dbSet;
			if (filter != null)
			{
				query = query.Where(filter);
			}
			if (includeProperties != null)
			{
				foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					query = query.Include(includeProperty);
				}
			}
			return await query.FirstOrDefaultAsync();
		}

		public async Task RemoveAsync(int id)
		{
			T entity = await dbSet.FindAsync(id);
			await RemoveAsync(entity);
		}

		public async Task RemoveAsync(T entity)
		{
			await Task.Run(() => { dbSet.Remove(entity); });
		}

		public async Task RemoveRangeAsync(IEnumerable<T> entities)
		{
			await Task.Run(() => { dbSet.RemoveRange(entities); });
		}
	}
}