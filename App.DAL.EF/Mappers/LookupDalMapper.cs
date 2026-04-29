using App.Contracts;
using App.Contracts.DAL.Lookups;
using Base.Domain;

namespace App.DAL.EF.Mappers;

public static class LookupDalMapper
{
    public static LookupDalDto Map<TLookup>(TLookup source)
        where TLookup : BaseEntity, ILookUpEntity
    {
        return new LookupDalDto
        {
            Id = source.Id,
            Code = source.Code,
            Label = source.Label.ToString()
        };
    }
}
