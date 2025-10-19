using Xunit;

namespace RDotNet
{
    public class ListsTest : RDotNetTestFixture
    {
        [Fact]
        public void TestIsList()
        {
            SetUpTest();
            //https://rdotnet.codeplex.com/workitem/81
            var engine = this.Engine;
            var pairList = engine.Evaluate("pairlist(a=5)");
            var aList = engine.Evaluate("list(a=5)");
            bool b = aList.AsList().IsList();
            Assert.Equal(true, pairList.IsList());
            Assert.Equal(true, aList.IsList());
        }

        [Fact]
        public void TestListSubsetting()
        {
            SetUpTest();
            //https://rdotnet.codeplex.com/workitem/81
            var engine = this.Engine;
            var numlist = engine.Evaluate("c(1.5, 2.5)").AsList();
            var numListString = numlist.ToString();
            var element = numlist[1];
        }

        [Fact]
        public void TestListSetNames()
        {
            SetUpTest();
            //  http://stackoverflow.com/questions/33326594/how-can-i-create-named-list-members-object-in-r-net
            var engine = this.Engine;
            var list = new GenericVector(engine, 2);
            // odpar <- list(mean = c(-1.5, 0, 1.5), var = c(0.5, 0.6, 0.8)) 
            list[0] = engine.CreateNumericVector(new[] { -1.5, 0, 1.5 });
            list[1] = engine.CreateNumericVector(new[] { 0.5, 0.6, 0.8 });
            list.SetNames("mean", "var");
            engine.SetSymbol("TestListSetNames", list);
            var listNames = engine.Evaluate("names(TestListSetNames)").AsCharacter().ToArray();
            Assert.Equal("mean", listNames[0]);
            Assert.Equal("var", listNames[1]);
        }

        [Fact]
        public void TestCoercionAsList()
        {
            SetUpTest();
            /*
             * as.list.function
   > str(as.list(as.list))
   List of 3
    $ x  : symbol
    $ ...: symbol
    $    : language UseMethod("as.list")
   >
             */

            var engine = this.Engine;
            var functionAsList = engine.Evaluate("as.list").AsList();
            Assert.Equal(3, functionAsList.Length);
            Assert.True(functionAsList[0].IsSymbol());
            Assert.True(functionAsList[1].IsSymbol());
            Assert.True(functionAsList[2].IsLanguage());

            var dataFrame = engine.Evaluate("data.frame(a = rep(LETTERS[1:3], 2), b = rep(1:3, 2))");
            /*
             > str(as.list(x))
   List of 2
    $ a: chr [1:6] "A" "B" "C" "A" ...
    $ b: int [1:6] 1 2 3 1 2 3
   >
   */
            var dataFrameAsList = dataFrame.AsList();
            Assert.Equal(2, dataFrameAsList.Length);
            Assert.False(dataFrameAsList[0].IsFactor());
            Assert.Equal(6, dataFrameAsList[1].AsInteger().Length);

            dataFrame = engine.Evaluate("data.frame(a = rep(LETTERS[1:3], 2), b = rep(1:3, 2), stringsAsFactors = TRUE)");
            /*
             > str(as.list(x))
             List of 2
              $ a: Factor w/ 3 levels "A","B","C": 1 2 3 1 2 3
              $ b: int [1:6] 1 2 3 1 2 3
            */
            dataFrameAsList = dataFrame.AsList();
            Assert.Equal(2, dataFrameAsList.Length);
            Assert.True(dataFrameAsList[0].IsFactor());
            Assert.Equal(6, dataFrameAsList[1].AsInteger().Length);
        }
    }
}