using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace ThreadSafeMethodTest {
  internal class Program {
    private const bool _queueOnThreadPool = true;
    private const int _noThreads = 10;

    private static void Main(string[] args) {

      var writer = new Writer();
      var mockWriter = new Mock<IWriter>();
      mockWriter.Setup(w => w.Write(It.IsAny<string>())).Callback((string i) => {
        writer.Write(i);
      });

      var testclass = new TestClass(mockWriter.Object);
      var threads = new List<ThreadStart>();

      for (int i = 0; i < _noThreads; i++) {
        var temp = i;
        threads.Add(() => testclass.TestMethod(temp.ToString()));
      }

      if (_queueOnThreadPool) {
        Parallel.For(0, _noThreads, i => ThreadPool.QueueUserWorkItem(state => threads[i].Invoke()));
      }
      else {
        Parallel.For(0, _noThreads, i => threads[i].Invoke()); 
      }

      for (int i = 0; i < _noThreads; i++) {
        int i1 = i;
        mockWriter.Verify(w => w.Write(It.Is<string>(s => s == i1.ToString())));
      }

      if (_queueOnThreadPool) {
        var strings = writer.GetStrings();
        for (int i = 0; i < _noThreads; i++) {
          var contains = strings.Contains(i.ToString());
          if (!contains) {
            throw new ApplicationException();
          }
        }
      }

      Console.WriteLine("All threads called method as expected.");
      Console.ReadKey();
    }
  }

  public class TestClass {

    private readonly IWriter writer;

    public TestClass(IWriter writer) {
      this.writer = writer;
    }

    public void TestMethod(string data) {
      writer.Write(data);
    }
  }

  public interface IWriter {
    void Write(string data);
  }

  public class Writer : IWriter {

    private List<string> _strings;

    public Writer() {
      _strings = new List<string>();
    }

    public void Write(string data) {
      Console.WriteLine(data);
      Debug.WriteLine(data);
      lock (_strings) {
        _strings.Add(data);
      }
    }

    public List<string> GetStrings() {
      return _strings;
    } 
  }
}
