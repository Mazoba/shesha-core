﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Impl;
using NHibernate.Proxy;
using NHibernate.Util;
using NHibernate.Engine;
using Abp.Domain.Entities;
using Abp.Extensions;
using Shesha.Domain;
using Shesha.NHibernate;

namespace Shesha.NHibernate.Session
{

    /// <summary>
    /// Provides extension methods to easily find dirty properties for NHibernate.
    /// </summary>
    public static class SessionExtensions
    {
        public static List<DirtyPropertyInfo> GetDirtyProperties(this ISession session, Object entity)
        {
            var className = NHibernateProxyHelper.GuessClass(entity).FullName;
            var sessionImpl = session.GetSessionImplementation();
            var persister = sessionImpl.Factory.GetEntityPersister(className);

            var oldEntry = session.GetEntry(entity);
            Object[] oldState = oldEntry?.LoadedState;
            Object[] currentState = persister.GetPropertyValues(entity);
            Int32[] dirtyProps = oldState != null
                ? persister.FindDirty(currentState, oldState, entity, sessionImpl)
                : null;

            if (dirtyProps == null)
            {
                dirtyProps = new int[currentState.Length];
                for (int i = 0; i < currentState.Length - 1; i++)
                {
                    dirtyProps[i] = i;
                }
            }

            return dirtyProps != null
                ? dirtyProps.Select(i => new DirtyPropertyInfo()
                {
                    Name = persister.PropertyNames[i],
                    OldValue = oldState?[i],
                    NewValue = currentState[i]
                }).ToList()
                : new List<DirtyPropertyInfo>();
        }

        public static EntityEntry GetEntry(this ISession session, Object entity)
        {
            var className = NHibernateProxyHelper.GuessClass(entity).FullName;
            var sessionImpl = session.GetSessionImplementation();
            var oldEntry = sessionImpl.PersistenceContext.GetEntry(entity);

            if (oldEntry == null)
            {
                if (entity is INHibernateProxy proxy)
                {
                    Object obj = sessionImpl.PersistenceContext.Unproxy(proxy);
                    oldEntry = sessionImpl.PersistenceContext.GetEntry(obj);
                }
                else
                {
                    //System.Diagnostics.Debug.Assert(false, "Entity was likely retrieved using an NHibernate session which is no longer available.");
                    return null;
                }
            }

            return oldEntry;
        }

        public static bool IsEntityDeleted(this ISession session, Object entity)
        {
            var entityEntry = session.GetEntry(entity);
            if (entityEntry.Status == Status.Deleted || entityEntry.Status == Status.Gone)
            {
                return true;
            }
            return entity is ISoftDelete && entity.As<ISoftDelete>().IsDeleted;
        }

        /// <summary>
        /// Ends session and commit transaction if active
        /// </summary>
        /// <param name="session"></param>
        /// <param name="commitTransaction"></param>
        public static void EndSession(this ISession session, bool commitTransaction = true)
        {
            try
            {
                if (session.Transaction != null && session.Transaction.IsActive)
                {
                    if (commitTransaction)
                    {
                        try
                        {
                            session.Transaction.Commit();
                        }
                        catch
                        {
                            session.Transaction.Rollback();
                            throw;
                        }
                    }
                    else
                    {
                        session.Transaction.Rollback();
                    }
                }
            }
            finally
            {
                if (session.IsOpen)
                    session.Close();

                session.Dispose();
            }
        }

        public static IQuery CreateQuery(this ISession session, System.Type entityType, FilterCriteria criteria)
        {
            return CreateQuery(session, entityType, "select ent ", criteria, null);
        }
        public static IQuery CreateQuery(this ISession session, System.Type entityType, FilterCriteria criteria, string orderByClause)
        {
            return CreateQuery(session, entityType, "select ent ", criteria, orderByClause);
        }

        public static IQuery CreateQuery(this ISession session, System.Type entityType, string hqlStart, FilterCriteria criteria, string orderByClause)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(hqlStart))
                sb.Append(hqlStart);

            AppendHqlClauses(entityType, criteria, sb);

            if (!string.IsNullOrEmpty(orderByClause))
            {
                sb.Append(" order by ");
                sb.Append(orderByClause.Replace("ascending", "asc").Replace("descending", "desc"));
            }

            var q = session.CreateQuery(sb.ToString());
            TransferHqlParameters(q, criteria);

            return q;
        }

        public static IQuery CreateQueryCount(this ISession session, System.Type entityType, FilterCriteria criteria)
        {
            return CreateQuery(session, entityType, "select count(*) ", criteria, null);
        }

        private static void TransferHqlParameters(IQuery q, FilterCriteria criteria)
        {
            foreach (var param in criteria.FilterParameters)
            {
                if (q.NamedParameters.Contains(param.Key))
                {
                    if (param.Value != null && param.Value is IList)
                    {
                        var list = param.Value as IList;
                        if (list.Any())
                            q.SetParameterList(param.Key, (IEnumerable)param.Value);
                        else
                            q.SetParameter(param.Key, null);
                    }
                    else
                        q.SetParameter(param.Key, param.Value);
                }
            }
        }

        private static void AppendHqlClauses(System.Type entityType, FilterCriteria criteria, StringBuilder sb)
        {
            sb.Append(" from ");
            sb.Append(entityType.FullName);
            sb.Append(" ent");

            foreach (var joinClause in criteria.JoinClauses)
            {
                sb.Append(" ");
                sb.Append(joinClause);
                sb.Append(" ");
            }

            if (criteria.FilterClauses.Count > 0)
            {
                sb.Append(" where ");
                bool hasClause = false;
                foreach (var clause in criteria.FilterClauses)
                {
                    if (hasClause)
                        sb.Append(" and ");

                    sb.Append("(");
                    sb.Append(clause);
                    sb.Append(")");
                    hasClause = true;
                }
            }
        }

        public static Guid GetId(this ISession session)
        {
            return ((SessionImpl)session).SessionId;
        }

        public class DirtyPropertyInfo
        {
            public string Name { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }
        }
    }
}
