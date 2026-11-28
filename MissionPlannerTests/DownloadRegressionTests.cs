using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;

namespace MissionPlannerTests
{
    [TestClass]
    public class DownloadRegressionTests
    {
        private sealed class ResponseServer : IDisposable
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly Task server;
            public readonly string Url;
            public ResponseServer(string response, Action afterSend = null)
            {
                listener.Start();
                Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/test";
                server = Task.Run(() => {
                    using (var connection = listener.AcceptTcpClient())
                    using (var stream = connection.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
                    {
                        stream.ReadTimeout = 10000;
                        while (!string.IsNullOrEmpty(reader.ReadLine())) { }
                        byte[] bytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(bytes, 0, bytes.Length);
                        afterSend?.Invoke();
                    }
                });
            }
            public void Dispose() { listener.Stop(); Assert.IsTrue(server.Wait(10000), "HTTP fixture did not finish"); }
        }

        [TestMethod]
        public void ChunkedDownloadPreservesContent()
        {
            string path = Path.GetTempFileName();
            try
            {
                using (var server = new ResponseServer("HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\nConnection: close\r\n\r\n5\r\nhello\r\n0\r\n\r\n"))
                    Assert.IsTrue(Download.getFilefromNet(server.Url, path));
                Assert.AreEqual("hello", File.ReadAllText(path));
                Assert.IsFalse(File.Exists(path + ".new"));
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void TruncatedDownloadPreservesExistingFile()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, "keep existing file");
            try
            {
                using (var server = new ResponseServer("HTTP/1.1 200 OK\r\nContent-Length: 100\r\nConnection: close\r\n\r\nshort"))
                    Assert.IsFalse(Download.getFilefromNet(server.Url, path));
                Assert.AreEqual("keep existing file", File.ReadAllText(path));
                Assert.IsFalse(File.Exists(path + ".new"));
            }
            finally { File.Delete(path); File.Delete(path + ".new"); }
        }

        [TestMethod]
        public async Task CancelledDownloadPreservesCacheAndCanRetry()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, "old");
            try
            {
                using (var release = new ManualResetEventSlim())
                using (var cancel = new CancellationTokenSource())
                using (var server = new ResponseServer(
                    "HTTP/1.1 200 OK\r\nContent-Length: 100\r\nConnection: close\r\n\r\npartial",
                    () => release.Wait(10000)))
                {
                    var bodyStarted = new TaskCompletionSource<bool>();
                    var download = Download.getFilefromNetAsync(server.Url, path, cancel.Token,
                        (progress, message) => bodyStarted.TrySetResult(true));
                    try
                    {
                        Assert.AreSame(bodyStarted.Task, await Task.WhenAny(bodyStarted.Task, Task.Delay(5000)),
                            "No body progress before cancellation");
                        cancel.Cancel();
                        Assert.AreSame(download, await Task.WhenAny(download, Task.Delay(5000)),
                            "Cancellation did not interrupt the stalled body");
                        Assert.IsFalse(await download);
                    }
                    finally
                    {
                        release.Set();
                    }
                }

                Assert.AreEqual("old", File.ReadAllText(path));
                Assert.AreEqual(0, Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".*.new").Length);
                using (var cancel = new CancellationTokenSource())
                using (var retry = new ResponseServer("HTTP/1.1 200 OK\r\nContent-Length: 5\r\nConnection: close\r\n\r\nretry"))
                    Assert.IsTrue(await Download.getFilefromNetAsync(retry.Url, path, cancel.Token));
                Assert.AreEqual("retry", File.ReadAllText(path));
            }
            finally
            {
                File.Delete(path);
                foreach (string temporary in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".*.new"))
                    File.Delete(temporary);
            }
        }

        [TestMethod]
        public void FileSizeReadsHeadersWithoutDownloadingBody()
        {
            using (var server = new ResponseServer("HTTP/1.1 200 OK\r\nContent-Length: 5000000000\r\nConnection: close\r\n\r\n"))
                Assert.AreEqual(5000000000L, Download.GetFileSize(server.Url));
        }
    }
}
