namespace CloudStorageORM.IntegrationTests.Azure
{
    using CloudStorageORM.IntegrationTests.Azure.DbContext;
    using CloudStorageORM.IntegrationTests.Azure.Models;
    using CloudStorageORM.Options;
    using global::CloudStorageORM.Enums;
    using global::CloudStorageORM.Extensions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;

    public class CloudStorageAzureTests
    {
        private readonly ServiceProvider _rootProvider;

        public CloudStorageAzureTests()
        {
            var cloudStorageOptions = new CloudStorageOptions
            {
                Provider = CloudProvider.Azure,
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = $"test-container-{Guid.NewGuid()}"
            };

            var efServices = new ServiceCollection();
            efServices.AddEntityFrameworkCloudStorageORM(cloudStorageOptions);
            var internalProvider = efServices.BuildServiceProvider();

            var services = new ServiceCollection();

            services.AddSingleton(cloudStorageOptions);
            services.AddDbContext<IntegrationTestDbContext>(options =>
            {
                options.UseInternalServiceProvider(internalProvider);
                options.UseCloudStorageORM(cfg =>
                {
                    cfg.Provider = cloudStorageOptions.Provider;
                    cfg.ConnectionString = cloudStorageOptions.ConnectionString;
                    cfg.ContainerName = cloudStorageOptions.ContainerName;
                });
            });

            _rootProvider = services.BuildServiceProvider();
        }

        #region Add

        [Fact]
        public async Task Add_ShouldPersistUser()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User { Id = Guid.NewGuid(), Name = "Add", Email = "add@test.com" };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var reloaded = await context.Users.FindAsync(user.Id);
            reloaded.ShouldNotBeNull();
            reloaded!.Name.ShouldBe("Add");
        }

