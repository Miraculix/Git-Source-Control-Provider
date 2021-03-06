﻿using System;
using System.IO;
using System.Linq;
using GitScc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BasicSccProvider.Tests
{
    /// <summary>
    ///This is a test class for GitFileStatusTrackerTest and is intended
    ///to contain all GitFileStatusTrackerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GitFileStatusTrackerTest
    {
        protected string tempFolder;
        protected string tempFile;
        protected string[] lines;
        
        public GitFileStatusTrackerTest()
        {
            tempFolder = Environment.CurrentDirectory + "\\" + Guid.NewGuid().ToString();
            tempFile = Path.Combine(tempFolder, "test");
            lines = new string[] { "First line", "Second line", "Third line" };
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

        [TestMethod()]
        public void GetRepositoryDirectoryTest()
        {
            GitFileStatusTracker.Init(tempFolder);
            var newFolder = tempFolder + "\\t t\\a a";
            Directory.CreateDirectory(newFolder);
            GitFileStatusTracker tracker = new GitFileStatusTracker(newFolder);
            Assert.AreEqual(tempFolder, tracker.GitWorkingDirectory);
        }

        [TestMethod()]
        public void HasGitRepositoryTest()
        {
            
            GitFileStatusTracker.Init(tempFolder);
            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);

            Assert.IsTrue(tracker.HasGitRepository);
            Assert.AreEqual(tempFolder, tracker.GitWorkingDirectory);
            Assert.IsTrue(Directory.Exists(tempFolder + "\\.git"));
        }

        [TestMethod]
        public void GetFileStatusTest()
        {
            GitFileStatusTracker.Init(tempFolder);
            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);

            File.WriteAllLines(tempFile, lines);
            Assert.AreEqual(GitFileStatus.New, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Added, tracker.GetFileStatus(tempFile));

            tracker.UnStageFile(tempFile);
            Assert.AreEqual(GitFileStatus.New, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Added, tracker.GetFileStatus(tempFile));

            tracker.Commit("test commit");
            Assert.AreEqual(GitFileStatus.Tracked, tracker.GetFileStatus(tempFile));

            File.WriteAllText(tempFile, "changed text");
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Modified, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Staged, tracker.GetFileStatus(tempFile));

            tracker.UnStageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Modified, tracker.GetFileStatus(tempFile));

            File.Delete(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Deleted, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Removed, tracker.GetFileStatus(tempFile));

            tracker.UnStageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Deleted, tracker.GetFileStatus(tempFile));
        }

        [TestMethod]
        public void GetFileContentTest()
        {
            GitFileStatusTracker.Init(tempFolder);
            File.WriteAllLines(tempFile, lines);

            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("test");

            var fileContent = tracker.GetFileContent(tempFile);

            using (var binWriter = new BinaryWriter(File.Open(tempFile + ".bk", System.IO.FileMode.Create)))
            {
                binWriter.Write(fileContent);
            }

            var newlines = File.ReadAllLines(tempFile + ".bk");
            Assert.AreEqual(lines[0], newlines[0]);
            Assert.AreEqual(lines[1], newlines[1]);
            Assert.AreEqual(lines[2], newlines[2]);
        }

        [TestMethod]
        public void GetFileContentTestNegative()
        {
            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);
            var fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);

            GitFileStatusTracker.Init(tempFolder);

            File.WriteAllLines(tempFile, lines);
            tracker = new GitFileStatusTracker(tempFolder);
            fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);

            tracker.StageFile(tempFile);
            fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);

            tracker.Commit("test");

            fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);
        }

        [TestMethod]
        public void GetChangedFilesTest()
        {
            GitFileStatusTracker.Init(tempFolder);

            File.WriteAllLines(tempFile, lines);

            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);
            Assert.AreEqual(GitFileStatus.New, tracker.ChangedFiles.ToList()[0].Status);

            tracker.StageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Added, tracker.ChangedFiles.ToList()[0].Status);

            tracker.Commit("test");
            
            Assert.AreEqual(0, tracker.ChangedFiles.Count());

            File.WriteAllText(tempFile, "a");
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Modified, tracker.ChangedFiles.ToList()[0].Status);

            tracker.StageFile(tempFile);
            Assert.AreEqual(GitFileStatus.Staged, tracker.ChangedFiles.ToList()[0].Status);
        }

        [TestMethod]
        public void LastCommitMessageTest()
        {
            GitFileStatusTracker.Init(tempFolder);
            File.WriteAllLines(tempFile, lines);

            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);
            tracker.StageFile(tempFile);
            
            tracker.Commit("test message");
            Assert.IsTrue(tracker.LastCommitMessage.StartsWith("test message"));
        }

        [TestMethod]
        public void AmendCommitTest()
        {
            GitFileStatusTracker.Init(tempFolder);
            File.WriteAllLines(tempFile, lines);

            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);
            tracker.StageFile(tempFile);

            tracker.Commit("test message");
            Assert.IsTrue(tracker.LastCommitMessage.StartsWith("test message"));

            File.WriteAllText(tempFile, "changed text");
            tracker.StageFile(tempFile);
            tracker.AmendCommit("new message");
            Assert.IsTrue(tracker.LastCommitMessage.StartsWith("new message"));
        }

        [TestMethod]
        public void DiffFileTest()
        {
            GitFileStatusTracker.Init(tempFolder);
            File.WriteAllLines(tempFile, lines);

            GitFileStatusTracker tracker = new GitFileStatusTracker(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("test message");
            File.WriteAllText(tempFile, "changed text");
            var diff = tracker.DiffFile(tempFile);
            Console.WriteLine(diff);
            Assert.IsTrue(diff.StartsWith("@@ -1,3 +1 @@"));
        }
    }

    [TestClass()]
    public class GitFileStatusTrackerTest_WithSubFolder : GitFileStatusTrackerTest
    {
        public GitFileStatusTrackerTest_WithSubFolder()
        {
            GitBash.GitExePath = null;
            tempFolder = Environment.CurrentDirectory + "\\" + Guid.NewGuid().ToString();
            Directory.CreateDirectory(Path.Combine(tempFolder, "中文 1č"));
            tempFile = Path.Combine(tempFolder, "中文 1č\\testč");
        }
    }

    [TestClass()]
    public class GitFileStatusTrackerTest_WithSubFolder_UsingGitBash : GitFileStatusTrackerTest
    {
        public GitFileStatusTrackerTest_WithSubFolder_UsingGitBash()
        {
            GitBash.GitExePath = @"C:\Program Files (x86)\Git\bin\sh.exe";
            tempFolder = Environment.CurrentDirectory + "\\" + Guid.NewGuid().ToString();
            Directory.CreateDirectory(Path.Combine(tempFolder, "folder 中文 čćšđžČĆŠĐŽ"));
            tempFile = Path.Combine(tempFolder, "folder 中文 čćšđžČĆŠĐŽ\\test");
        }
    }
}
