using App.BLL.DTO.Admin.Lookups;
using App.BLL.DTO.Common.Errors;
using App.BLL.Services.Admin;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Lookups;
using AwesomeAssertions;
using Moq;

namespace WebApp.Tests.Unit.BLL.Services;

public class AdminLookupService_Tests
{
    private readonly Mock<ILookupRepository> _lookups = new(MockBehavior.Strict);
    private readonly Mock<IAppUOW> _uow = new(MockBehavior.Strict);
    private readonly AdminLookupService _service;

    public AdminLookupService_Tests()
    {
        _uow.SetupGet(uow => uow.Lookups).Returns(_lookups.Object);
        _service = new AdminLookupService(_uow.Object);
    }

    [Fact]
    public void GetLookupTypes_ReturnsSupportedAdminLookupTypes()
    {
        var types = _service.GetLookupTypes();

        types.Select(type => type.Type).Should().BeEquivalentTo(Enum.GetValues<AdminLookupType>());
        types.Should().Contain(type => type.Type == AdminLookupType.PropertyType && type.Title == "Property types");
    }

    [Fact]
    public async Task GetLookupItemsAsync_MapsItemsAndProtectedFlags()
    {
        _lookups
            .Setup(repo => repo.GetLookupItemsAsync(LookupTable.ManagementCompanyRole, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new LookupItemDalDto { Id = Guid.NewGuid(), Code = "OWNER", Label = "Owner" },
                new LookupItemDalDto { Id = Guid.NewGuid(), Code = "CUSTOM", Label = "Custom" }
            ]);

        var result = await _service.GetLookupItemsAsync(AdminLookupType.ManagementCompanyRole);

        result.Type.Should().Be(AdminLookupType.ManagementCompanyRole);
        result.Title.Should().Be("Management company roles");
        result.LookupTypes.Should().NotBeEmpty();
        result.Items.Should().HaveCount(2);
        result.Items.Single(item => item.Code == "OWNER").IsProtected.Should().BeTrue();
        result.Items.Single(item => item.Code == "CUSTOM").IsProtected.Should().BeFalse();
    }

    [Fact]
    public async Task CreateLookupItemAsync_BlankCodeOrLabel_ReturnsValidationError_AndDoesNotSave()
    {
        var result = await _service.CreateLookupItemAsync(
            AdminLookupType.PropertyType,
            new AdminLookupEditDto { Code = " ", Label = "Apartment" });

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ValidationAppError>();
        _lookups.Verify(repo => repo.CreateLookupItemAsync(It.IsAny<LookupTable>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLookupItemAsync_DuplicateCode_ReturnsConflict_AndDoesNotSave()
    {
        _lookups
            .Setup(repo => repo.CodeExistsAsync(LookupTable.PropertyType, "APT", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateLookupItemAsync(
            AdminLookupType.PropertyType,
            new AdminLookupEditDto { Code = "APT", Label = "Apartment" });

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();
        _lookups.Verify(repo => repo.CreateLookupItemAsync(It.IsAny<LookupTable>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLookupItemAsync_ValidRequest_TrimsValuesCreatesAndSaves()
    {
        var createdId = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.CodeExistsAsync(LookupTable.PropertyType, " APT ", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _lookups
            .Setup(repo => repo.CreateLookupItemAsync(LookupTable.PropertyType, "APT", "Apartment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = createdId, Code = "APT", Label = "Apartment" });
        _uow.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.CreateLookupItemAsync(
            AdminLookupType.PropertyType,
            new AdminLookupEditDto { Code = " APT ", Label = " Apartment " });

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(createdId);
        result.Value.Code.Should().Be("APT");
        result.Value.Label.Should().Be("Apartment");
        result.Value.IsProtected.Should().BeFalse();
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLookupItemAsync_MissingExistingItem_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.FindLookupItemAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LookupItemDalDto?)null);

        var result = await _service.UpdateLookupItemAsync(
            AdminLookupType.PropertyType,
            id,
            new AdminLookupEditDto { Code = "APT", Label = "Apartment" });

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLookupItemAsync_ProtectedCodeChange_ReturnsBusinessRuleError()
    {
        var id = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.FindLookupItemAsync(LookupTable.ManagementCompanyRole, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = id, Code = "OWNER", Label = "Owner" });

        var result = await _service.UpdateLookupItemAsync(
            AdminLookupType.ManagementCompanyRole,
            id,
            new AdminLookupEditDto { Code = "ADMIN", Label = "Owner" });

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BusinessRuleError>();
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLookupItemAsync_ValidRequest_UpdatesAndSaves()
    {
        var id = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.FindLookupItemAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = id, Code = "APT", Label = "Apartment" });
        _lookups
            .Setup(repo => repo.CodeExistsAsync(LookupTable.PropertyType, " APT_UPDATED ", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _lookups
            .Setup(repo => repo.UpdateLookupItemAsync(LookupTable.PropertyType, id, "APT_UPDATED", "Apartment updated", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = id, Code = "APT_UPDATED", Label = "Apartment updated" });
        _uow.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.UpdateLookupItemAsync(
            AdminLookupType.PropertyType,
            id,
            new AdminLookupEditDto { Code = " APT_UPDATED ", Label = " Apartment updated " });

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("APT_UPDATED");
        result.Value.Label.Should().Be("Apartment updated");
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDeleteCheckAsync_ProtectedLookup_ReturnsBlockReason()
    {
        var id = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.FindLookupItemAsync(LookupTable.WorkStatus, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = id, Code = "SCHEDULED", Label = "Scheduled" });
        _lookups
            .Setup(repo => repo.IsLookupInUseAsync(LookupTable.WorkStatus, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.GetDeleteCheckAsync(AdminLookupType.WorkStatus, id);

        result.IsProtected.Should().BeTrue();
        result.BlockReason.Should().Be("Protected lookup values cannot be deleted.");
    }

    [Fact]
    public async Task DeleteLookupItemAsync_InUseLookup_ReturnsBusinessRuleError_AndDoesNotDelete()
    {
        var id = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.FindLookupItemAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = id, Code = "APT", Label = "Apartment" });
        _lookups
            .Setup(repo => repo.IsLookupInUseAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteLookupItemAsync(AdminLookupType.PropertyType, id);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BusinessRuleError>();
        _lookups.Verify(repo => repo.DeleteLookupItemAsync(It.IsAny<LookupTable>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLookupItemAsync_SafeLookup_DeletesAndSaves()
    {
        var id = Guid.NewGuid();
        _lookups
            .Setup(repo => repo.FindLookupItemAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LookupItemDalDto { Id = id, Code = "APT", Label = "Apartment" });
        _lookups
            .Setup(repo => repo.IsLookupInUseAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _lookups
            .Setup(repo => repo.DeleteLookupItemAsync(LookupTable.PropertyType, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.DeleteLookupItemAsync(AdminLookupType.PropertyType, id);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}
