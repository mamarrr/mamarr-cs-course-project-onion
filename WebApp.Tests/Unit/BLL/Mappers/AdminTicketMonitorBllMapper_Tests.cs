using App.BLL.DTO.Admin.Tickets;
using App.BLL.Mappers.Admin;
using App.DAL.DTO.Admin.Tickets;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.BLL.Mappers;

public class AdminTicketMonitorBllMapper_Tests
{
    private readonly AdminTicketMonitorBllMapper _mapper = new();

    [Fact]
    public void MapSearch_MapsAllFilters()
    {
        var createdFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var createdTo = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var dueFrom = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueTo = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        var dto = new AdminTicketSearchDto
        {
            Company = "Company A",
            Customer = "Customer A",
            TicketNumber = "T-001",
            Status = "CREATED",
            Priority = "MEDIUM",
            Category = "PLUMBING",
            Vendor = "Vendor A",
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            DueFrom = dueFrom,
            DueTo = dueTo,
            OverdueOnly = true,
            OpenOnly = true
        };

        var dal = _mapper.Map(dto);

        dal.Company.Should().Be(dto.Company);
        dal.Customer.Should().Be(dto.Customer);
        dal.TicketNumber.Should().Be(dto.TicketNumber);
        dal.Status.Should().Be(dto.Status);
        dal.Priority.Should().Be(dto.Priority);
        dal.Category.Should().Be(dto.Category);
        dal.Vendor.Should().Be(dto.Vendor);
        dal.CreatedFrom.Should().Be(createdFrom);
        dal.CreatedTo.Should().Be(createdTo);
        dal.DueFrom.Should().Be(dueFrom);
        dal.DueTo.Should().Be(dueTo);
        dal.OverdueOnly.Should().BeTrue();
        dal.OpenOnly.Should().BeTrue();
    }

    [Fact]
    public void MapListItem_MapsAllScalars()
    {
        var dto = _mapper.Map(ListItem());

        dto.Id.Should().Be(new Guid("10000000-0000-0000-0000-000000000001"));
        dto.TicketNumber.Should().Be("T-001");
        dto.Title.Should().Be("Leaking pipe");
        dto.CompanyName.Should().Be("Company A");
        dto.CustomerName.Should().Be("Customer A");
        dto.StatusCode.Should().Be("CREATED");
        dto.StatusLabel.Should().Be("Created");
        dto.PriorityLabel.Should().Be("Medium");
        dto.CategoryLabel.Should().Be("Plumbing");
        dto.VendorName.Should().Be("Vendor A");
        dto.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void MapDetails_MapsDetailsAndNestedCollections()
    {
        var scheduledWorkId = new Guid("20000000-0000-0000-0000-000000000001");
        var workLogId = new Guid("30000000-0000-0000-0000-000000000001");
        var dto = _mapper.Map(new AdminTicketDetailsDalDto
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            TicketNumber = "T-001",
            Title = "Leaking pipe",
            CompanyName = "Company A",
            CustomerName = "Customer A",
            StatusCode = "CREATED",
            StatusLabel = "Created",
            PriorityLabel = "Medium",
            CategoryLabel = "Plumbing",
            VendorName = "Vendor A",
            CreatedAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.AddDays(1),
            IsOverdue = false,
            Description = "Water leak in bathroom",
            PropertyLabel = "Property A",
            UnitNumber = "A-101",
            ResidentName = "Resident A",
            ScheduledWorks =
            [
                new AdminScheduledWorkDalDto
                {
                    Id = scheduledWorkId,
                    VendorName = "Vendor A",
                    WorkStatusLabel = "Scheduled",
                    ScheduledStart = DateTime.UtcNow,
                    ScheduledEnd = DateTime.UtcNow.AddHours(1),
                    RealStart = DateTime.UtcNow.AddMinutes(10),
                    RealEnd = DateTime.UtcNow.AddMinutes(50)
                }
            ],
            WorkLogs =
            [
                new AdminWorkLogDalDto
                {
                    Id = workLogId,
                    LoggedBy = "System Admin",
                    CreatedAt = DateTime.UtcNow,
                    WorkStart = DateTime.UtcNow.AddMinutes(5),
                    WorkEnd = DateTime.UtcNow.AddMinutes(45),
                    Hours = 1.5m,
                    MaterialCost = 10m,
                    LaborCost = 20m,
                    Description = "Fixed"
                }
            ]
        });

        dto.Description.Should().Be("Water leak in bathroom");
        dto.PropertyLabel.Should().Be("Property A");
        dto.UnitNumber.Should().Be("A-101");
        dto.ResidentName.Should().Be("Resident A");
        dto.ScheduledWorks.Should().ContainSingle();
        dto.ScheduledWorks[0].Id.Should().Be(scheduledWorkId);
        dto.ScheduledWorks[0].VendorName.Should().Be("Vendor A");
        dto.ScheduledWorks[0].WorkStatusLabel.Should().Be("Scheduled");
        dto.WorkLogs.Should().ContainSingle();
        dto.WorkLogs[0].Id.Should().Be(workLogId);
        dto.WorkLogs[0].LoggedBy.Should().Be("System Admin");
        dto.WorkLogs[0].Hours.Should().Be(1.5m);
        dto.WorkLogs[0].MaterialCost.Should().Be(10m);
        dto.WorkLogs[0].LaborCost.Should().Be(20m);
        dto.WorkLogs[0].Description.Should().Be("Fixed");
    }

    private static AdminTicketListItemDalDto ListItem()
    {
        return new AdminTicketListItemDalDto
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            TicketNumber = "T-001",
            Title = "Leaking pipe",
            CompanyName = "Company A",
            CustomerName = "Customer A",
            StatusCode = "CREATED",
            StatusLabel = "Created",
            PriorityLabel = "Medium",
            CategoryLabel = "Plumbing",
            VendorName = "Vendor A",
            CreatedAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.AddDays(-1),
            IsOverdue = true
        };
    }
}
