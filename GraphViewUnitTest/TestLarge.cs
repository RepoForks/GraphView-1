﻿#define LOCALTEST

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using GraphView;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GraphViewUnitTest.Gremlin;

namespace GraphViewUnitTest
{
    [TestClass]
    public class TestLarge
    {
#if LOCALTEST
        private const string DOCDB_URL = "https://localhost:8081/";
        private const string DOCDB_AUTHKEY = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private const string DOCDB_DATABASE = "Wenbin";
#else
        private const string DOCDB_URL = "https://graphview.documents.azure.com:443/";
        private const string DOCDB_AUTHKEY = "MqQnw4xFu7zEiPSD+4lLKRBQEaQHZcKsjlHxXn2b96pE/XlJ8oePGhjnOofj1eLpUdsfYgEhzhejk2rjH/+EKA==";
        private const string DOCDB_DATABASE = "Temperary";
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DocumentDBConnection CreateConnection(string tips = null, int? edgeSpillThreshold = null)
        {
            StackFrame frame = new StackFrame(1);
            if (!string.IsNullOrEmpty(tips)) {
                tips = $" {tips}";
            }
            string collectionName = $"[{frame.GetMethod().Name}]{tips}";

            DocumentDBConnection connection = DocumentDBConnection.ResetGraphAPICollection(DOCDB_URL, DOCDB_AUTHKEY, DOCDB_DATABASE, collectionName, AbstractGremlinTest.TEST_USE_REVERSE_EDGE, AbstractGremlinTest.TEST_SPILLED_EDGE_THRESHOLD_VIAGRAPHAPI);

            return connection;
        }

        [TestMethod]
        public void TestModernGraph_Dummy()
        {
            DocumentDBConnection connection = CreateConnection();
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("person").Property("age", "27").Property("name", "vadas").Next();
            graph.g().AddV("person").Property("age", "29").Property("name", "marko").Next();
            graph.g().AddV("person").Property("age", "35").Property("name", "peter").Next();
            graph.g().AddV("person").Property("age", "32").Property("name", "josh").Next();
            graph.g().AddV("software").Property("lang", "java").Property("name", "lop").Next();
            graph.g().AddV("software").Property("lang", "java").Property("name", "ripple").Next();

            graph.g().V().Has("name", "marko").AddE("knows").Property("weight", 0.5).To(graph.g().V().Has("name", "vadas")).Next();
            graph.g().V().Has("name", "marko").AddE("knows").Property("weight", 1.0).To(graph.g().V().Has("name", "josh")).Next();
            graph.g().V().Has("name", "marko").AddE("knows").Property("weight", 0.4).To(graph.g().V().Has("name", "lop")).Next();
            graph.g().V().Has("name", "peter").AddE("created").Property("weight", 0.2).To(graph.g().V().Has("name", "lop")).Next();
            graph.g().V().Has("name", "josh").AddE("created").Property("weight", 0.4).To(graph.g().V().Has("name", "lop")).Next();
            graph.g().V().Has("name", "josh").AddE("created").Property("my", "josh2ripple").Property("weight", 1.0).To(graph.g().V().Has("name", "ripple")).Next();
        }

        [TestMethod]
        public void TestDocDBLimit()
        {
            DocumentDBConnection connection = CreateConnection();
            GraphViewCommand graph = new GraphViewCommand(connection);

            string largeProperty = new string('_', 1024 * 1024 * 2 - 1024);  // Size = 2MB
            graph.g().AddV("dummy").Property("large", largeProperty).Next();
        }


