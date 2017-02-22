﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphView.TSQL_Syntax_Tree;

namespace GraphView
{
    /// <summary>
    /// The visitor that classifies table references in a FROM clause
    /// into named table references and others. A named table reference
    /// represents the entire collection of vertices in the graph. 
    /// Other table references correspond to query derived tables, 
    /// variable tables defined earlier in the script, or table-valued 
    /// functions.
    /// </summary>
    internal class TableClassifyVisitor : WSqlFragmentVisitor
    {
        private List<WNamedTableReference> vertexTableList;
        private List<WTableReferenceWithAlias> nonVertexTableList;
        
        public void Invoke(
            WFromClause fromClause, 
            List<WNamedTableReference> vertexTableList, 
            List<WTableReferenceWithAlias> nonVertexTableList)
        {
            this.vertexTableList = vertexTableList;
            this.nonVertexTableList = nonVertexTableList;

            foreach (WTableReference tabRef in fromClause.TableReferences)
            {
                tabRef.Accept(this);
            }
        }

        public override void Visit(WNamedTableReference node)
        {
            vertexTableList.Add(node);
        }

        public override void Visit(WQueryDerivedTable node)
        {
            nonVertexTableList.Add(node);
        }

        public override void Visit(WSchemaObjectFunctionTableReference node)
        {
            nonVertexTableList.Add(node);
        }

        public override void Visit(WVariableTableReference node)
        {
            nonVertexTableList.Add(node);
        }
    }

    /// <summary>
    /// The visitor that traverses the syntax tree and returns the columns 
    /// accessed in current query fragment for each provided table alias. 
    /// This visitor is used to determine what vertex/edge properties are projected 
    /// when a JSON query is sent to the underlying system to retrieve vertices and edges. 
    /// </summary>
    internal class AccessedTableColumnVisitor : WSqlFragmentVisitor
    {
        // A collection of table aliases and their columns
        // accessed in the query block
        Dictionary<string, HashSet<string>> accessedColumns;
        private bool _isOnlyTargetTableReferenced;

        public Dictionary<string, HashSet<string>> Invoke(WSqlFragment sqlFragment, List<string> targetTableReferences, 
            out bool isOnlyTargetTableReferecend)
        {
            _isOnlyTargetTableReferenced = true;
            accessedColumns = new Dictionary<string, HashSet<string>>(targetTableReferences.Count);
            foreach (string tabAlias in targetTableReferences)
            {
                accessedColumns.Add(tabAlias, new HashSet<string>());
            }

            sqlFragment.Accept(this);

            foreach (string tableRef in targetTableReferences)
            {
                if (accessedColumns[tableRef].Count == 0)
                {
                    accessedColumns.Remove(tableRef);
                }
            }

            isOnlyTargetTableReferecend = _isOnlyTargetTableReferenced;
            return accessedColumns;
        }

        public override void Visit(WColumnReferenceExpression node) 
        {
            if (node.ColumnType == ColumnType.Wildcard)
                return;

            string columnName = node.ColumnName;
            string tableAlias = node.TableReference;

            if (tableAlias == null)
            {
                throw new QueryCompilationException("Identifier " + columnName + " must be bound to a table alias.");
            }

            if (accessedColumns.ContainsKey(tableAlias))
            {
                accessedColumns[tableAlias].Add(columnName);
            }
            else
            {
                _isOnlyTargetTableReferenced = false;
            }
        }

        public override void Visit(WMatchPath node)
        {
            foreach (var sourceEdge in node.PathEdgeList)
            {
                WSchemaObjectName source = sourceEdge.Item1;
                string tableAlias = source.BaseIdentifier.Value;
                WEdgeColumnReferenceExpression edge = sourceEdge.Item2;

                if (accessedColumns.ContainsKey(tableAlias))
                {
                    switch (edge.EdgeType)
                    {
                        case WEdgeType.OutEdge:
                            accessedColumns[tableAlias].Add(ColumnGraphType.OutAdjacencyList.ToString());
                            break;
                        case WEdgeType.InEdge:
                            accessedColumns[tableAlias].Add(ColumnGraphType.InAdjacencyList.ToString());
                            break;
                        case WEdgeType.BothEdge:
                            accessedColumns[tableAlias].Add(ColumnGraphType.BothAdjacencyList.ToString());
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Turn a SQL-style boolean WValueExpression to lower case
    /// </summary>
    internal class BooleanWValueExpressionVisitor : WSqlFragmentVisitor
    {
        public void Invoke(
            WBooleanExpression booleanExpression)
        {
            if (booleanExpression != null)
                booleanExpression.Accept(this);
        }

        public override void Visit(WValueExpression valueExpression)
        {
            bool bool_value;
            // JSON requires a lower case string if it is a boolean value
            if (!valueExpression.SingleQuoted && bool.TryParse(valueExpression.Value, out bool_value))
                valueExpression.Value = bool_value.ToString().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Return how many times have GraphView runtime functions appeared in a BooleanExpression
    /// </summary>
    internal class GraphviewRuntimeFunctionCountVisitor : WSqlFragmentVisitor
    {
        private int runtimeFunctionCount;

        public int Invoke(
            WBooleanExpression booleanExpression)
        {
            runtimeFunctionCount = 0;

            if (booleanExpression != null)
                booleanExpression.Accept(this);

            return runtimeFunctionCount;
        }

        public override void Visit(WFunctionCall fcall)
        {
            switch (fcall.FunctionName.Value.ToLowerInvariant())
            {
                case "withinarray":
                case "withoutarray":
                case "hasproperty":
                    runtimeFunctionCount++;
                    break;
            }
        }
    }

    /// <summary>
    /// Return how many times have aggregate functions appeared in a SelectQueryBlock
    /// </summary>
    internal class AggregateFunctionCountVisitor : WSqlFragmentVisitor
    {
        private int aggregateFunctionCount;

        public int Invoke(
            WSelectQueryBlock selectQueryBlock)
        {
            aggregateFunctionCount = 0;

            if (selectQueryBlock != null)
                selectQueryBlock.Accept(this);

            return aggregateFunctionCount;
        }

        public override void Visit(WFunctionCall fcall)
        {
            switch (fcall.FunctionName.Value.ToUpper())
            {
                case "COUNT":
                case "FOLD":
                case "TREE":
                case "CAP":
                    aggregateFunctionCount++;
                    break;
            }
        }

        public override void Visit(WSchemaObjectFunctionTableReference tableReference)
        {
            if (tableReference is WGroupTableReference)
            {
                aggregateFunctionCount++;
            }

            tableReference.AcceptChildren(this);
        }
    }
}
