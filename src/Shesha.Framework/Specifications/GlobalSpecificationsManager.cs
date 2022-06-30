using Abp.Dependency;
using Abp.Reflection;
using Abp.Specifications;
using Shesha.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Shesha.Specifications
{
    public class GlobalSpecificationsManager: IGlobalSpecificationsManager, ISingletonDependency
    {
        private readonly ITypeFinder _typeFinder;
        private readonly List<ISpecificationInfo> _specifications;

        public GlobalSpecificationsManager(ITypeFinder typeFinder)
        {
            _typeFinder = typeFinder;

            var specifications = _typeFinder.Find(t => t.IsSpecificationType()).ToList();
            specifications = specifications.Where(t => t.HasAttribute<GlobalSpecificationAttribute>()).ToList();

            _specifications = _typeFinder.Find(t => t.IsSpecificationType() && t.HasAttribute<GlobalSpecificationAttribute>())
                .SelectMany(t => SpecificationsHelper.GetSpecificationsInfo(t).Cast<ISpecificationInfo>())
                .ToList();
        }

        /// inheritedDoc
        public List<ISpecificationInfo> Specifications => _specifications;
    }
}
