using RDotNet;
using RDotNet.NativeLibrary;
using System;
using System.Collections;

namespace SimpleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            github_issue_131();
            return;

            string rHome = @"C:\Progra~1\R\R-36~1.0";
            string rPath = rHome + @"\bin\x64";
            if (args.Length > 0)
                rHome = args[0];
            else
            {
                rHome = @"C:\Progra~1\R\R-36~1.0";
                rPath = rHome + @"\bin\x64";
            }
            if (args.Length > 1)
            {
                rPath = rHome + @"\bin\" + args[1];
            }
            REngine.SetEnvironmentVariables(rPath, rHome);
            using (REngine e = REngine.GetInstance())
            {
                ReproGH97(e);
            }


            //string rHome = null;
            //string rPath = null;
            //if (args.Length > 0)
            //    rPath = args[0];
            //if (args.Length > 1)
            //    rHome = args[1];

            //NativeUtility util = new NativeUtility();
            //Console.WriteLine(util.FindRPaths(ref rPath, ref rHome));

            //rHome = null;
            //rPath = null;

            //REngine.SetEnvironmentVariables(rPath: rPath, rHome: rHome);
            //REngine e = REngine.GetInstance();

            //Console.WriteLine(NativeUtility.SetEnvironmentVariablesLog);

            //counter = 0;
            //for (int i = 0; i < 6; i++)
            //    TestDataFrameInMemoryCreation(e);

            //for (int i = 0; i < 6; i++)
            //    TestCallStop(e);

            //e.Dispose();
        }

        private static void ReproGH97(REngine engine)
        {
            // https://github.com/jmp75/rdotnet/issues/97
            SymbolicExpression expression;

            var log = NativeUtility.SetEnvironmentVariablesLog;
            engine.Initialize();
            engine.Evaluate("x <- data.frame(c1 = c('a', 'b'), stringsAsFactors = FALSE)");
            engine.Evaluate("y <- data.frame(x = c('a', 'b'), stringsAsFactors = TRUE)");
            engine.Evaluate("c1 <- x$c1");

            expression = engine.GetSymbol("x");
            Console.WriteLine("Values as characters:");
            Console.WriteLine(expression.AsDataFrame()[0][0]);
            Console.WriteLine(expression.AsDataFrame()[0][1]);
            Console.WriteLine("*********************");
            expression = engine.GetSymbol("y");
            Console.WriteLine("Values as factor:");
            Console.WriteLine(expression.AsDataFrame()[0][0]);
            Console.WriteLine(expression.AsDataFrame()[0][1]);
            Console.WriteLine("*********************");
            expression = engine.GetSymbol("c1");
            Console.WriteLine("Values direct from column:");
            Console.WriteLine(expression.AsCharacter()[0]);
            Console.WriteLine(expression.AsCharacter()[1]);
            Console.WriteLine("*********************");

            Console.WriteLine("");
            Console.WriteLine("*********************");
            Console.WriteLine("Now going for a second round");
            Console.WriteLine("*********************");
            expression = engine.GetSymbol("x");
            Console.WriteLine("Values as characters:");
            Console.WriteLine(expression.AsDataFrame()[0][0]);
            Console.WriteLine(expression.AsDataFrame()[0][1]);
            Console.WriteLine("*********************");
            expression = engine.GetSymbol("y");
            Console.WriteLine("Values as factor:");
            Console.WriteLine(expression.AsDataFrame()[0][0]);
            Console.WriteLine(expression.AsDataFrame()[0][1]);
            Console.WriteLine("*********************");
            expression = engine.GetSymbol("c1");
            Console.WriteLine("Values direct from column:");
            Console.WriteLine(expression.AsCharacter()[0]);
            Console.WriteLine(expression.AsCharacter()[1]);
            Console.WriteLine("*********************");

            Console.WriteLine(log);
        }

        private static void github_issue_131()
        {
            MockDevice device = new MockDevice();
            REngine.SetEnvironmentVariables();
            var engine = REngine.GetInstance(dll: null, initialize: true, parameter: null, device: device);
            engine.Evaluate("rm(list=ls())");
            device.Initialize();
            Console.WriteLine("Before calling engine.evaluate with console write actions");
            engine.Evaluate("print(NULL)");
            engine.Evaluate("cat(123123123)");
            Console.WriteLine("After calling engine.evaluate with console write actions");
            var s = device.GetString();
            Console.WriteLine("My device has the string '{0}'", s);
        }

        private static void github_issue_90()
        {

            string rHome = @"c:\Program Files\R\R-3.4.4\";
            string rPath = @"C:\Program Files\R\R-3.4.4\bin\x64";

            REngine.SetEnvironmentVariables(rPath: rPath, rHome: rHome);

            //p.RHome = @"c:\Program Files\Microsoft\R Open\R-3.4.3\";
            REngine engine = REngine.GetInstance();

            engine.Evaluate("library(dplyr)");
            engine.Evaluate("library(keras)");
            engine.Evaluate("model <- keras_model_sequential() %>% layer_dense(units = 1000, input_shape = c(1000)) %>% compile(loss = 'mse',optimizer = 'adam')");

            int counter = 0;

            while (true)
            {

                Console.WriteLine(counter++);

                IntegerVector vec = engine.CreateIntegerVector(1000);
                vec.SetVector(new int[1000]);
                engine.SetSymbol("vec1000", vec);

                var execution = "predict(model, t(vec1000))";
                using (var output = engine.Evaluate(execution))
                {
                    using (var a = output.AsList())
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            using (var b = a[i].AsNumeric())
                            {

                            }
                        }
                    }
                };
            }
        }
    }
}