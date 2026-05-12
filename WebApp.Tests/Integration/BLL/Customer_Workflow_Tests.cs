using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Customers.Errors;
using App.BLL.DTO.Properties;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Customer_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Customer_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListProfileUpdateAndDeleteCustomer_Workflow()
    {
        using var culture = new CultureScope("en");
        var customer = await CreateCustomerAsync("customer-basic");

        using (var listScope = _factory.Services.CreateScope())
        {
            var bll = listScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var list = await bll.Customers.ListForCompanyAsync(CompanyRoute());
            var profile = await bll.Customers.GetProfileAsync(CustomerRoute(customer.Slug));

            list.Value.Should().Contain(item => item.CustomerId == customer.CustomerId);
            profile.Value.Name.Should().Be(customer.Name);
            profile.Value.RegistryCode.Should().Be(customer.RegistryCode);
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.Customers.UpdateAndGetProfileAsync(
                CustomerRoute(customer.Slug),
                new CustomerBllDto
                {
                    Name = "Updated Workflow Customer",
                    RegistryCode = customer.RegistryCode,
                    BillingEmail = "updated-workflow-customer@test.ee",
                    BillingAddress = "Updated Customer Street 1",
                    Phone = "+372 5555 7001"
                });

            updated.IsSuccess.Should().BeTrue();
            updated.Value.Name.Should().Be("Updated Workflow Customer");
            updated.Value.BillingEmail.Should().Be("updated-workflow-customer@test.ee");
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.Customers.DeleteAsync(CustomerRoute(customer.Slug), "Updated Workflow Customer");
        var afterDelete = await deleteBll.Customers.GetProfileAsync(CustomerRoute(customer.Slug));

        deleted.IsSuccess.Should().BeTrue();
        afterDelete.ShouldFailWith<NotFoundError>();
    }

    [Fact]
    public async Task CustomerDeleteIsBlockedWhilePropertyExists()
    {
        var customer = await CreateCustomerAsync("customer-delete-blocked");

        using (var propertyScope = _factory.Services.CreateScope())
        {
            var bll = propertyScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var property = await bll.Properties.CreateAndGetProfileAsync(
                CustomerRoute(customer.Slug),
                new PropertyBllDto
                {
                    PropertyTypeId = TestTenants.PropertyTypeReferencedId,
                    Label = "Customer Dependency Property",
                    AddressLine = "Dependency Street 1",
                    City = "Tallinn",
                    PostalCode = "10111"
                });

            property.IsSuccess.Should().BeTrue();
        }

        using var deleteScope = _factory.Services.CreateScope();
        var bllDelete = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var blocked = await bllDelete.Customers.DeleteAsync(CustomerRoute(customer.Slug), customer.Name);

        blocked.ShouldFailWith<BusinessRuleError>();
    }

    [Fact]
    public async Task CustomerValidationDuplicateRegistryAndMissingRouteFailuresAreReturned()
    {
        var customer = await CreateCustomerAsync("customer-validation");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var duplicate = await bll.Customers.CreateAsync(CompanyRoute(), NewCustomerDto(
            "Duplicate Workflow Customer",
            customer.RegistryCode));
        var invalid = await bll.Customers.CreateAsync(CompanyRoute(), new CustomerBllDto
        {
            Name = " ",
            RegistryCode = UniqueCode("CUST")
        });
        var missingCompany = await bll.Customers.GetProfileAsync(new CustomerRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = "missing-company",
            CustomerSlug = customer.Slug
        });

        duplicate.Errors.Should().Contain(error => error is DuplicateRegistryCodeError);
        invalid.ShouldFailWith<ValidationAppError>();
        missingCompany.ShouldFailWith<NotFoundError>();
    }

    private async Task<CustomerSeed> CreateCustomerAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var registryCode = UniqueCode("CUST");
        var name = $"Workflow Customer {suffix} {Guid.NewGuid():N}"[..45];
        var created = await bll.Customers.CreateAndGetProfileAsync(
            CompanyRoute(),
            NewCustomerDto(name, registryCode));

        created.IsSuccess.Should().BeTrue();
        return new CustomerSeed(created.Value.Id, created.Value.Slug, name, registryCode);
    }

    private static ManagementCompanyRoute CompanyRoute()
    {
        return new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        };
    }

    private static CustomerRoute CustomerRoute(string customerSlug)
    {
        return new CustomerRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = customerSlug
        };
    }

    private static CustomerBllDto NewCustomerDto(string name, string registryCode)
    {
        return new CustomerBllDto
        {
            Name = name,
            RegistryCode = registryCode,
            BillingEmail = $"{registryCode.ToLowerInvariant()}@test.ee",
            BillingAddress = "Workflow Customer Street 1",
            Phone = "+372 5555 7000"
        };
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record CustomerSeed(Guid CustomerId, string Slug, string Name, string RegistryCode);
}
