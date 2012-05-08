namespace dropkick.tests.Configuration.Dsl
{
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using TestObjects;
    using dropkick.Configuration.Dsl.Http;
    using dropkick.DeploymentModel;
    using System.Linq;

    public class BaseHttpTaskTest
    {
        protected TestPhysicalServer _physicalServer;
        protected TestProtoServer _protoServer;
        protected static string _physicalServerName = "should be set in test";
        protected string _tempFileName;

        [SetUp]
        public void Setup()
        {
           
            _protoServer = new TestProtoServer();
            HUB.Settings = new object();
        }

        [TearDown]
        public void TearDown()
        {
            if(File.Exists(_tempFileName))
            {
                File.Delete(_tempFileName);
            }
        }

        protected static TestPhysicalServer CreateNewPhysicalServer()
        {
            return new TestPhysicalServer
            {
                Name = _physicalServerName
            };
        }

        protected string CreateTempFile(string fileContent)
        {
            _tempFileName = Path.GetTempFileName();
            File.WriteAllText(_tempFileName, fileContent);
            return _tempFileName;
        }
    }

    [TestFixture]
    [Category("Integration")]
    public class ExecuteHttpTask : BaseHttpTaskTest
    {
       
        [Test]
        public void should_be_good_even_though_status_code_is_not_success()
        {
            _physicalServerName = "google.com";
            _protoServer.HttpGet()
                .Path("not existing");

            Assert.AreNotEqual(0, ExecuteStatusses().Count);
            ExecuteStatusses().All(ds => ds == DeploymentItemStatus.Good).ShouldBeTrue();
        }

        [Test]
        public void should_not_be_good_when_expected_status_code_does_not_match()
        {
            _physicalServerName = "google.com";
            _protoServer.HttpGet()
                .Path("not existing")
                .ExpectSuccessStatusCode();

            ExecuteStatusses().ShouldContain(DeploymentItemStatus.Alert);
        }

        [Test]
        public void should_be_good_when_status_code_matches()
        {
            _physicalServerName = "google.com";
            _protoServer.HttpGet()
                .ExpectSuccessStatusCode();

            ExecuteStatusses().All(es=> es == DeploymentItemStatus.Good).ShouldBeTrue();
        }
        
        [Test]
        public void should_warn_when_content_contains_invalid_words()
        {
            _physicalServerName = "www.google.pl";
            _protoServer.HttpGet()
                .InvalidWordsAre("html");

            ExecuteStatusses().ShouldContain(DeploymentItemStatus.Alert);
            ExecuteMessages().Any(ms=> ms.Contains("html")).ShouldBeTrue();
        }


        private DeploymentResult ExecuteTask()
        {
            _physicalServer = CreateNewPhysicalServer();
            _protoServer.ProtoTask.RegisterRealTasks(_physicalServer);
            return _physicalServer.Task.Execute();
        }

        List<DeploymentItemStatus> ExecuteStatusses()
        {
            return ExecuteTask().Select(vt => vt.Status).ToList();
        }

        List<string> ExecuteMessages()
        {
            return ExecuteTask().Results.Select(r => r.Message).ToList();
        }
    }

    [TestFixture]
    [Category("Integration")]
    public class VerifyHttpTask : BaseHttpTaskTest
    {
        private DeploymentResult VerifyTask()
        {
            _physicalServer = CreateNewPhysicalServer();
            _protoServer.ProtoTask.RegisterRealTasks(_physicalServer);
            return _physicalServer.Task.VerifyCanRun();
        }

      

        [Test]
        public void should_build_valid_url_from_machine_name()
        {
            _physicalServerName = "test-server.com";
            _protoServer.HttpGet();            
            VerifyStatusses().ShouldContain(DeploymentItemStatus.Good);
            VerifyMessages().Any(s=> s.Contains("http://test-server.com")).ShouldBeTrue();
        }        

        [Test]
        public void should_warn_when_uri_is_not_valid()
        {
            _physicalServerName = "invalid host name";
            _protoServer.HttpGet();
            VerifyStatusses().ShouldContain(DeploymentItemStatus.Alert);
        }

        [Test]
        public void should_append_path_properly()
        {
            _physicalServerName = "test.com";
            _protoServer.HttpGet()
                .Path("/some/fancy/path");
            VerifyMessages().Any(s => s.Contains("http://test.com/some/fancy/path")).ShouldBeTrue();
        }

        [Test]
        public void should_check_if_file_with_base_uri_exists()
        {
            _physicalServerName = "test";            
            _protoServer.HttpGet()
                .BaseUriIsInFile("not existing file", "");
            VerifyStatusses().ShouldContain(DeploymentItemStatus.Alert);
        }

        [Test]
        public void should_complain_that_the_file_is_not_valid_xml()
        {
            _physicalServerName = "test";
            _protoServer.HttpGet()
                .BaseUriIsInFile(CreateTempFile(""), "");

            VerifyStatusses().ShouldContain(DeploymentItemStatus.Alert);
        }

        [Test]
        public void should_extract_xpath_value_to_use_as_base_uri()
        {
            _physicalServerName = "test";
            _protoServer.HttpGet()
                .BaseUriIsInFile(CreateTempFile("<config><target Uri=\"http://base.uri\" /></config>"),
                                 "/config/target/@Uri")
                .Path("withPath");
            VerifyMessages().Any(m => m.Contains("http://base.uri/withPath")).ShouldBeTrue();
        }

        [Test]
        public void should_warn_that_given_xpath_didn_get_value()
        {
            _physicalServerName = "test";
            _protoServer.HttpGet()
                .BaseUriIsInFile(CreateTempFile("<config><target Uri=\"http://base.uri\" /></config>"),
                                 "/config/target/@UriBad")
                .Path("withPath");
            VerifyStatusses().ShouldContain(DeploymentItemStatus.Alert);
        }


        List<DeploymentItemStatus> VerifyStatusses()
        {
            return VerifyTask().Select(vt => vt.Status).ToList();
        }

        List<string> VerifyMessages()
        {
            return VerifyTask().Results.Select(r => r.Message).ToList();
        }
    }
}