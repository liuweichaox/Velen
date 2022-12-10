﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Velen.Domain.Customers;
using Velen.Domain.SeedWork;
using Velen.Infrastructure.Processing.InternalCommands;
using Velen.Infrastructure.Processing.Outbox;

namespace Velen.Infrastructure.Domain
{
    public class AppDbContext : DbContext
    {
        private IMediator _mediator;
        public AppDbContext(DbContextOptions<AppDbContext> options,IMediator mediator) : base(options)
        {
            _mediator = mediator;
        }

        public DbSet<InternalCommand> InternalCommands { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
        public async  Task DispatchEventsAsync()
        {
            var domainEntities = ChangeTracker
                .Entries<Entity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            domainEntities.ToList()
                .ForEach(entity => entity.Entity.ClearDomainEvents());

            var tasks = domainEvents
                .Select(async (domainEvent) =>
                {
                    await _mediator.Publish(domainEvent);
                });

            await Task.WhenAll(tasks);
        }
    }
}