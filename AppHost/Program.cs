var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.HydeSearch>("hyde-search");

builder.Build().Run();
