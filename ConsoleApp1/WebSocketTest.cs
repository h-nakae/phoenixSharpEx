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

        // ★接続先URL 
        //private const string host = "phoenix-integration-tester.herokuapp.com";
        private const string host = "192.168.25.117:4000";

        public static async void joinAndPush()
        {
			Console.WriteLine("app start");

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

            //★接続先Topic
            //var roomChannel = socket.MakeChannel("tester:phoenix-sharp");
            var roomChannel = socket.MakeChannel("room:lobby");

            roomChannel.On(Message.InBoundEvent.phx_close, m => closeMessage = m);
			roomChannel.On(Message.InBoundEvent.phx_error, m => errorMessage = m);
			roomChannel.On("after_join", m => Console.WriteLine("after join: " + m.payload["message"].Value<string>()));

			roomChannel.Join(param)
				.Receive(Reply.Status.Ok, r => joinOkReply = r)
				.Receive(Reply.Status.Error, r => joinErrorReply = r);

			Reply? testOkReply = null;

            while(1 == 1)
            {
                string message = Console.ReadLine();

                if (message == "end")
                {
                    break;
                }
                else
                {
                    // ★messageに送信したい文字列を格納
                    var payload = new Dictionary<string, object> {
                        { "body", message }
                    };

                    roomChannel
                        .Push("new_msg", payload)
                        .Receive(Reply.Status.Ok, r => testOkReply = r);
                }
            }

            //Console.WriteLine("aaa: " + testOkReply.Value.response.ToObject<Dictionary<string, object>>());
            Console.WriteLine("app end");
		}
    }
}