        [Fact]
        public async Task AddAsync_ShouldPersistUser()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User { Id = Guid.NewGuid(), Name = "AddAsync", Email = "addasync@test.com" };

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var reloaded = await context.Users.FindAsync(user.Id);
            reloaded.ShouldNotBeNull();
            reloaded!.Email.ShouldBe("addasync@test.com");
        }

        [Fact]
        public async Task AddRange_ShouldPersistUsers()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "A", Email = "a@a.com" },
                new User { Id = Guid.NewGuid(), Name = "B", Email = "b@b.com" },
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var all = await context.Users.ToListAsync();
            all.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task AddRangeAsync_ShouldPersistUsers()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "X", Email = "x@x.com" },
                new User { Id = Guid.NewGuid(), Name = "Y", Email = "y@y.com" },
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            var all = await context.Users.ToListAsync();
            all.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        #endregion

        #region Attach

        [Fact]
        public async Task Attach_ShouldTrackEntityWithoutSaving()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Attach User",
                Email = "attach@user.com"
            };

            context.Users.Attach(user);

            context.ChangeTracker.Entries<User>().First().State.ShouldBe(EntityState.Unchanged);
        }

        [Fact]
        public async Task AttachRange_Params_ShouldTrackEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "Attach 1", Email = "a1@test.com" },
                new User { Id = Guid.NewGuid(), Name = "Attach 2", Email = "a2@test.com" }
            };

            context.Users.AttachRange(users);

            var tracked = context.ChangeTracker.Entries<User>().ToList();
            tracked.Count.ShouldBe(2);
            tracked.All(e => e.State == EntityState.Unchanged).ShouldBeTrue();
        }

        [Fact]
        public async Task AttachRange_Enumerable_ShouldTrackEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "Attach A", Email = "aa@test.com" },
                new User { Id = Guid.NewGuid(), Name = "Attach B", Email = "bb@test.com" }
            };

            context.Users.AttachRange(users);

            var tracked = context.ChangeTracker.Entries<User>().ToList();
            tracked.Count.ShouldBe(2);
            tracked.All(e => e.State == EntityState.Unchanged).ShouldBeTrue();
        }


        #endregion

        #region Find    

        [Fact]
        public async Task Find_ShouldReturnEntity_WhenExists()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Find User",
                Email = "find@user.com"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = context.Users.Find(user.Id);
            result.ShouldNotBeNull();
            result!.Email.ShouldBe("find@user.com");
        }

        [Fact]
        public async Task FindAsync_WithParams_ShouldReturnEntity_WhenExists()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "FindAsync User",
                Email = "findasync@user.com"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await context.Users.FindAsync(user.Id);
            result.ShouldNotBeNull();
            result!.Name.ShouldBe("FindAsync User");
        }

        [Fact]
        public async Task FindAsync_WithArrayAndToken_ShouldReturnEntity_WhenExists()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Token User",
                Email = "token@user.com"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await context.Users.FindAsync(new object[] { user.Id }, CancellationToken.None);
            result.ShouldNotBeNull();
            result!.Name.ShouldBe("Token User");
        }

        #endregion

        #region Remove

        [Fact]
        public async Task Remove_ShouldDeleteEntity()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Delete Me",
                Email = "delete@user.com"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            var result = await context.Users.FindAsync(user.Id);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task RemoveRange_WithParams_ShouldDeleteAll()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user1 = new User { Id = Guid.NewGuid(), Name = "User1", Email = "1@x.com" };
            var user2 = new User { Id = Guid.NewGuid(), Name = "User2", Email = "2@x.com" };

            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            context.Users.RemoveRange(user1, user2);
            await context.SaveChangesAsync();

            var result1 = await context.Users.FindAsync(user1.Id);
            var result2 = await context.Users.FindAsync(user2.Id);

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        [Fact]
        public async Task RemoveRange_WithEnumerable_ShouldDeleteAll()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "User3", Email = "3@x.com" },
                new User { Id = Guid.NewGuid(), Name = "User4", Email = "4@x.com" }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            context.Users.RemoveRange((IEnumerable<User>)users);
            await context.SaveChangesAsync();

            foreach (var user in users)
            {
                var result = await context.Users.FindAsync(user.Id);
                result.ShouldBeNull();
            }
        }


        #endregion

        #region Update

        [Fact]
        public async Task Update_ShouldModifyExistingEntity()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User { Id = Guid.NewGuid(), Name = "Original", Email = "original@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            user.Name = "Updated";
            context.Users.Update(user);
            await context.SaveChangesAsync();

            var reloaded = await context.Users.FindAsync(user.Id);
            reloaded.ShouldNotBeNull();
            reloaded!.Name.ShouldBe("Updated");
        }

        [Fact]
        public async Task UpdateRange_Params_ShouldModifyEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "U1", Email = "u1@test.com" },
                new User { Id = Guid.NewGuid(), Name = "U2", Email = "u2@test.com" },
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            users[0].Name = "Updated1";
            users[1].Name = "Updated2";
            context.Users.UpdateRange(users[0], users[1]);
            await context.SaveChangesAsync();

            var list = await context.Users.ToListAsync();
            list.First(x => x.Id == users[0].Id).Name.ShouldBe("Updated1");
            list.First(x => x.Id == users[1].Id).Name.ShouldBe("Updated2");
        }

        [Fact]
        public async Task UpdateRange_Enumerable_ShouldModifyEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "L1", Email = "l1@test.com" },
                new User { Id = Guid.NewGuid(), Name = "L2", Email = "l2@test.com" },
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            users[0].Email = "changed1@test.com";
            users[1].Email = "changed2@test.com";

            context.Users.UpdateRange(users);
            await context.SaveChangesAsync();

            var list = await context.Users.ToListAsync();
            list.First(x => x.Id == users[0].Id).Email.ShouldBe("changed1@test.com");
            list.First(x => x.Id == users[1].Id).Email.ShouldBe("changed2@test.com");
        }

        #endregion

        #region List

        [Fact]
        public async Task ToList_ShouldReturnAllEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            context.Users.Add(new User { Id = Guid.NewGuid(), Name = "List", Email = "list@test.com" });
            await context.SaveChangesAsync();

            var list = context.Users.ToList();
            list.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task ToListAsync_ShouldReturnAllEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            context.Users.Add(new User { Id = Guid.NewGuid(), Name = "AsyncList", Email = "alist@test.com" });
            await context.SaveChangesAsync();

            var list = await context.Users.ToListAsync();
            list.ShouldContain(u => u.Name == "AsyncList");
        }

        #endregion

        [Fact]
        public async Task AsAsyncEnumerable_ShouldIterateAllEntities()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            context.Users.Add(new User { Id = Guid.NewGuid(), Name = "AsyncEnum", Email = "asyncenum@test.com" });
            await context.SaveChangesAsync();

            var count = 0;
            await foreach (var user in context.Users.AsAsyncEnumerable())
            {
                count++;
            }

            count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task AsQueryable_ShouldSupportLinqQueries()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var id = Guid.NewGuid();
            context.Users.Add(new User { Id = id, Name = "LinqUser", Email = "linq@test.com" });
            await context.SaveChangesAsync();

            var queried = context.Users.AsQueryable().FirstOrDefault(u => u.Id == id);
            queried.ShouldNotBeNull();
        }

        [Fact]
        public void Entry_ShouldReturnCorrectEntityEntry()
        {
            using var scope = _rootProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();

            var user = new User { Id = Guid.NewGuid(), Name = "Entry", Email = "entry@test.com" };
            var entry = context.Users.Entry(user);

            entry.Entity.ShouldBe(user);
        }

    }
}