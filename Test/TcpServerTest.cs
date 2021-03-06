﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Net;
using NUnit.Framework;

namespace Test.TCP
{
    [TestFixture]
    public class TcpServerTest
    {
        [Test]
        public void TestAccept()
        {
            var port = Tools.GetPort();
            var server = new TcpServer
            {
                AcceptThreads = 10,
                AcceptBacklog = 1000,
            };
            server.Listen(port);
            Assert.AreEqual($"tcp://[::]:{port}", server.ToString());
            Console.WriteLine($"Test : info TP{port}: Opened Server at port {port}.");

            using (var client = new TcpAsyncClient())
            {
                client.Connect("::1", port);
                Assert.AreEqual($"tcp://[::1]:{port}", client.ToString());
                client.Send(new byte[1000]);
                client.Close();
            }
            Console.WriteLine($"Test : info TP{port}: Test connect to ::1 successful.");

            using (var client = new TcpAsyncClient())
            {
                client.Connect("127.0.0.1", port);
                Assert.AreEqual($"tcp://127.0.0.1:{port}", client.ToString());
                client.Send(new byte[1000]);
                client.Close();
            }
            Console.WriteLine($"Test : info TP{port}: Test connect to 127.0.0.1 successful.");

            var count = 10000;
            var watch = Stopwatch.StartNew();
            var success = 0;

            var ip = IPAddress.Parse("127.0.0.1");
            Parallel.For(0, count, (n) =>
            {
                using (var client = new TcpAsyncClient())
                {
                    client.Connect(ip, port);
                    Interlocked.Increment(ref success);
                }
            });
            watch.Stop();

            Console.WriteLine($"Test : info TP{port}: {success} connections in {watch.Elapsed}");
            var cps = Math.Round(success / watch.Elapsed.TotalSeconds, 2);
            Console.WriteLine($"Test : info TP{port}: {cps} connections/s");
        }

        [Test]
        public void TestAccept_SingleThread()
        {
            var port = Tools.GetPort();
            var server = new TcpServer
            {
                AcceptThreads = 1,
                AcceptBacklog = 100
            };
            server.Listen(IPAddress.Loopback, port);
            Assert.AreEqual($"tcp://127.0.0.1:{port}", server.ToString());
            Console.WriteLine($"Test : info TP{port}: Opened Server at port {port}.");

            using (var client = new TcpAsyncClient())
            {
                client.Connect("localhost", port);
                client.Send(new byte[1000]);
                client.Close();
            }
            Console.WriteLine($"Test : info TP{port}: Test connect to ::1 successful.");

            using (var client = new TcpAsyncClient())
            {
                client.Connect("127.0.0.1", port);
                client.Send(new byte[1000]);
                client.Close();
            }
            Console.WriteLine($"Test : info TP{port}: Test connect to 127.0.0.1 successful.");

            var busyCount = 0;
            server.AcceptTasksBusy += (s,e) => Interlocked.Increment(ref busyCount);
            var count = 10000;
            var watch = Stopwatch.StartNew();
            var success = 0;
            var errors = 0;
            var ip = IPAddress.Parse("127.0.0.1");
            Parallel.For(0, count, (n) =>
            {
                using (var client = new TcpAsyncClient())
                {
                    try
                    {
                        client.Connect(ip, port);
                        Interlocked.Increment(ref success);
                    }
                    catch 
                    {
                        Interlocked.Increment(ref errors);
                    }
                }
            });
            watch.Stop();

            if (errors > 0 && busyCount == 0) throw new Exception($"BusyCount {busyCount}, Errors {errors}");
            Assert.IsTrue(success > errors, $"Errors {errors} > Success {success}");
            Assert.AreEqual(count, success + errors);
            Console.WriteLine($"Test : info TP{port}: {success} connections in {watch.Elapsed}");
            Console.WriteLine($"Test : info TP{port}: {errors} failed connections in {watch.Elapsed}");
            var cps = Math.Round(success / watch.Elapsed.TotalSeconds, 2);
            Console.WriteLine($"Test : info TP{port}: {cps} connections/s");
        }