        [TestMethod]
        [Ignore]  // Ignored because this test is TOO slow!
        public void TestAddHeavyEdges_LargeQuantity()
        {
            const int EDGE_COUNT = 100;

            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                Debug.WriteLine($"AddEdge: i = {i}");
                string longLabel = new string('-', 1024 * 10 * i);  // Size = 10-1000KB
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: longLabel).To(graph.g().V().HasLabel("SinkV")).Next();
            }
        }

        [TestMethod]
        public void TestAddHeavyEdges_MidiumQuantity()
        {
            const int EDGE_COUNT = 10;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                string longLabel = new string((char)('0' + i), 1024 * 900);  // Size = 900KB
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: longLabel).To(graph.g().V().HasLabel("SinkV")).Next();
            }
        }

        [TestMethod]
        public void TestAddHeavyEdges_SmallQuantity()
        {
            const int EDGE_COUNT = 3;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                string longLabel = new string((char)('0' + i), 1024 * 900);  // Size = 900KB
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: longLabel).To(graph.g().V().HasLabel("SinkV")).Next();
            }
        }


        [TestMethod]
        public void TestAddAndDropEdges_Small()
        {
            const int EDGE_COUNT = 10;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: "Dummy").To(graph.g().V().HasLabel("SinkV")).Next();
            }
            graph.g().E().HasLabel("Dummy").Drop().Next();
        }


        [TestMethod]
        public void TestAddAndDropEdges_DropAll_Small()
        {
            const int EDGE_COUNT = 10;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE("dummyLabel").To(graph.g().V().HasLabel("SinkV")).Next();
            }
            graph.g().V().OutE().Drop().Next();
        }

        [TestMethod]
        public void TestAddAndDropEdges_DropSome_Small()
        {
            const int EDGE_COUNT = 20;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: $"MOD_{i % 4}").To(graph.g().V().HasLabel("SinkV")).Next();
            }
            graph.g().V().OutE().HasLabel("MOD_0", "MOD_2").Drop().Next();
        }

        [TestMethod]
        public void TestAddDropEdges_DropAll_Large()
        {
            const int EDGE_COUNT = 10;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                string label = $"{i}@{new string('_', 1024 * 900)}";
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: label).To(graph.g().V().HasLabel("SinkV")).Next();
            }
            graph.g().V().OutE().Drop().Next();
        }

        [TestMethod]
        public void TestAddDropEdges_DropSome_Large()
        {
            const int EDGE_COUNT = 30;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}", 4);
            GraphViewCommand graph = new GraphViewCommand(connection);
            string suffix = new string('_', 10);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE(edgeLabel: $"MOD_{i % 4}{suffix}").To(graph.g().V().HasLabel("SinkV")).Next();
            }
            graph.g().V().OutE().HasLabel($"MOD_0{suffix}", $"MOD_2{suffix}").Drop().Next();
        }

        [TestMethod]
        public void TestDropNodes_Small()
        {
            const int EDGE_COUNT = 5;
            DocumentDBConnection connection = CreateConnection($"(A->B->C, A->C) E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);
            string suffix = string.Empty;

            graph.g().AddV("A").Next();
            graph.g().AddV("B").Next();
            graph.g().AddV("C").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("A").AddE(edgeLabel: $"AB_{i}{suffix}").To(graph.g().V().HasLabel("B")).Next();
                graph.g().V().HasLabel("A").AddE(edgeLabel: $"AC_{i}{suffix}").To(graph.g().V().HasLabel("C")).Next();
                graph.g().V().HasLabel("B").AddE(edgeLabel: $"BC_{i}{suffix}").To(graph.g().V().HasLabel("C")).Next();
            }
            graph.g().V().HasLabel("B").Drop().Next();
        }


        [TestMethod]
        public void TestDropNodes_Large()
        {
            const int EDGE_COUNT = 10;
            DocumentDBConnection connection = CreateConnection($"(A->B->C, A->C) E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);
            string suffix = new string('_', 1024);

            graph.g().AddV("A").Next();
            graph.g().AddV("B").Next();
            graph.g().AddV("C").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("A").AddE(edgeLabel: $"AB_{i}{suffix}").To(graph.g().V().HasLabel("B")).Next();
                graph.g().V().HasLabel("A").AddE(edgeLabel: $"AC_{i}{suffix}").To(graph.g().V().HasLabel("C")).Next();
                graph.g().V().HasLabel("B").AddE(edgeLabel: $"BC_{i}{suffix}").To(graph.g().V().HasLabel("C")).Next();
            }
            graph.g().V().HasLabel("B").Drop().Next();
        }

        [TestMethod]
        public void TestChangeEdgeProperties_Small()
        {
            const int EDGE_COUNT = 4;
            DocumentDBConnection connection = CreateConnection($"{MethodBase.GetCurrentMethod().Name} E={EDGE_COUNT}");
            GraphViewCommand graph = new GraphViewCommand(connection);
            string suffix = string.Empty;

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE($"E{i}").Property($"E{i} Property", $"E{i} PropValue").To(graph.g().V().HasLabel("SinkV")).Next();
            }

            for (int i = 0; i < EDGE_COUNT; i++) {
                if (i % 2 == 1) {
                    graph.g().V().OutE().HasLabel($"E{i}").Property($"E{i} Property", null).Next();
                    graph.g().V().OutE().HasLabel($"E{i}").Property($"E{i} Another Property", "Dummy!").Next();
                }
            }
        }

        [TestMethod]
        public void TestChangeEdgeProperties_Large()
        {
            const int EDGE_COUNT = 10;
            DocumentDBConnection connection = CreateConnection($"{MethodBase.GetCurrentMethod().Name} E={EDGE_COUNT}", 1);
            GraphViewCommand graph = new GraphViewCommand(connection);
            string suffix = new string('_', 1024);

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE($"E{i}{suffix}").Property($"E{i} Property", $"E{i} PropValue").To(graph.g().V().HasLabel("SinkV")).Next();
            }

            for (int i = 0; i < EDGE_COUNT; i++) {
                if (i % 2 == 1) {
                    graph.g().V().OutE().HasLabel($"E{i}{suffix}").Property($"E{i} Property", null).Next();
                    graph.g().V().OutE().HasLabel($"E{i}{suffix}").Property($"E{i} Another Property", "Dummy!").Next();
                }
            }
        }


        [TestMethod]
        public void TestChangeEdgeProperties_Large2Larger()
        {
            const int EDGE_COUNT = 20;
            DocumentDBConnection connection = CreateConnection($"E={EDGE_COUNT}", 5);
            GraphViewCommand graph = new GraphViewCommand(connection);
            string suffix = new string('_', 1024);  // 1 KB
            string suffix2 = new string('_', 10240);  // 10KB

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE($"E{i}{suffix}").Property($"E{i} Property", $"E{i} PropValue").To(graph.g().V().HasLabel("SinkV")).Next();
            }

            // Drop all added properties
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().OutE().HasLabel($"E{i}{suffix}").Property($"E{i} Property", null).Next();
            }

            // Add new property
            // Each document can only hold ONE edge-object!
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().OutE().HasLabel($"E{i}{suffix}").Property($"E{i} Another Property", "Dummy!").Next();
            }
        }


        [TestMethod]
        public void TestSelfLoop_Small()
        {
            DocumentDBConnection connection = CreateConnection($"");
            GraphViewCommand graph = new GraphViewCommand(connection);

            graph.g().AddV("Self").AddE("SelfLoop1").To(graph.g().V().HasLabel("Self")).Next();
            graph.g().V().HasLabel("Self").AddE("SelfLoop2").Next();
            //graph.g().AddV("Self").AddE("SelfLoop===").Next();
        }

        [TestMethod]
        public void TestSpillThreshold_Small()
        {
            const int EDGE_COUNT = 20;
            const int THRESHOLD = 6;
            DocumentDBConnection connection = CreateConnection($"Threshold={THRESHOLD},E={EDGE_COUNT}", THRESHOLD);
            GraphViewCommand graph = new GraphViewCommand(connection);

            Console.WriteLine($"EdgeSpillThreashold: {connection.EdgeSpillThreshold}");
            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE($"...E{i}...").Property($"E{i} Property", $"E{i} PropValue").To(graph.g().V().HasLabel("SinkV")).Next();
            }
        }


        [TestMethod]
        public void TestSpillThreshold_Large()
        {
            const int EDGE_COUNT = 7;
            const int THRESHOLD = 2;
            string prefix = new string('_', 1024 * 600);
            DocumentDBConnection connection = CreateConnection($"Threshold={THRESHOLD},E={EDGE_COUNT}", THRESHOLD);
            GraphViewCommand graph = new GraphViewCommand(connection);
            Console.WriteLine($"EdgeSpillThreashold: {connection.EdgeSpillThreshold}");

            graph.g().AddV("SourceV").Next();
            graph.g().AddV("SinkV").Next();
            for (int i = 0; i < EDGE_COUNT; i++) {
                graph.g().V().HasLabel("SourceV").AddE($"...E{i}..{prefix}.").Property($"E{i} Property", $"E{i} PropValue").To(graph.g().V().HasLabel("SinkV")).Next();
            }
        }
    }
}
