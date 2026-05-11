namespace WebApp.Tests.E2E;

[CollectionDefinition("E2E", DisableParallelization = true)]
public class E2ECollection : ICollectionFixture<PlaywrightWebAppFactory>
{
}
