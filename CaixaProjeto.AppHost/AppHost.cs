var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.CaixaProjeto_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.CaixaProjeto_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService)
    // Links claros no dashboard do Aspire para abrir a aplicação
    .WithUrlForEndpoint("https", url => url.DisplayText = "Abrir a aplicação")
    .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

builder.Build().Run();
