namespace Rene.Utils.Db.IntegrationTest.Tests.Common
{
    using Microsoft.Data.SqlClient;
    using System.Data.Common;
    using Testcontainers.MsSql;

    /// <summary>
    /// Fixture for MsSqlContainer. It creates a container with a backup of AdventureWorksLT2022 database.
    /// </summary>
    public class MsSqlContainerFixture : IAsyncLifetime
    {
        private const string LocalBackupPath = @"D:\Proyectos\DotNet.Rene.Utils.Db\resources\backups";
        private const string TargetBackupPath = "/backups/mssql/";
        private const string BackupFileName = "AdventureWorksLT2022.bak";

        private const string Password = "Password123";
        private readonly MsSqlContainer _container;

        public MsSqlContainerFixture()
        {
            var backupPath = Path.Combine(LocalBackupPath, BackupFileName);
            var info = new FileInfo(backupPath);

            _container = new MsSqlBuilder()
                .WithPassword(Password)
                .WithResourceMapping(info, TargetBackupPath)
                .Build();
        }

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
        }

        public async Task DisposeAsync() => await _container.StopAsync();
    }
}