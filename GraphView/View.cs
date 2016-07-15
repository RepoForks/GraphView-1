﻿// GraphView
// 
// Copyright (c) 2015 Microsoft Corporation
// 
// All rights reserved. 
// 
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.Win32.SafeHandles;

namespace GraphView
{
    public partial class GraphViewConnection : IDisposable
    {
        private string _nodeName;
        private string _tableSet;

        private static readonly List<string> ColumnList =
            new List<string>
            {
                "GlobalNodeId",
                "InDegree",
                "LocalNodeId"
            };
        //For node View
        private Dictionary<string, Int64> _dictionaryTableId; // <Table Name> => <Table Id>
        private Dictionary<string, int> _dictionaryTableOffsetId; // <Table Name> => <Table Offset Id> (counted from 0)

        private Dictionary<Tuple<string, string>, Int64> _dictionaryColumnId;
        // <Table Name, Column Name> => <Column Id>

        private List<Tuple<string, List<Tuple<string, string>>>> _propertymapping;


        //For edge view
        private Dictionary<Tuple<string, string>, int> _edgeColumnToColumnId; //<NodeTable, Edge> => ColumnId
        private Dictionary<string, List<long>> _dictionaryAttribute; //<EdgeViewAttributeName> => List<AttributeId>
        private Dictionary<string, string> _attributeType; //<EdgeViewAttribute> => <Type>

        //For dropping edge view
        private static readonly List<string> EdgeViewFunctionList =
            new List<string>()
            {
                "Decoder",
                "ExclusiveEdgeGenerator",
                "bfsPath",
                "bfsPathWithMessage",
                "PathMessageEncoder",
                "PathMessageDecoder"
            };
        
    }
}
