using System;
using System.Collections.Generic;
using System.Text;
using Phoenix;
using PhoenixInpl;
using NUnit.Framework;
using System.Net;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class WebSocketTest
    {
		private const int networkDelay = 2000 /* ms */;
		private const string host = "phoenix-integration-tester.herokuapp.com";

		public static async void DelaySample()
        {
			Console.WriteLine("start");
			/// 
			/// setup
			/// 
			var address = string.Format("http://{0}/api/health-check", host);

			// heroku health check
			using (WebClient client = new WebClient())
			{
				client.Headers.Add("Content-Type", "application/json");
				client.DownloadString(address);
			}

			var onOpenCount = 0;
			Socket.OnOpenDelegate onOpenCallback = () => onOpenCount++;

			List<String> onMessageData = new List<string>();
			Socket.OnMessageDelegate onMessageCallback = m => onMessageData.Add(m);

			// connecting is synchronous as implemented above
			var socketFactory = new WebsocketSharpFactory();
			var socket = new Socket(socketFactory, new Socket.Options
			{
				channelRejoinInterval = TimeSpan.FromMilliseconds(200),
				logger = new BasicLogger()
			});

			socket.OnOpen += onOpenCallback;
			socket.OnMessage += onMessageCallback;

			socket.Connect(string.Format("ws://{0}/socket", host), null);

			Reply? joinOkReply = null;
			Reply? joinErrorReply = null;

			Message afterJoinMessage = null;
			Message closeMessage = null;
			Message errorMessage = null;

			var param = new Dictionary<string, object> {
				{ "auth", "doesn't matter" },
			};

			var roomChannel = socket.MakeChannel("tester:phoenix-sharp");
			roomChannel.On(Message.InBoundEvent.phx_close, m => closeMessage = m);
			roomChannel.On(Message.InBoundEvent.phx_error, m => errorMessage = m);
			roomChannel.On("after_join", m => Console.WriteLine("after join: " + m.payload["message"].Value<string>()));

			roomChannel.Join(param)
				.Receive(Reply.Status.Ok, r => joinOkReply = r)
				.Receive(Reply.Status.Error, r => joinErrorReply = r);

			/// 
			/// test echo reply
			/// 
			var payload = new Dictionary<string, object> {
					{ "echo", "test" }
			};

			Reply? testOkReply = null;

			roomChannel
				.Push("reply_test", payload)
				.Receive(Reply.Status.Ok, r => testOkReply = r);

			//Console.WriteLine("aaa: " + testOkReply.Value.response.ToObject<Dictionary<string, object>>());
			Console.WriteLine("end");
			//await Task.Delay(5000);

			Console.ReadLine();
		}
    }
}
