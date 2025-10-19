using Xunit;
using RDotNet.Devices;
using System;
using System.IO;
using System.Reflection;

namespace RDotNet
{
    [Collection("R.NET unit tests")]
    public class REngineInstanceTest
    {
        //[SetUp]
        public void SetUp()
        {
            REngine.SetEnvironmentVariables();
        }

        [Fact(Skip = "This does not pass - kept as is until dynamic-interop changes the exception type to ArgumentException")]
        public void TestCreateInstanceWithWrongDllName()
        {
            Assert.Throws<ArgumentException>(
                () => {
                    TestREngine.CreateTestEngine("R.NET", "NotExist.dll");
                });
        }

        /// <summary>
        /// A facility to test REngine behavior in unit tests without needing to dispose of the REngine singleton
        /// </summary>
        private class TestREngine : REngine
        {
            public static TestREngine CreateTestEngine(string id, string dll = null)
            {
                dll = ProcessRDllFileName(dll); // as is done in REngine.CreateInstance. not ideal; rethink.
                return new TestREngine(id, dll);
            }

            public TestREngine(string id, string dll)
                : base(id, dll) { }
        }

        [Fact(Skip ="cannot test this easily with new API. Rethink")] // cannot test this easily with new API. Rethink
        public void TestIsRunning()
        {
            var engine = REngine.GetInstance();
            Assert.NotNull(engine);
            Assert.False(engine.IsRunning);
            engine.Initialize();
            Assert.True(engine.IsRunning);
            engine.Dispose();
            Assert.False(engine.IsRunning);
        }

        // Marking this test as ignore, as it is incompatible with trying to get all unit tests
        // run from NUnit to pass successfully.
        // Keeping the test code as a basis for potential further feasibility investigations
        public void TestCreateEngineTwice()
        {
            // Investigate:
            // https://rdotnet.codeplex.com/workitem/54
            var engine = REngine.GetInstance();
            engine.Initialize();
            var paths = engine.Evaluate(".libPaths()").AsCharacter().ToArray();
            Console.WriteLine(engine.Evaluate("Sys.getenv('R_HOME')").AsCharacter().ToArray()[0]);
            // engine.Evaluate("library(rjson)");
            engine.Dispose();
            Console.WriteLine("Before second creation");
            engine = REngine.GetInstance();
            Console.WriteLine("Before second initialize");
            engine.Initialize();
            Console.WriteLine(engine.Evaluate("Sys.getenv('R_HOME')").AsCharacter().ToArray()[0]);
            paths = engine.Evaluate(".libPaths()").AsCharacter().ToArray();
            try
            {
                engine.Evaluate("library(methods)");
            }
            catch
            {
                engine.Evaluate("traceback()");
                throw;
            }
            finally
            {
                engine.Dispose();
            }
            Assert.False(engine.IsRunning);
        }

        public class Job : MarshalByRefObject
        {
            // uses R.NET here
            public void Execute()
            {
                var engine = InitREngine();
                engine.Evaluate("x <- 5");
            }

            // initializes REngine
            private REngine InitREngine()
            {
                var engine = REngine.GetInstance(initialize: false);
                engine.Initialize(null, new NullCharacterDevice(), setupMainLoop: false);  // real char device?
                //AppDomain.CurrentDomain.DomainUnload += (EventHandler)((o, e) => engine.Dispose());
                return engine;
            }
        }

        // Disabling unit tests that were looking at multiple appdomains
        //AppDomain is Not part of the .NET core 2.0 specs:
        // //https://docs.microsoft.com/en-us/dotnet/api/?term=AppDomainSetup&view=netcore-2.0

        //[Fact]
        //public void TestMultipleAppDomains()
        //{
        //    var e = REngine.GetInstance(); // need to trigger the R main loop setup once, and it may as well be in the default appdomain
        //    TestAppDomain("test1");  // works
        //    TestAppDomain("test2");  // hangs at the last line in Job.Execute()
        //}

        //public static void TestAppDomain(string jobName)
        //{
        //    var domainSetup = new AppDomainSetup();
        //    domainSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //    AppDomain ad = AppDomain.CreateDomain(jobName, AppDomain.CurrentDomain.Evidence, domainSetup);

        //    var type = typeof(Job);
        //    var jd = (Job)ad.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName, true, BindingFlags.CreateInstance, null,
        //                        new object[] { }, null, null);
        //    jd.Execute();
        //    AppDomain.Unload(ad);
        //}

        //[Test, Ignore("Running in several application domains to load plug-ins not yet feasible?")] // TODO
        //public void TestSeveralAppDomains()
        //{
        //    var engine = REngine.GetInstance();
        //    engine.Initialize();

        //    // create another AppDomain for loading the plug-ins
        //    AppDomainSetup setup = new AppDomainSetup();
        //    setup.ApplicationBase = Path.GetDirectoryName(typeof(REngine).Assembly.Location);

        //    setup.DisallowApplicationBaseProbing = false;
        //    setup.DisallowBindingRedirects = false;

        //    var domain = AppDomain.CreateDomain("Plugin AppDomain", null, setup);

        //    domain.Load(typeof(REngine).Assembly.EscapedCodeBase);
        //}
    }
}