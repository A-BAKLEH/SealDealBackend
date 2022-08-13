using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.ProjectAggregate.Specifications;

public class ProjectByIdWithItemsSpec : Specification<Project>, ISingleResultSpecification
{
  public ProjectByIdWithItemsSpec(int projectId)
  {
    /*Query
        .Where(project => project.Id == projectId)
        .Include(project => project.Items);*/
  }
}
