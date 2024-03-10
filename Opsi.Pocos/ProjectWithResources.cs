using System.Reflection;

namespace Opsi.Pocos;

public class ProjectWithResources : ProjectBase
{
    public ProjectWithResources()
    {
        Resources = new List<Resource>();
    }

    public IReadOnlyCollection<Resource> Resources { get; set; }

    public static ProjectWithResources FromProjectBase(ProjectBase projectBase)
    {
        var projectWithResources = new ProjectWithResources();

        foreach (var propInfo in typeof(ProjectBase).GetProperties(BindingFlags.Public|BindingFlags.Instance))
        {
            propInfo.SetValue(projectWithResources, propInfo.GetValue(projectBase));
        }

        return projectWithResources;
    }
}
