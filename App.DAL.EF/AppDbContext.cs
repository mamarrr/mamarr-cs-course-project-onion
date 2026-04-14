using System.Text.Json;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace App.DAL.EF;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IDataProtectionKeyContext
{
    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;

    // This maps to the table that stores data protection keys.
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    public DbSet<Contact> Contacts { get; set; } = default!;
    public DbSet<ContactType> ContactTypes { get; set; } = default!;
    public DbSet<Customer> Customers { get; set; } = default!;
    public DbSet<CustomerRepresentative> CustomerRepresentatives { get; set; } = default!;
    public DbSet<CustomerRepresentativeRole> CustomerRepresentativeRoles { get; set; } = default!;
    public DbSet<Lease> Leases { get; set; } = default!;
    public DbSet<LeaseRole> LeaseRoles { get; set; } = default!;
    public DbSet<ManagementCompany> ManagementCompanies { get; set; } = default!;
    public DbSet<ManagementCompanyRole> ManagementCompanyRoles { get; set; } = default!;
    public DbSet<ManagementCompanyUser> ManagementCompanyUsers { get; set; } = default!;
    public DbSet<ManagementCompanyJoinRequest> ManagementCompanyJoinRequests { get; set; } = default!;
    public DbSet<Property> Properties { get; set; } = default!;
    public DbSet<PropertyType> PropertyTypes { get; set; } = default!;
    public DbSet<Resident> Residents { get; set; } = default!;
    public DbSet<ResidentContact> ResidentContacts { get; set; } = default!;
    public DbSet<ResidentUser> ResidentUsers { get; set; } = default!;
    public DbSet<ScheduledWork> ScheduledWorks { get; set; } = default!;
    public DbSet<Ticket> Tickets { get; set; } = default!;
    public DbSet<TicketCategory> TicketCategories { get; set; } = default!;
    public DbSet<TicketPriority> TicketPriorities { get; set; } = default!;
    public DbSet<TicketStatus> TicketStatuses { get; set; } = default!;
    public DbSet<Unit> Units { get; set; } = default!;
    public DbSet<Vendor> Vendors { get; set; } = default!;
    public DbSet<VendorContact> VendorContacts { get; set; } = default!;
    public DbSet<VendorTicketCategory> VendorTicketCategories { get; set; } = default!;
    public DbSet<WorkLog> WorkLogs { get; set; } = default!;
    public DbSet<WorkStatus> WorkStatuses { get; set; } = default!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureDateTimeAsUtc(builder);
        ConfigureConstraintNames(builder);
        ConfigureIndexesAndUniqueConstraints(builder);
        ConfigureSlugProperties(builder);

        builder.Entity<ManagementCompanyRole>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<ContactType>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<CustomerRepresentativeRole>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<LeaseRole>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<PropertyType>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<TicketCategory>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<TicketPriority>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<TicketStatus>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<WorkStatus>().Property(e => e.Label)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        // disable cascade delete
        foreach (var relationship in builder.Model
                     .GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigureSlugProperties(ModelBuilder builder)
    {
        builder.Entity<ManagementCompany>()
            .Property(e => e.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Entity<Customer>()
            .Property(e => e.Slug)
            .HasMaxLength(128)
            .IsRequired();
    }

    private static void ConfigureConstraintNames(ModelBuilder builder)
    {
        builder.Entity<ManagementCompanyUser>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.ManagementCompanyUsers)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_mcompany_user_mcompany");

        builder.Entity<ManagementCompanyUser>()
            .HasOne(e => e.AppUser)
            .WithMany(e => e.ManagementCompanyUsers)
            .HasForeignKey(e => e.AppUserId)
            .HasConstraintName("fk_mcompany_user_appuser");

        builder.Entity<ManagementCompanyUser>()
            .HasOne(e => e.ManagementCompanyRole)
            .WithMany(e => e.ManagementCompanyUsers)
            .HasForeignKey(e => e.ManagementCompanyRoleId)
            .HasConstraintName("fk_mcompany_user_role");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasOne(e => e.AppUser)
            .WithMany(e => e.ManagementCompanyJoinRequests)
            .HasForeignKey(e => e.AppUserId)
            .HasConstraintName("fk_mcompany_join_request_appuser");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.JoinRequests)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_mcompany_join_request_mcompany");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasOne(e => e.RequestedManagementCompanyRole)
            .WithMany(e => e.ManagementCompanyJoinRequests)
            .HasForeignKey(e => e.RequestedManagementCompanyRoleId)
            .HasConstraintName("fk_mcompany_join_request_requested_role");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasOne(e => e.ResolvedByAppUser)
            .WithMany(e => e.ResolvedManagementCompanyJoinRequests)
            .HasForeignKey(e => e.ResolvedByAppUserId)
            .HasConstraintName("fk_mcompany_join_request_resolver_appuser");

        builder.Entity<Customer>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.Customers)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_customer_mcompany");

        builder.Entity<Resident>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.Residents)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_resident_mcompany");

        builder.Entity<ResidentUser>()
            .HasOne(e => e.AppUser)
            .WithMany(e => e.ResidentUsers)
            .HasForeignKey(e => e.AppUserId)
            .HasConstraintName("fk_resident_user_appuser");

        builder.Entity<ResidentUser>()
            .HasOne(e => e.Resident)
            .WithMany(e => e.ResidentUsers)
            .HasForeignKey(e => e.ResidentId)
            .HasConstraintName("fk_resident_user_resident");

        builder.Entity<Property>()
            .HasOne(e => e.PropertyType)
            .WithMany(e => e.Properties)
            .HasForeignKey(e => e.PropertyTypeId)
            .HasConstraintName("fk_property_type");

        builder.Entity<Property>()
            .HasOne(e => e.Customer)
            .WithMany(e => e.Properties)
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("fk_property_customer");

        builder.Entity<Unit>()
            .HasOne(e => e.Property)
            .WithMany(e => e.Units)
            .HasForeignKey(e => e.PropertyId)
            .HasConstraintName("fk_unit_property");

        builder.Entity<CustomerRepresentative>()
            .HasOne(e => e.CustomerRepresentativeRole)
            .WithMany(e => e.CustomerRepresentatives)
            .HasForeignKey(e => e.CustomerRepresentativeRoleId)
            .HasConstraintName("fk_custrep_role");

        builder.Entity<CustomerRepresentative>()
            .HasOne(e => e.Customer)
            .WithMany(e => e.CustomerRepresentatives)
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("fk_custrep_customer");

        builder.Entity<CustomerRepresentative>()
            .HasOne(e => e.Resident)
            .WithMany(e => e.CustomerRepresentatives)
            .HasForeignKey(e => e.ResidentId)
            .HasConstraintName("fk_custrep_resident");

        builder.Entity<Lease>()
            .HasOne(e => e.LeaseRole)
            .WithMany(e => e.Leases)
            .HasForeignKey(e => e.LeaseRoleId)
            .HasConstraintName("fk_lease_role");

        builder.Entity<Lease>()
            .HasOne(e => e.Unit)
            .WithMany(e => e.Leases)
            .HasForeignKey(e => e.UnitId)
            .HasConstraintName("fk_lease_unit");

        builder.Entity<Lease>()
            .HasOne(e => e.Resident)
            .WithMany(e => e.Leases)
            .HasForeignKey(e => e.ResidentId)
            .HasConstraintName("fk_lease_resident");

        builder.Entity<Vendor>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.Vendors)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_vendor_mcompany");

        builder.Entity<VendorContact>()
            .HasOne(e => e.Contact)
            .WithMany(e => e.VendorContacts)
            .HasForeignKey(e => e.ContactId)
            .HasConstraintName("fk_vendor_contact_contact");

        builder.Entity<VendorContact>()
            .HasOne(e => e.Vendor)
            .WithMany(e => e.VendorContacts)
            .HasForeignKey(e => e.VendorId)
            .HasConstraintName("fk_vendor_contact_vendor");

        builder.Entity<Contact>()
            .HasOne(e => e.ContactType)
            .WithMany(e => e.Contacts)
            .HasForeignKey(e => e.ContactTypeId)
            .HasConstraintName("fk_contact_type");

        builder.Entity<Contact>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.Contacts)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_contact_mcompany");

        builder.Entity<ResidentContact>()
            .HasOne(e => e.Resident)
            .WithMany(e => e.ResidentContacts)
            .HasForeignKey(e => e.ResidentId)
            .HasConstraintName("fk_resident_contact_resident");

        builder.Entity<ResidentContact>()
            .HasOne(e => e.Contact)
            .WithMany(e => e.ResidentContacts)
            .HasForeignKey(e => e.ContactId)
            .HasConstraintName("fk_resident_contact_contact");

        builder.Entity<Ticket>()
            .HasOne(e => e.ManagementCompany)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.ManagementCompanyId)
            .HasConstraintName("fk_ticket_mcompany");

        builder.Entity<Ticket>()
            .HasOne(e => e.Customer)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("fk_ticket_customer");

        builder.Entity<Ticket>()
            .HasOne(e => e.Resident)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.ResidentId)
            .HasConstraintName("fk_ticket_resident");

        builder.Entity<Ticket>()
            .HasOne(e => e.Property)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.PropertyId)
            .HasConstraintName("fk_ticket_property");

        builder.Entity<Ticket>()
            .HasOne(e => e.Unit)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.UnitId)
            .HasConstraintName("fk_ticket_unit");

        builder.Entity<Ticket>()
            .HasOne(e => e.TicketCategory)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.TicketCategoryId)
            .HasConstraintName("fk_ticket_category");

        builder.Entity<Ticket>()
            .HasOne(e => e.Vendor)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.VendorId)
            .HasConstraintName("fk_ticket_vendor");

        builder.Entity<Ticket>()
            .HasOne(e => e.TicketStatus)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.TicketStatusId)
            .HasConstraintName("fk_ticket_status");

        builder.Entity<Ticket>()
            .HasOne(e => e.TicketPriority)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.TicketPriorityId)
            .HasConstraintName("fk_ticket_priority");

        builder.Entity<VendorTicketCategory>()
            .HasOne(e => e.Vendor)
            .WithMany(e => e.VendorTicketCategories)
            .HasForeignKey(e => e.VendorId)
            .HasConstraintName("fk_vtc_vendor");

        builder.Entity<VendorTicketCategory>()
            .HasOne(e => e.TicketCategory)
            .WithMany(e => e.VendorTicketCategories)
            .HasForeignKey(e => e.TicketCategoryId)
            .HasConstraintName("fk_vtc_category");

        builder.Entity<ScheduledWork>()
            .HasOne(e => e.Vendor)
            .WithMany(e => e.ScheduledWorks)
            .HasForeignKey(e => e.VendorId)
            .HasConstraintName("fk_schedwork_vendor");

        builder.Entity<ScheduledWork>()
            .HasOne(e => e.Ticket)
            .WithMany(e => e.ScheduledWorks)
            .HasForeignKey(e => e.TicketId)
            .HasConstraintName("fk_schedwork_ticket");

        builder.Entity<ScheduledWork>()
            .HasOne(e => e.WorkStatus)
            .WithMany(e => e.ScheduledWorks)
            .HasForeignKey(e => e.WorkStatusId)
            .HasConstraintName("fk_schedwork_status");

        builder.Entity<WorkLog>()
            .HasOne(e => e.AppUser)
            .WithMany(e => e.WorkLogs)
            .HasForeignKey(e => e.AppUserId)
            .HasConstraintName("fk_worklog_appuser");

        builder.Entity<WorkLog>()
            .HasOne(e => e.ScheduledWork)
            .WithMany(e => e.WorkLogs)
            .HasForeignKey(e => e.ScheduledWorkId)
            .HasConstraintName("fk_worklog_schedwork");
    }

    private static void ConfigureIndexesAndUniqueConstraints(ModelBuilder builder)
    {
        // Schema-defined unique constraints
        builder.Entity<ManagementCompanyRole>()
            .HasAlternateKey(e => e.Code)
            .HasName("uq_MCOMPANY_ROLE_CODE");

        builder.Entity<ManagementCompanyRole>()
            .HasAlternateKey(e => e.Label)
            .HasName("uq_MCOMPANY_ROLE_LABEL");

        builder.Entity<ContactType>()
            .HasAlternateKey(e => e.Code)
            .HasName("uq_CONTACT_TYPE_CODE");

        builder.Entity<CustomerRepresentativeRole>()
            .HasAlternateKey(e => e.Code)
            .HasName("uq_CUSTOMER_REPRESENTATIVE_ROLE_CODE");

        builder.Entity<PropertyType>()
            .HasAlternateKey(e => e.Code)
            .HasName("uq_PROPERTY_TYPE_CODE");

        builder.Entity<TicketCategory>()
            .HasAlternateKey(e => e.Code)
            .HasName("uq_TICKET_CATEGORY_CODE");

        builder.Entity<WorkStatus>()
            .HasAlternateKey(e => e.Code)
            .HasName("uq_WORK_STATUS_CODE");

        builder.Entity<ManagementCompany>()
            .HasAlternateKey(e => e.RegistryCode)
            .HasName("uq_MANAGMENT_COMPANY_REGISTRY_CODE");

        builder.Entity<ManagementCompany>()
            .HasAlternateKey(e => e.Slug)
            .HasName("uq_management_company_slug");

        builder.Entity<Customer>()
            .HasAlternateKey(e => new { e.ManagementCompanyId, e.RegistryCode })
            .HasName("uq_customer_mcompany_registry");

        builder.Entity<Customer>()
            .HasAlternateKey(e => new { e.ManagementCompanyId, e.Slug })
            .HasName("uq_customer_mcompany_slug");

        builder.Entity<Vendor>()
            .HasAlternateKey(e => new { e.ManagementCompanyId, e.RegistryCode })
            .HasName("uq_vendor_mcompany_registry");

        builder.Entity<ManagementCompanyUser>()
            .HasAlternateKey(e => new { e.ManagementCompanyId, e.AppUserId })
            .HasName("uq_mcompany_user_pair");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasIndex(e => new { e.AppUserId, e.ManagementCompanyId })
            .IsUnique()
            .HasDatabaseName("ux_mcompany_join_request_pending_user_company")
            .HasFilter($"\"Status\" = '{ManagementCompanyJoinRequestStatus.Pending}'");

        builder.Entity<ResidentUser>()
            .HasAlternateKey(e => new { e.ResidentId, e.AppUserId })
            .HasName("uq_resident_user_pair");

        builder.Entity<VendorTicketCategory>()
            .HasAlternateKey(e => new { e.VendorId, e.TicketCategoryId })
            .HasName("uq_vtc_pair");

        builder.Entity<Unit>()
            .HasAlternateKey(e => new { e.PropertyId, e.UnitNr })
            .HasName("uq_unit_property_unitnr");

        builder.Entity<Ticket>()
            .HasAlternateKey(e => new { e.ManagementCompanyId, e.TicketNr })
            .HasName("uq_ticket_mcompany_ticketnr");

        builder.Entity<Contact>()
            .HasAlternateKey(e => new { e.ManagementCompanyId, e.ContactTypeId, e.ContactValue })
            .HasName("uq_contact_mcompany_type_value");

        // FK indexes from schema.sql
        builder.Entity<ManagementCompanyUser>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_mcompany_user_mcompany_id_fk");

        builder.Entity<ManagementCompanyUser>()
            .HasIndex(e => e.AppUserId)
            .HasDatabaseName("ix_mcompany_user_appuser_id_fk");

        builder.Entity<ManagementCompanyUser>()
            .HasIndex(e => e.ManagementCompanyRoleId)
            .HasDatabaseName("ix_mcompany_user_mcompany_role_id_fk");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_mcompany_join_request_mcompany_id_fk");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasIndex(e => e.AppUserId)
            .HasDatabaseName("ix_mcompany_join_request_appuser_id_fk");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasIndex(e => e.RequestedManagementCompanyRoleId)
            .HasDatabaseName("ix_mcompany_join_request_requested_role_id_fk");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasIndex(e => e.ResolvedByAppUserId)
            .HasDatabaseName("ix_mcompany_join_request_resolved_by_appuser_id_fk");

        builder.Entity<ManagementCompanyJoinRequest>()
            .HasIndex(e => new { e.ManagementCompanyId, e.Status, e.CreatedAt })
            .HasDatabaseName("ix_mcompany_join_request_company_status_created_at")
            .IsDescending(false, false, true);

        builder.Entity<Customer>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_customer_mcompany_id_fk");

        builder.Entity<ManagementCompany>()
            .HasIndex(e => e.Slug)
            .IsUnique()
            .HasDatabaseName("ux_management_company_slug");

        builder.Entity<Customer>()
            .HasIndex(e => new { e.ManagementCompanyId, e.Slug })
            .IsUnique()
            .HasDatabaseName("ux_customer_company_slug");

        builder.Entity<Resident>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_resident_mcompany_id_fk");

        builder.Entity<ResidentUser>()
            .HasIndex(e => e.AppUserId)
            .HasDatabaseName("ix_resident_user_appuser_id_fk");

        builder.Entity<ResidentUser>()
            .HasIndex(e => e.ResidentId)
            .HasDatabaseName("ix_resident_user_resident_id_fk");

        builder.Entity<Property>()
            .HasIndex(e => e.PropertyTypeId)
            .HasDatabaseName("ix_property_property_type_id_fk");

        builder.Entity<Property>()
            .HasIndex(e => e.CustomerId)
            .HasDatabaseName("ix_property_customer_id_fk");

        builder.Entity<Unit>()
            .HasIndex(e => e.PropertyId)
            .HasDatabaseName("ix_unit_property_id_fk");

        builder.Entity<CustomerRepresentative>()
            .HasIndex(e => e.CustomerRepresentativeRoleId)
            .HasDatabaseName("ix_customer_representative_role_id_fk");

        builder.Entity<CustomerRepresentative>()
            .HasIndex(e => e.CustomerId)
            .HasDatabaseName("ix_customer_representative_customer_id_fk");

        builder.Entity<CustomerRepresentative>()
            .HasIndex(e => e.ResidentId)
            .HasDatabaseName("ix_customer_representative_resident_id_fk");

        builder.Entity<Lease>()
            .HasIndex(e => e.LeaseRoleId)
            .HasDatabaseName("ix_lease_lease_role_id_fk");

        builder.Entity<Lease>()
            .HasIndex(e => e.UnitId)
            .HasDatabaseName("ix_lease_unit_id_fk");

        builder.Entity<Lease>()
            .HasIndex(e => e.ResidentId)
            .HasDatabaseName("ix_lease_resident_id_fk");

        builder.Entity<Vendor>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_vendor_mcompany_id_fk");

        builder.Entity<VendorContact>()
            .HasIndex(e => e.ContactId)
            .HasDatabaseName("ix_vendor_contact_contact_id_fk");

        builder.Entity<VendorContact>()
            .HasIndex(e => e.VendorId)
            .HasDatabaseName("ix_vendor_contact_vendor_id_fk");

        builder.Entity<Contact>()
            .HasIndex(e => e.ContactTypeId)
            .HasDatabaseName("ix_contact_contact_type_id_fk");

        builder.Entity<Contact>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_contact_mcompany_id_fk");

        builder.Entity<ResidentContact>()
            .HasIndex(e => e.ResidentId)
            .HasDatabaseName("ix_resident_contact_resident_id_fk");

        builder.Entity<ResidentContact>()
            .HasIndex(e => e.ContactId)
            .HasDatabaseName("ix_resident_contact_contact_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_ticket_mcompany_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.CustomerId)
            .HasDatabaseName("ix_ticket_customer_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.ResidentId)
            .HasDatabaseName("ix_ticket_resident_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.PropertyId)
            .HasDatabaseName("ix_ticket_property_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.UnitId)
            .HasDatabaseName("ix_ticket_unit_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.TicketCategoryId)
            .HasDatabaseName("ix_ticket_ticket_category_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.VendorId)
            .HasDatabaseName("ix_ticket_vendor_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.TicketStatusId)
            .HasDatabaseName("ix_ticket_ticket_status_id_fk");

        builder.Entity<Ticket>()
            .HasIndex(e => e.TicketPriorityId)
            .HasDatabaseName("ix_ticket_ticket_priority_id_fk");

        builder.Entity<VendorTicketCategory>()
            .HasIndex(e => e.VendorId)
            .HasDatabaseName("ix_vtc_vendor_id_fk");

        builder.Entity<VendorTicketCategory>()
            .HasIndex(e => e.TicketCategoryId)
            .HasDatabaseName("ix_vtc_category_id_fk");

        builder.Entity<ScheduledWork>()
            .HasIndex(e => e.VendorId)
            .HasDatabaseName("ix_scheduled_work_vendor_id_fk");

        builder.Entity<ScheduledWork>()
            .HasIndex(e => e.TicketId)
            .HasDatabaseName("ix_scheduled_work_ticket_id_fk");

        builder.Entity<ScheduledWork>()
            .HasIndex(e => e.WorkStatusId)
            .HasDatabaseName("ix_scheduled_work_work_status_id_fk");

        builder.Entity<WorkLog>()
            .HasIndex(e => e.AppUserId)
            .HasDatabaseName("ix_work_log_appuser_id_fk");

        builder.Entity<WorkLog>()
            .HasIndex(e => e.ScheduledWorkId)
            .HasDatabaseName("ix_work_log_scheduled_work_id_fk");

        // Additional non-FK indexes from schema.sql
        builder.Entity<Ticket>()
            .HasIndex(e => new { e.ManagementCompanyId, e.TicketStatusId, e.CreatedAt })
            .HasDatabaseName("ix_ticket_company_status_created_at")
            .IsDescending(false, false, true);

        builder.Entity<Ticket>()
            .HasIndex(e => new { e.VendorId, e.TicketStatusId, e.CreatedAt })
            .HasDatabaseName("ix_ticket_vendor_status_created_at")
            .IsDescending(false, false, true);

        builder.Entity<ScheduledWork>()
            .HasIndex(e => new { e.VendorId, e.ScheduledStart })
            .HasDatabaseName("ix_scheduled_work_vendor_scheduled_start");

        builder.Entity<WorkLog>()
            .HasIndex(e => new { e.ScheduledWorkId, e.CreatedAt })
            .HasDatabaseName("ix_work_log_schedwork_created_at")
            .IsDescending(false, true);

        builder.Entity<Ticket>()
            .HasIndex(e => new { e.ManagementCompanyId, e.TicketNr })
            .IsUnique()
            .HasDatabaseName("ux_ticket_company_ticket_nr");

        builder.Entity<Unit>()
            .HasIndex(e => new { e.PropertyId, e.UnitNr })
            .IsUnique()
            .HasDatabaseName("ux_unit_property_unit_nr");

        builder.Entity<Customer>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_customer_active_by_company")
            .HasFilter("\"IsActive\" = TRUE");

        builder.Entity<Resident>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_resident_active_by_company")
            .HasFilter("\"IsActive\" = TRUE");

        builder.Entity<Vendor>()
            .HasIndex(e => e.ManagementCompanyId)
            .HasDatabaseName("ix_vendor_active_by_company")
            .HasFilter("\"IsActive\" = TRUE");

        builder.Entity<VendorContact>()
            .HasIndex(e => e.VendorId)
            .IsUnique()
            .HasDatabaseName("ux_vendor_contact_one_primary")
            .HasFilter("\"IsPrimary\" = TRUE");

        builder.Entity<ResidentContact>()
            .HasIndex(e => e.ResidentId)
            .IsUnique()
            .HasDatabaseName("ux_resident_contact_one_primary")
            .HasFilter("\"IsPrimary\" = TRUE");
    }

    private static void ConfigureDateTimeAsUtc(ModelBuilder builder)
    {
        // Value converter for DateTime
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Value converter for DateTime?
        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                    : v.Value.ToUniversalTime())
                : v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

}
