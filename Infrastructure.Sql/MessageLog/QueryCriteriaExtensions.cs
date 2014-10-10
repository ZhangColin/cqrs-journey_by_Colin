using System;
using System.Linq.Expressions;
using System.Text;
using Infrastructure.MessageLog;

namespace Infrastructure.Sql.MessageLog {
    internal static class QueryCriteriaExtensions {
        public static Expression<Func<MessageLogEntity, bool>> ToExpression(this QueryCriteria criteria) {
            Expression<Func<MessageLogEntity, bool>> expression = null;

            foreach(string assemblyName in criteria.AssemblyNames) {
                string value = assemblyName;
                if(expression==null) {
                    expression = e => e.AssemblyName == value;
                }
                else {
                    expression = expression.Or(e => e.AssemblyName == value);
                }
            }

            Expression<Func<MessageLogEntity, bool>> filter = null;
            foreach(string item in criteria.FullNames) {
                string value = item;
                if(filter== null) {
                    filter = e => e.FullName == value;
                }
                else {
                    filter = filter.Or(e => e.FullName == value);
                }
            }

            if(filter!=null) {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }
            
            foreach(string item in criteria.Namespaces) {
                string value = item;
                if(filter== null) {
                    filter = e => e.Namespace == value;
                }
                else {
                    filter = filter.Or(e => e.Namespace == value);
                }
            }

            if(filter!=null) {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }
            
            foreach(string item in criteria.SourceIds) {
                string value = item;
                if(filter== null) {
                    filter = e => e.SourceId == value;
                }
                else {
                    filter = filter.Or(e => e.SourceId == value);
                }
            }

            if(filter!=null) {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }
            
            foreach(string item in criteria.SourceTypes) {
                string value = item;
                if(filter== null) {
                    filter = e => e.SourceType == value;
                }
                else {
                    filter = filter.Or(e => e.SourceType == value);
                }
            }

            if(filter!=null) {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }
            
            foreach(string item in criteria.TypeNames) {
                string value = item;
                if(filter== null) {
                    filter = e => e.TypeName == value;
                }
                else {
                    filter = filter.Or(e => e.TypeName == value);
                }
            }

            if(filter!=null) {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }

            if(criteria.EndDate.HasValue) {
                string creationDatefilter = criteria.EndDate.Value.ToString("o");
                filter = e => e.CreationDate.CompareTo(creationDatefilter) <= 0;

                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }

            return expression;
        } 
    }
}