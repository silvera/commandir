namespace Commandir.Interfaces;

public static class PathProvider
{
    public static string GetPath<T>(T command, Func<T, string> nameSelector, Func<T, IEnumerable<T>> parentsSelector)
    {
        List<string> components = new List<string>();
        GetPathComponents(command, components, nameSelector, parentsSelector);
        components.Reverse();
        return string.Concat(components);
    }

    private static void GetPathComponents<T>(T command, List<string> names, Func<T, string> nameSelector, Func<T, IEnumerable<T>> parentsSelector)
    {
        names.Add($"/{nameSelector(command)}");
        foreach(var parentCommand in parentsSelector(command))
        {
            GetPathComponents(parentCommand, names, nameSelector, parentsSelector);
        }
    }
}