        [Test]
        public void TestErrors()
        {
            var port = Tools.GetPort();
            var server = new TcpServer
            {
                AcceptThreads = 1,
                AcceptBacklog = 1
            };
            Assert.AreEqual(false, server.IsListening);
            server.Listen(port);
            Assert.AreEqual(true, server.IsListening);
            Console.WriteLine($"Test : info TP{port}: Opened Server at port {port}.");

            try { server.AcceptBacklog = 10; }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(InvalidOperationException), ex.GetType());
                Assert.AreEqual("Socket is already listening!", ex.Message);
            }
            try { server.Listen(port); }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(InvalidOperationException), ex.GetType());
                Assert.AreEqual("Socket is already listening!", ex.Message);
            }
            try { server.AcceptThreads = 10; }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(InvalidOperationException), ex.GetType());
                Assert.AreEqual("Socket is already listening!", ex.Message);
            }
            try { server.BufferSize = 1000; }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(InvalidOperationException), ex.GetType());
                Assert.AreEqual("Socket is already listening!", ex.Message);
            }
            try { server.BufferSize = 0; }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentOutOfRangeException), ex.GetType());
                Assert.AreEqual("value", ((ArgumentOutOfRangeException)ex).ParamName);
            }

            server.ReceiveTimeout = Settings.Timeout;
            server.SendTimeout = Settings.Timeout;

            var exceptions = new List<Exception>();
            void AcceptError(object sender, EventArgs e) => throw new Exception("AcceptError");
            void QueueError(object sender, TcpServerClientExceptionEventArgs<TcpAsyncClient> e)
            {
                lock(exceptions) exceptions.Add(e.Exception);
                Console.WriteLine($"Test : info TP{port}: Client {e.Client} Error Test {e.Exception.Message}");
            }
            server.ClientAccepted += AcceptError;
            server.ClientException += QueueError;

            using (var client = new TcpAsyncClient())
            {
                client.Connect("localhost", port);
                while (client.IsConnected)
                {
                    try { client.Send(new byte[1000]); }
                    catch { }
                }
            }
            Console.WriteLine($"Test : info TP{port}: Test connect to ::1 successful.");

            {
                Assert.AreEqual(1, exceptions.Count);
                Assert.AreEqual("AcceptError", exceptions[0].Message);
            }
            exceptions.Clear();

            server.ClientAccepted -= AcceptError;
            server.ClientException -= QueueError;
            void ClientAcceptedBufferError(object sender, TcpServerClientEventArgs<TcpAsyncClient> e)
            {
                Assert.AreEqual(server.ReceiveTimeout, e.Client.ReceiveTimeout);
                Assert.AreEqual(server.SendTimeout, e.Client.SendTimeout);
                e.Client.Buffered += (s1, e1) => throw new Exception("ClientAcceptedBufferError");
                e.Client.Error += (s2, e2) => { lock (exceptions) exceptions.Add(e2.Exception); };
            }
            server.ClientAccepted += ClientAcceptedBufferError;

            using (var client = new TcpAsyncClient())
            {
                client.Connect("localhost", port);
                while (client.IsConnected)
                {
                    try { client.Send(new byte[1000]); }
                    catch { }
                }
            }
            Console.WriteLine($"Test : info TP{port}: Test connect to ::1 successful.");

            {
                Assert.AreEqual(1, exceptions.Count);
                Assert.AreEqual("ClientAcceptedBufferError", exceptions[0].Message);
            }

            server.Close();
            Assert.AreEqual(false, server.IsListening);

            try { server.Listen(port); }
            catch (Exception ex) { Assert.AreEqual(typeof(ObjectDisposedException), ex.GetType()); }
            Assert.AreEqual(false, server.IsListening);
        }

        [Test]
        public void TestSend()
        {
            var port = Tools.GetPort();
            var server = new TcpServer();
            server.Listen(port);
            server.ClientAccepted += (s1, e1) => e1.Client.Received += (s2, e2) => e2.Handled = true;
            Console.WriteLine($"Test : info TP{port}: Opened Server at port {port}.");

            long bytes = 0;
            var watch = Stopwatch.StartNew();
            var ip = IPAddress.Parse("127.0.0.1");

            Parallel.For(0, 16, (n) =>
            {
                using (var client = new TcpAsyncClient())
                {
                    client.Connect(ip, port);
                    for (var d = 0; d < 256; d++)
                    {
                        client.Send(new byte[1024 * 1024]);
                        Interlocked.Add(ref bytes, 1024 * 1024);
                    }
                    client.Close();
                }
                Console.WriteLine($"Test : info TP{port}: Client {n + 1} completed.");
            });
            watch.Stop();

            Console.WriteLine($"Test : info TP{port}: {bytes:N} bytes in {watch.Elapsed}");
            var bps = Math.Round(bytes / watch.Elapsed.TotalSeconds, 2);
            Console.WriteLine($"Test : info TP{port}: {bps:N} bytes/s");
        }

        [Test]
        public void TestDisconnectAsync()
        {
            var serverClientConnectedEventCount = 0;
            var serverClientDisconnectedEventCount = 0;
            var clientConnectedEventCount = 0;
            var clientDisconnectedEventCount = 0;
            var port = Tools.GetPort();
            var server = new TcpServer
            {
                AcceptBacklog = 1000
            };
            server.Listen(port);
            server.ClientAccepted += (s1, e1) =>
            {
                e1.Client.Connected += (s2, e2) => Interlocked.Increment(ref serverClientConnectedEventCount);
                e1.Client.Disconnected += (s2, e2) => Interlocked.Increment(ref serverClientDisconnectedEventCount);
            };
            Console.WriteLine($"Test : info TP{port}: Opened Server at port {port}.");

            var clients = new List<TcpAsyncClient>();
            var ip = IPAddress.Parse("127.0.0.1");
            Console.WriteLine($"Test : info TP{port}: Connecting 1000 clients to {ip}:{port}.");
            Parallel.For(0, 1000, (n) =>
            {
                var client = new TcpAsyncClient();
                client.Connected += (s1, e1) => Interlocked.Increment(ref clientConnectedEventCount);
                client.Disconnected += (s1, e1) => Interlocked.Increment(ref clientDisconnectedEventCount);
                client.Connect(ip, port);
                lock(clients) clients.Add(client);
            });
            //all clients connected
            Assert.AreEqual(1000, clientConnectedEventCount, "Some clients could not connect to target.");
            Console.WriteLine($"Test : info TP{port}: Connected 1000 clients to {ip}:{port}.");
            //no client disconnected
            Assert.AreEqual(0, clientDisconnectedEventCount, "Some clients where disconnected!");

            Console.WriteLine($"Test : info TP{port}: ConnectedEventCount ok.");

            //give the server some more time
            Thread.Sleep(2000);

            //all clients connected
            Assert.AreEqual(clientConnectedEventCount, serverClientConnectedEventCount);
            Assert.AreEqual(1000, server.Clients.Length);
            //no client disconnected
            Assert.AreEqual(0, clientDisconnectedEventCount);
            Assert.AreEqual(clientDisconnectedEventCount, serverClientDisconnectedEventCount);

            Console.WriteLine($"Test : info TP{port}: DisconnectedEventCount ({clientDisconnectedEventCount}) ok.");

            //disconnect some
            int i = 0, disconnected = 0;
            foreach (var client in clients)
            {
                if (i++ % 3 == 0)
                {
                    disconnected++;
                    client.Close();
                }
            }

            Assert.AreEqual(disconnected, clientDisconnectedEventCount);

            //give the server some more time
            Thread.Sleep(2000);

            Assert.AreEqual(clientDisconnectedEventCount, serverClientDisconnectedEventCount);
            Assert.AreEqual(clientConnectedEventCount - disconnected, server.Clients.Length);

            Console.WriteLine($"Test : info TP{port}: DisconnectedEventCount ({clientDisconnectedEventCount}) ok.");

            foreach (var client in clients)
            {
                client.Close();
            }
            Assert.AreEqual(clientConnectedEventCount, serverClientConnectedEventCount);

            //give the server some more time
            Thread.Sleep(2000);

            Assert.AreEqual(clientDisconnectedEventCount, serverClientDisconnectedEventCount);

            Console.WriteLine($"Test : info TP{port}: DisconnectedEventCount ({clientDisconnectedEventCount}) ok.");
        }

        [Test]
        public void TestPortAlreadyInUse1()
        {
            var listen = Tools.OpenPort(out var port);
            try
            {
                var server = new TcpServer();
                server.Listen(port);
                Assert.Fail("AddressAlreadyInUse expected!");
            }
            catch (SocketException ex)
            {
                Assert.AreEqual(SocketError.AddressAlreadyInUse, ex.SocketErrorCode);
            }
            finally
            {
                listen.Stop();
            }
        }

        [Test]
        public void TestPortAlreadyInUse2()
        {
            var listen = Tools.OpenPort(out var port);
            try
            {
                var server = new TcpServer();
                server.Listen(port);
                Assert.Fail("AddressAlreadyInUse expected!");
            }
            catch (SocketException ex)
            {
                Assert.AreEqual(SocketError.AddressAlreadyInUse, ex.SocketErrorCode);
            }
            finally
            {
                listen.Stop();
            }
        }

        [Test]
        public void TestSendAllBeforeClose_Client2TcpListener()
        {
            var port = Tools.GetPort();
            var listen = new TcpListener(IPAddress.Loopback, port);
            listen.Start();
            try
            {
                var serverTask = Task.Factory.StartNew(delegate
                {
                    var client = listen.AcceptTcpClient();
                    using (var reader = new StreamReader(client.GetStream()))
                    {
                        for (var i = 0; i < 10000; i++)
                        {
                            var s = reader.ReadLine();
                            Assert.AreEqual(i.ToString(), s);
                        }
                    }
                });
                var clientTask = Task.Factory.StartNew(delegate
                {
                    using (var client = new TcpAsyncClient())
                    {
                        client.Connect(IPAddress.Loopback, port);
                        using (var writer = new StreamWriter(client.GetStream()))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                writer.WriteLine(i);
                            }
                        }
                    }
                });
                Task.WaitAll(serverTask, clientTask);
            }
            finally
            {
                listen.Stop();
            }
        }

        [Test]
        public void TestSendAllBeforeClose_Client2Server()
        {
            var port = Tools.GetPort();
            var server = new TcpServer();
            server.Listen(port);
            using (var completed = new ManualResetEvent(false))
            {
                try
                {
                    server.ClientAccepted += (sender, eventArgs) => Task.Factory.StartNew((c) =>
                    {
                        var client = (TcpAsyncClient)c;
                        using (var reader = new StreamReader(client.GetStream()))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                var s = reader.ReadLine();
                                Assert.AreEqual(i.ToString(), s);
                            }
                        }
                        completed.Set();
                    }, eventArgs.Client);
                    using (var client = new TcpAsyncClient())
                    {
                        client.Connect(IPAddress.Loopback, port);
                        using (var writer = new StreamWriter(client.GetStream()))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                writer.WriteLine(i);
                            }
                        }
                    }
                    if (!completed.WaitOne(Settings.Timeout)) throw new TimeoutException();
                }
                finally
                {
                    server.Close();
                }
            }
        }

        [Test]
        public void TestSendAllBeforeClose_Client2Server_CloseBeforeRead()
        {
            var port = Tools.GetPort();
            var server = new TcpServer();
            server.Listen(port);
            using (var completed = new ManualResetEvent(false))
            {
                try
                {
                    Task t = null;
                    server.ClientAccepted += (sender, eventArgs) => t = Task.Factory.StartNew((c) =>
                    {
                        var client = (TcpAsyncClient)c;
                        var stream = client.GetStream();
                        //wait for client disco
                        while (client.IsConnected) Thread.Sleep(1);
                        //read after client disco
                        using (var reader = new StreamReader(stream))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                var s = reader.ReadLine();
                                Assert.AreEqual(i.ToString(), s);
                            }
                        }
                        completed.Set();
                    }, eventArgs.Client);
                    using (var client = new TcpAsyncClient())
                    {
                        client.Connect(IPAddress.Loopback, port);
                        using (var writer = new StreamWriter(client.GetStream()))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                writer.WriteLine(i);
                            }
                        }
                        client.Close();
                    }
                    if (!completed.WaitOne(Settings.Timeout)) throw new TimeoutException();
                    Assert.AreEqual(null, t?.Exception, $"{t.Exception}");
                }
                finally
                {
                    server.Close();
                }
            }
        }

        [Test]
        public void TestSendAllBeforeClose_Server2Client()
        {
            var port = Tools.GetPort();
            var server = new TcpServer();
            server.Listen(port);
            using (var completed = new ManualResetEvent(false))
            {
                try
                {
                    server.ClientAccepted += (sender, eventArgs) => Task.Factory.StartNew((c) =>
                    {
                        var client = eventArgs.Client;
                        using (var writer = new StreamWriter(client.GetStream()))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                writer.WriteLine(i);
                            }
                        }
                        client.Close();
                        completed.Set();
                    }, eventArgs.Client);
                    using (var client = new TcpAsyncClient())
                    {
                        client.Connect(IPAddress.Loopback, port);
                        using (var reader = new StreamReader(client.GetStream()))
                        {
                            for (var i = 0; i < 10000; i++)
                            {
                                var s = reader.ReadLine();
                                Assert.AreEqual(i.ToString(), s);
                            }
                        }
                    }
                    if (!completed.WaitOne(Settings.Timeout)) throw new TimeoutException();
                }
                finally
                {
                    server.Close();
                }
            }
        }

        [Test]
        public void TestBuffering()
        {
            for (var i = 0; i < 100; i++)
            {
                var port = Tools.GetPort();
                using (var server = new TcpServer())
                using (var sendEvent = new ManualResetEvent(false))
                using (var testClient = new TcpAsyncClient())
                using (var bufferedEvent = new ManualResetEvent(false))
                {
                    // on client accept send buffer
                    server.ClientAccepted += (sender, eventArgs) => Task.Factory.StartNew((c) =>
                    {
                        var serverClient = eventArgs.Client;
                        serverClient.Send(new byte[] { 2, 1, 0, 1 });
                        serverClient.Close();
                        sendEvent.Set();
                    }, eventArgs.Client);

                    // open server port
                    server.Listen(port);

                    // on client buffered, test receivebuffer
                    testClient.Buffered += (s, e) =>
                    {
                        if (testClient.ReceiveBuffer.Available < 4) return;
                        Assert.AreEqual(4, testClient.ReceiveBuffer.Available);
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(3));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(2));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(1));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(0));
                        Assert.AreEqual(2, testClient.ReceiveBuffer.ReadByte());

                        Assert.AreEqual(3, testClient.ReceiveBuffer.Available);
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(3));
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(2));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(1));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(0));
                        Assert.AreEqual(1, testClient.ReceiveBuffer.ReadByte());

                        Assert.AreEqual(2, testClient.ReceiveBuffer.Available);
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(3));
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(2));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(1));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(0));
                        Assert.AreEqual(0, testClient.ReceiveBuffer.ReadByte());

                        Assert.AreEqual(1, testClient.ReceiveBuffer.Available);
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(3));
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(2));
                        Assert.AreEqual(true, testClient.ReceiveBuffer.Contains(1));
                        Assert.AreEqual(false, testClient.ReceiveBuffer.Contains(0));
                        Assert.AreEqual(1, testClient.ReceiveBuffer.ReadByte());
                        bufferedEvent.Set();
                    };

                    // connect to server
                    testClient.Connect(IPAddress.Loopback, port);

                    //wait for completion
                    if (!sendEvent.WaitOne(Settings.Timeout)) throw new TimeoutException("Send event not completed!");
                    if (!bufferedEvent.WaitOne(Settings.Timeout)) throw new TimeoutException("Buffered event not completed!");
                }
            }
        }
    }
}
