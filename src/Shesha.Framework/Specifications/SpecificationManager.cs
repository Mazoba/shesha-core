using Abp;
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
        private static readonly AsyncLocal<SpecificationManagerState> InternalState = new AsyncLocal<SpecificationManagerState>();

        private static readonly AsyncLocal<bool> IsDisabled = new AsyncLocal<bool>();

        private readonly IGlobalSpecificationsManager _globalSpecificationsManager;

        public IIocManager IocManager { get; set; }

        public SpecificationManager(IGlobalSpecificationsManager globalSpecificationsManager)
        {
            _globalSpecificationsManager = globalSpecificationsManager;
        }

        private static SpecificationManagerState State
        {
            get 
            {
                if (InternalState.Value != null)
                    return InternalState.Value;
                lock (InternalState)
                {
                    if (InternalState.Value != null)
                        return InternalState.Value;

                    return InternalState.Value = new SpecificationManagerState();
                }
            }
        }

        public List<Type> SpecificationTypes => ActiveSpecifications.Select(s => s.SpecificationsType).ToList();

        public List<ISpecificationInfo> ActiveSpecifications {
            get 
            {
                if (!State.IsEnabled)
                    return new List<ISpecificationInfo>();

                var specs = _globalSpecificationsManager.Specifications;
                var localSpecs = State.Stack.ToList().Cast<ISpecificationInfo>();
                
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

            return specTypes.Select(si => GetSpecificationInstance<T>(si)).ToList();
        }

        private ISpecification<T> GetSpecificationInstance<T>(ISpecificationInfo specInfo) 
        {
            return IocManager.IsRegistered(specInfo.SpecificationsType)
                ? IocManager.Resolve(specInfo.SpecificationsType) as ISpecification<T>
                : Activator.CreateInstance(specInfo.SpecificationsType) as ISpecification<T>;
        }

        public ISpecificationsContext Use<TSpec, TEntity>() where TSpec : ISpecification<TEntity>
        {
            var context = new SpecificationsContext(typeof(TSpec), typeof(TEntity));

            State.Stack.Push(context);

            context.Disposed += (sender, args) =>
            {
                if (!State.Stack.TryPop(out var specificationsContext))
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

            State.Stack.Push(context);

            context.Disposed += (sender, args) =>
            {
                if (!State.Stack.TryPop(out var specificationsContext))
                    throw new Exception("Failed to remove specifications from the current context");

                if (specificationsContext != context)
                    throw new Exception("Wrong specifications sequence. Make sure that you dispose specification contexts in a correct sequence");
            };
            return context;
        }

        protected void EnableSpecifications()
        {
            State.IsEnabled = true;
        }

        public IDisposable DisableSpecifications()
        {
            State.IsEnabled = false;
            return new DisposeAction(() => EnableSpecifications());
        }
    }

    /// <summary>
    /// Specifications menager state
    /// </summary>
    public class SpecificationManagerState 
    {
        public ConcurrentStack<ISpecificationsContext> Stack { get; set; }
        public bool IsEnabled { get; set; }

        public SpecificationManagerState()
        {
            Stack = new ConcurrentStack<ISpecificationsContext>();
            IsEnabled = true;
        }
    }
}
