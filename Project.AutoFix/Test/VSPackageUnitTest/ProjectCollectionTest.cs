﻿//-----------------------------------------------------------------------
// <copyright file="ProjectCollectionTest.cs">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
//-----------------------------------------------------------------------
namespace VSPackageUnitTest
{
    using System.Collections;
    using EnvDTE;
    using StyleCop.VisualStudio;
    using Microsoft.VisualStudio.TestTools.MockObjects;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///This is a test class for ProjectCollectionTest and is intended
    ///to contain all ProjectCollectionTest Unit Tests
    ///</summary>
    [TestClass()]
    [DeploymentItem("Microsoft.VisualStudio.QualityTools.MockObjectFramework.dll")]
    [DeploymentItem("StyleCop.VSPackage.dll")]
    public class ProjectCollectionTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get;
            set;
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///A test for GetEnumerator
        ///</summary>
        [TestMethod()]
        public void VsGetEnumeratorSelectedProjectsTest()
        {
            ProjectCollection target = new ProjectCollection();
            Mock<IEnumerable> mockEnumerable = new Mock<IEnumerable>();
            Mock<IEnumerator> mockEnumerator = new Mock<IEnumerator>();
            IEnumerator expected = mockEnumerator.Instance;
            mockEnumerable.ImplementExpr(e => e.GetEnumerator(), expected);
            target.SelectedProjects = mockEnumerable.Instance;
            IEnumerator actual;
            actual = target.GetEnumerator();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for GetEnumerator
        ///</summary>
        [TestMethod()]
        public void VsGetEnumeratorSolutionProjectsTest()
        {
            ProjectCollection target = new ProjectCollection();
            Mock<IEnumerable> mockEnumerable = new Mock<IEnumerable>();
            Mock<IEnumerator> mockEnumerator = new Mock<IEnumerator>();
            Mock<Projects> mockProjects = new Mock<Projects>();
            IEnumerator expected = mockEnumerator.Instance;
            mockProjects.ImplementExpr(p => p.GetEnumerator(), expected);
            mockEnumerable.ImplementExpr(e => e.GetEnumerator(), expected);
            target.SolutionProjects = mockProjects.Instance;
            IEnumerator actual;
            actual = target.GetEnumerator();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for GetEnumerator
        ///</summary>
        [TestMethod()]
        public void VsGetEnumeratorNullTest()
        {
            ProjectCollection target = new ProjectCollection();
            IEnumerator actual;
            actual = target.GetEnumerator();
            Assert.IsNull(actual);
        }

        /// <summary>
        ///A test for SelectedProjects
        ///</summary>
        [TestMethod()]
        public void VsSelectedProjectsTest()
        {
            ProjectCollection target = new ProjectCollection();
            IEnumerable expected = new Mock<IEnumerable>().Instance;
            IEnumerable actual;
            target.SelectedProjects = expected;
            actual = target.SelectedProjects;
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for SolutionProjects
        ///</summary>
        [TestMethod()]
        public void VsSolutionProjectsTest()
        {
            ProjectCollection target = new ProjectCollection();
            Mock<Projects> mockProjects = new Mock<Projects>();
            Projects expected = mockProjects.Instance;
            Projects actual;
            target.SolutionProjects = expected;
            actual = target.SolutionProjects;
            Assert.AreEqual(expected, actual);
        }
    }
}
