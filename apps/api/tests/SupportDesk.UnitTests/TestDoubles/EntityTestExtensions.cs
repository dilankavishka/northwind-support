using SupportDesk.Domain.Common;

namespace SupportDesk.UnitTests.TestDoubles;

internal static class EntityTestExtensions
{
    public static T WithId<T>(this T entity, int id) where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
        return entity;
    }
}
