// ReSharper disable once CheckNamespace

namespace Rene.Utils.Db.IntegrationTest
{
    using MediatR;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System.Data.Common;
    using Testcontainers.MsSql;
    using Tests.Common;

    [CollectionDefinition("TestContainerCollection")]
    public class TestContainerCollection : ICollectionFixture<MsSqlContainerFixture>;

    /// <summary>
    /// Fixture for MsSqlContainer. It creates a container with a backup of AdventureWorksLT2022 database.
    /// </summary>
    public class MsSqlContainerFixture : IAsyncLifetime
    {
        private const string LocalBackupPath = @"resources\backups";
        private const string TargetBackupPath = "/backups/mssql/";
        private const string BackupFileName = "AdventureWorksLT2022.bak";

        private const string Password = "Password123";
        private readonly MsSqlContainer _container;

        internal TestingDbContext? Context;

        public MsSqlContainerFixture()
        {
            var backupPath = Path.Combine(GetSolutionDirectoryInfo(), LocalBackupPath, BackupFileName);
            var info = new FileInfo(backupPath);

            _container = new MsSqlBuilder()
                .WithPassword(Password)
                .WithResourceMapping(info, TargetBackupPath)
                .Build();
        }

        internal IMediator? Mediator { get; private set; }

        public async Task InitializeAsync()
        {
            var backupPath = Path.Combine(TargetBackupPath, BackupFileName);
            var restoreCommand = $"RESTORE DATABASE AdventureWorksLT2022 FROM DISK = '{backupPath}' WITH MOVE 'AdventureWorksLT2022_Data' TO '/var/opt/mssql/data/AdventureWorksLT2022.mdf', MOVE 'AdventureWorksLT2022_Log' TO '/var/opt/mssql/data/AdventureWorksLT2022_log.ldf'";

            await _container.StartAsync();

            await using DbConnection connection = new SqlConnection(_container.GetConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = restoreCommand;
            await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();

            var services = new ServiceCollection();

            services.AddDbContext<TestingDbContext>(options => options.UseSqlServer($"{_container.GetConnectionString()};Database=AdventureWorksLT2022;"));
            services.AddMediatR(configure => configure.RegisterServicesFromAssemblies(typeof(TestingDbContext).Assembly));
            services.AddMagicAutoMapper().AddAutoMapper(typeof(TestingDbContext).Assembly);
            services.AddDbUtils<TestingDbContext>(typeof(TestingDbContext).Assembly);

            var provider = services.BuildServiceProvider();

            Mediator = provider.GetRequiredService<IMediator>();
            Context = provider.GetRequiredService<TestingDbContext>();
        }

        public async Task DisposeAsync() => await _container.StopAsync();

        private string GetSolutionDirectoryInfo(string? current = null)
        {
            var directory = new DirectoryInfo(current ?? Directory.GetCurrentDirectory());
            if (directory.Exists && !directory.GetFiles("*.sln").Any()) return GetSolutionDirectoryInfo(directory.Parent?.FullName ?? null);

            return directory.FullName;
        }
    }
}