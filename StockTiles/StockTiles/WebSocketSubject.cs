using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace StockTiles
{
    public class WebSocketSubject : IDisposable, IObservable<string>
    {
        private MessageWebSocket messageWebSocket;
        private Subject<string> socketSubject = new Subject<string>();

        private WebSocketSubject() 
        {
            messageWebSocket = new MessageWebSocket();
            messageWebSocket.Control.MessageType = SocketMessageType.Utf8;

            messageWebSocket.MessageReceived += MessageReceived;           
            messageWebSocket.Closed += Closed;
        }

        public static async Task<WebSocketSubject> Create(string url)
        {
            WebSocketSubject subject = new WebSocketSubject();
            await subject.messageWebSocket.ConnectAsync(new Uri(url));
            return subject;
        }

        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {         
            using (DataReader reader = args.GetDataReader())
            {
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                socketSubject.OnNext(reader.ReadString(reader.UnconsumedBufferLength));
            }         
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            if (messageWebSocket != null)
            {
                messageWebSocket.Dispose();
                socketSubject.OnCompleted();

                messageWebSocket = null;
            }
        }

        public void Dispose()
        {
            messageWebSocket.Dispose();
            socketSubject.Dispose();
            messageWebSocket = null;
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return socketSubject.Subscribe(observer);
        }
    }
}
