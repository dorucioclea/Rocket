﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Aiwins.Rocket.Domain.Entities;
using Aiwins.Rocket.Domain.Repositories.EntityFrameworkCore;
using Aiwins.Rocket.EntityFrameworkCore;
using Aiwins.Blogging.EntityFrameworkCore;

namespace Aiwins.Blogging.Posts
{
    public class EfCorePostRepository : EfCoreRepository<IBloggingDbContext, Post, Guid>, IPostRepository
    {
        public EfCorePostRepository(IDbContextProvider<IBloggingDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        public async Task<List<Post>> GetPostsByBlogId(Guid id)
        {
            return await DbSet.Where(p => p.BlogId == id).OrderByDescending(p=>p.CreationTime).ToListAsync();
        }

        public async Task<Post> GetPostByUrl(Guid blogId, string url)
        {
            var post = await DbSet.FirstOrDefaultAsync(p => p.BlogId == blogId && p.Url == url);

            if (post == null)
            {
                throw new EntityNotFoundException(typeof(Post), nameof(post));
            }

            return post;
        }

        public async Task<List<Post>> GetOrderedList(Guid blogId,bool descending = false)
        {
            if (!descending)
            {
                return await DbSet.Where(x=>x.BlogId==blogId).OrderByDescending(x => x.CreationTime).ToListAsync();
            }
            else
            {
                return await DbSet.Where(x => x.BlogId == blogId).OrderBy(x => x.CreationTime).ToListAsync();
            }

        }

        public override IQueryable<Post> WithDetails()
        {
            return GetQueryable().IncludeDetails();
        }
    }
}