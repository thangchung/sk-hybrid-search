var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("password", secret: true);
var elasticsearch = builder.AddElasticsearch("elasticsearch");

// Add the Hybrid Search project
var hybridSearch = builder.AddProject<Projects.HybridSearch>("hybrid-search")
	.WithReference(elasticsearch).WaitFor(elasticsearch);

builder.Build().Run();
