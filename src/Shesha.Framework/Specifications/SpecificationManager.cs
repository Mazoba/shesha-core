using Abp.Dependency;
using Abp.Specifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

namespace Shesha.Specifications
{
    /// <summary>
    /// Provides access to a list of specifications that should be applied in current execution context. Includes both global specifications and custom ones (e.g. applied to concrete API endpoints)
    /// </summary>
    public class SpecificationManager : ISpecificationManager, ITransientDependency
    {
        private static readonly AsyncLocal<ConcurrentStack<ISpecificationsContext>> InternalStack = new AsyncLocal<ConcurrentStack<ISpecificationsContext>>();
        private readonly IGlobalSpecificationsManager _globalSpecificationsManager;

        public SpecificationManager(IGlobalSpecificationsManager globalSpecificationsManager)
        {
            _globalSpecificationsManager = globalSpecificationsManager;
        }

        private static ConcurrentStack<ISpecificationsContext> Stack {
            get {
                if (InternalStack.Value != null)
                    return InternalStack.Value;
                lock (InternalStack)
                {
                    if (InternalStack.Value != null)
                        return InternalStack.Value;

                    return InternalStack.Value = new ConcurrentStack<ISpecificationsContext>();
                }
            }
        }

        public List<Type> SpecificationTypes => ActiveSpecifications.Select(s => s.SpecificationsType).ToList();

        public List<ISpecificationInfo> ActiveSpecifications {
            get 
            {
                var specs = _globalSpecificationsManager.Specifications;
                var localSpecs = Stack.ToList().Cast<ISpecificationInfo>();
                
                return specs.Union(localSpecs).ToList();
            }
        }

        public IQueryable<T> ApplySpecifications<T>(IQueryable<T> queryable)
        {
            var specs = GetSpecifications<T>();
            var filteredQuery = queryable;
            
            foreach (var spec in specs) 
            {
                filteredQuery = filteredQuery.Where(spec.ToExpression());
            }
            return filteredQuery;
        }

        public List<ISpecification<T>> GetSpecifications<T>()
        {
            var specTypes = ActiveSpecifications.Where(t => t.EntityType == typeof(T));

            return specTypes.Select(st => Activator.CreateInstance(st.SpecificationsType) as ISpecification<T>).ToList();
        }

        public ISpecificationsContext Use<TSpec, TEntity>() where TSpec : ISpecification<TEntity>
        {
            var context = new SpecificationsContext(typeof(TSpec), typeof(TEntity));

            Stack.Push(context);

            context.Disposed += (sender, args) =>
            {
                if (!Stack.TryPop(out var specificationsContext))
                    throw new Exception("Failed to remove specifications from the current context");
                
                if (specificationsContext != context)
                    throw new Exception("Wrong specifications sequence. Make sure that you dispose specification contexts in a correct sequence");
            };
            return context;
        }

        public IDisposable Use(params Type[] specificationType)
        {
            var specifications = specificationType.SelectMany(t => SpecificationsHelper.GetSpecificationsInfo(t)).ToList();

            var result = new CompositeDisposable();
            foreach (var specificationInfo in specifications) 
            {
                result.Add(Use(specificationInfo.SpecificationsType, specificationInfo.EntityType));
            }
            return result;
        }

        private ISpecificationsContext Use(Type specificationType, Type entityType)
        {
            var context = new SpecificationsContext(specificationType, entityType);

            Stack.Push(context);

            context.Disposed += (sender, args) =>
            {
                if (!Stack.TryPop(out var specificationsContext))
                    throw new Exception("Failed to remove specifications from the current context");

                if (specificationsContext != context)
                    throw new Exception("Wrong specifications sequence. Make sure that you dispose specification contexts in a correct sequence");
            };
            return context;
        }
    }
}
