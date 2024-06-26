https://www.milanjovanovic.tech/blog/implementing-soft-delete-with-ef-core

o delete or not to delete, that is the question (pun intended).

The traditional way to remove information in a database is through a "hard delete." A hard delete permanently erases a record from the database table. While this seems straightforward, it presents a significant risk: once that data is gone, it's gone for good.

Instead of physically removing a record, a soft delete marks it as deleted, usually by setting a flag like IsDeleted to true. The record remains in the database, but it's effectively hidden from regular application queries.

Today, we'll dive into the details of how to implement soft deletes using EF Core. We'll discuss global query filters, explore efficient ways to handle soft-deleted data, and weigh the trade-offs.

What Is a Soft Delete?
A soft delete is a data persistence strategy that prevents the permanent deletion of records from your database. Instead of removing data from the database, a flag is set on the record, indicating it as "deleted."

This approach allows the application to ignore these records during normal queries. However, you can restore these records if necessary. Soft delete is also practical if you want to keep foreign key constraints in place. Soft delete is a "non-destructive" operation in contrast with hard delete, where data is completely removed from the database.

A hard delete uses the SQL DELETE statement:

DELETE FROM bookings.Reviews
WHERE Id = @BookingId;
A soft delete, on the other hand, uses an UPDATE statement:

UPDATE bookings.Reviews
SET IsDeleted = 1, DeletedOnUtc = @UtcNow
WHERE Id = @BookingId;
The data is still present in the database, and the operation can be undone.

But you need to remember to filter out soft-deleted data when querying the database:

SELECT *
FROM bookings.Reviews
WHERE IsDeleted = 0;
Let's see how we can implement soft delete with EF Core.

Soft Deletes Using EF Core Interceptors
​EF Core interceptors provide a powerful mechanism for intercepting and modifying database operations. For example, you can intercept the saving changes operation to implement soft delete functionality.

Let's create an ISoftDeletable marker interface to represent soft-deletable entities:

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedOnUtc { get; set; }
}
The entities that should support soft delete will implement this interface. You will need to apply the respective database migration to create these columns.

The next component we need is a SaveChangesInterceptor, which allows us to hook into the SavingChangesAsync (or SavingChanges) method. We can access the ChangeTracker and look for entries that implement ISoftDeletable and are flagged for deletion. We can figure this out by checking if the entity state is EntityState.Deleted.

When we find the entities flagged for deletion, we loop through them and update their state to EntityState.Modified. You should also set the respective values for the IsDeleted and DeletedOnUtc properties. This will cause EF to generate an UPDATE operation instead of a DELETE operation.

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(
                eventData, result, cancellationToken);
        }

        IEnumerable<EntityEntry<ISoftDeletable>> entries =
            eventData
                .Context
                .ChangeTracker
                .Entries<ISoftDeletable>()
                .Where(e => e.State == EntityState.Deleted);

        foreach (EntityEntry<ISoftDeletable> softDeletable in entries)
        {
            softDeletable.State = EntityState.Modified;
            softDeletable.Entity.IsDeleted = true;
            softDeletable.Entity.DeletedOnUtc = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
This approach ensures that all delete operations across the application respect the soft delete policy.

You'll need to register the SoftDeleteInterceptor with dependency injection and configure it with the ApplicationDbContext.

services.AddSingleton<SoftDeleteInterceptor>();

services.AddDbContext<ApplicationDbContext>(
    (sp, options) => options
        .UseSqlServer(connectionString)
        .AddInterceptors(
            sp.GetRequiredService<SoftDeleteInterceptor>()));
If you want to learn more, here's an article with a few practical use cases for EF Core interceptors.

Automatically Filtering Soft-Deleted Data
To ensure that soft-deleted records are automatically excluded from queries, we can use EF Core global query filters. We can apply query filters to entities using the OnModelCreating method to automatically exclude records marked as deleted. This feature dramatically simplifies writing queries.

Here's how to configure the soft delete query filter:

public sealed class ApplicationDbContext(
    DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>().HasQueryFilter(r => !r.IsDeleted);
    }
}
A limitation is you can't have more than one query filter configured per entity.

However, it's sometimes useful to explicitly include soft-deleted records. You can achieve this using the IgnoreQueryFilters method.

dbContex.Reviews
    .IgnoreQueryFilters()
    .Where(r => r.ApartmentId == apartmentId)
    .ToList();
Faster Queries Using Filtered Index
To enhance query performance, especially in tables with a significant number of soft-deleted records, you can use filtered indexes. A filtered index only includes records that meet the specified criteria. This reduces the index size and improves query execution times for operations that exclude filtered records. Most popular databases support filtered indexes.

Here's how you can configure a filtered index with EF Core:

public sealed class ApplicationDbContext(
    DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>().HasQueryFilter(r => !r.IsDeleted);

        modelBuilder.Entity<Review>()
            .HasIndex(r => r.IsDeleted)
            .HasFilter("IsDeleted = 0");
    }
}
The HasFilter method accepts the SQL filter for records that will be included in the index.

You can also create a filtered index using SQL:

CREATE INDEX IX_Reviews_IsDeleted
ON bookings.Reviews (IsDeleted)
WHERE IsDeleted = 0;
You can learn more about filtered indexes from the documentation:

​SQL Server filtered index​
​PostgreSQL partial index​
Do You Really Need Soft Deletes?
It's worthwhile to think through if you even need to soft delete records.

In enterprise systems, you're typically not thinking about "deleting" data. There are business concepts that don't involve deleting data. A few examples are canceling an order, refunding a payment, or voiding an invoice. These "destructive" operations return the system to a previous state. But from a business perspective, you aren't really deleting data.

Soft deletes are helpful if there is a risk of accidental deletion. They allow you to easily restore soft-deleted records.

In any case, consider if soft deletes make sense from a business perspective.

Takeaway
Soft deletes offer a valuable safety net for data recovery and can enhance historical data tracking. However, it's crucial to assess whether they truly align with your application's specific requirements. Consider factors like the importance of deleted data recovery, any auditing needs, and your industry's regulations. Creating a filtered index can improve query performance on tables with soft-deleted records.

If you decide that soft deletes are a good fit, EF Core provides the tools necessary for a streamlined implementation.

Thanks for reading, and I'll see you next week!