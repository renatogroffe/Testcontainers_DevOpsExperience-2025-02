using BenchmarkDotNet.Running;
using BenchmarkingDapperEFCoreCRMSqlServer;
using BenchmarkingDapperEFCoreCRMSqlServer.Tests;
using DbUp;
using System.Reflection;
using Testcontainers.MsSql;


Console.WriteLine("Criando container para uso do SQL Server 2022...");
var msSqlContainer = new MsSqlBuilder()
  .WithImage("mcr.microsoft.com/mssql/server:2022-CU17-ubuntu-22.04")
  .Build();
await msSqlContainer.StartAsync();
// Lembrando que o SQL Server ja tem uma estrategia de Wait embutida:
// https://github.com/testcontainers/testcontainers-dotnet/discussions/1167#discussioncomment-9270050

var connectionString = msSqlContainer.GetConnectionString();
Console.WriteLine($"Connection String da base de dados master: {connectionString}");
Configurations.Load(connectionString);

Console.WriteLine("Executando Migrations com DbUp...");

var upgrader = DeployChanges.To.SqlDatabase(Configurations.BaseMaster)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();
var result = upgrader.PerformUpgrade();

if (result.Successful)
{
    Console.WriteLine("Migrations do DbUp executadas com sucesso!");
    new BenchmarkSwitcher([ typeof(CRMTests) ]).Run(args);
}
else
{
    Environment.ExitCode = 3;
    Console.WriteLine($"Falha na execucao das Migrations do DbUp: {result.Error.Message}");
}

if (Environment.GetEnvironmentVariable("ExecucaoManual") == "true")
{
    Console.WriteLine();
    Console.WriteLine($"Connection String da base de dados master: {connectionString}");
    Configurations.Load(connectionString);

    Console.WriteLine("Pressione ENTER para interromper a execucao do container...");
    Console.ReadLine();

    await msSqlContainer.StopAsync();
    Console.WriteLine("Pressione ENTER para encerrar a aplicacao...");
    Console.ReadLine();
}

return Environment.ExitCode;