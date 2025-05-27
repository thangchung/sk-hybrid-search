var builder = DistributedApplication.CreateBuilder(args);

// Add ElasticSearch as a container resource
//var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch", "8.11.0")
//    .WithEnvironment("discovery.type", "single-node")
//    .WithEnvironment("xpack.security.enabled", "false")
//    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
//    .WithEndpoint(targetPort: 9200, name: "http")
//    .WithBindMount("es-data", "/usr/share/elasticsearch/data")
//	.WithEndpoint("tcp", (e) =>
//	{
//		e.Port = 9200;
//		e.IsProxied = false;
//	});

var password = builder.AddParameter("password", secret: true);
var elasticsearch = builder.AddElasticsearch("elasticsearch")
	.WithEndpoint("tcp", (e) =>
	{
		e.Port = 9200;
		e.IsProxied = false;
	});

// Add the Hyde Search project
var hydeSearch = builder.AddProject<Projects.HydeSearch>("hyde-search")
	.WithReference(elasticsearch).WaitFor(elasticsearch);

builder.Build().Run();
