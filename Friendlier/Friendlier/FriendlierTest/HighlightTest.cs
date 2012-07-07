using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xyglo;
using Microsoft.Xna.Framework;

namespace TestProject1
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class HighlightTest
    {
        /// <summary>
        /// Our test Project
        /// </summary>
        Project m_project;

        /// <summary>
        /// Our test FileBuffer
        /// </summary>
        FileBuffer m_fileBuffer;

        /// <summary>
        /// A File Position
        /// </summary>
        FilePosition m_fp1;

        /// <summary>
        /// A screen position
        /// </summary>
        ScreenPosition m_hl1;

        /// <summary>
        /// List of commands so that we can test undo/redo
        /// </summary>
        List<Command> m_commands;

        [TestInitialize]
        public void initialise()
        {
            // Init Project and FileBuffer
            //
            m_project = new Project();
            m_fileBuffer = new FileBuffer();

            // Init positions
            //
            m_fp1 = new FilePosition(0, 0);
            m_hl1 = new ScreenPosition(-1, -1);

            // Initialise the commands list
            //
            m_commands = new List<Command>();
        }

        public HighlightTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void HighlightingList()
        {
            // TODO: Add test logic here
            //
            List<Highlight> hlList = new List<Highlight>();

            Highlight hl1 = new Highlight(0, 0, 4, "test", HighlightType.Keyword);
            Highlight hl2 = new Highlight(0, 0, 4, "test", HighlightType.Keyword);

            hlList.Add(hl1);

            // Does the list contain the Highlight
            //
            Assert.IsTrue(hlList.Contains(hl1));

            // Add the same highlight again
            //
            hlList.Add(hl2);

            // Do we have two entries?
            //
            Assert.IsTrue(hlList.Count == 2);

            // Create a distinct list
            //
            List<Highlight> distinctList = hlList.Distinct().ToList();

            // Do we have one distinct version?
            //
            Assert.IsTrue(distinctList.Count == 1);

            // Add a couple more
            //
            distinctList.Add(new Highlight(0, 10, 50, "ABCEDEFGHI", HighlightType.Define));
            distinctList.Add(new Highlight(10, 0, 5, "Hello", HighlightType.Comment));
            distinctList.Add(new Highlight(1, 50, 55, "Trust", HighlightType.Keyword));
            distinctList.Add(new Highlight(5, 10, 15, "Small", HighlightType.UserHighlight));

            // Sort them
            //
            distinctList.Sort();

            //distinctList.Sort(delegate(Highlight h1, Highlight h2) { return h1.CompareTo(h2); });

            // Assert beginning of list
            //
            Assert.IsTrue(distinctList[0].m_startHighlight.Y == 0);

            // Length of list
            Assert.IsTrue(distinctList.Count == 5);

            // Test last member - test that sorting works
            //
            Assert.IsTrue(distinctList[4].m_startHighlight.Y == 10);
        }
    }
}
