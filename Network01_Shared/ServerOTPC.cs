using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;



namespace Network01_Shared
{
    /// <summary>
    /// This class takes care of accepting incoming connections and event handling
    /// </summary>
    /// <typeparam name="T"></typeparam>    


    public class ServerOTPC<T> where T : ServerOTPC<T>.ConnectionHandler, new() //utilizzare i generics risparmia all'utente della mia classe pericolosi downcast
    {
        private static int          s_iCounter;

        private Socket              m_hListener;
        private Dictionary<int, T>  m_hConnections; //sarebbe stato meglio un ConcurrentDictionary
        private int                 m_iBacklog;
        private int                 m_iBufferSize;
        private Thread              m_hThreadListener;

        public event Action<T> ClientConnected;
        public event Action<T> ClientDisconnected;
        public event Action    ServerStarted;
        public event Action    ServerStopped;

        public int Port { get; private set; }

        public ServerOTPC(int iBlackLog, int iBufferSize)
        {
            m_iBufferSize = iBufferSize;
            m_iBacklog      = iBlackLog;
            m_hConnections  = new Dictionary<int, T>();
        }

        public void Start(int iPort)
        {
            if (m_hThreadListener != null)
                throw new ApplicationException("Server Already Started");

            Port                = iPort;
            m_hThreadListener   = new Thread(ThreadListener);

            m_hListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_hListener.Bind(new IPEndPoint(IPAddress.Any, Port));
            m_hListener.Listen(m_iBacklog);

            m_hThreadListener.Start();
        }

        public void Stop()
        {
            m_hListener.Close();
            m_hThreadListener.Join();
            m_hThreadListener = null;
        }

        //in questo modo diamo la possibilità all'utente di rilevare gli errori del server
        //se dichiaro hHandler come T, l'utente troverà il suo oggetto, diversamente avrebbe trovato il nostro, quindi meglio usare T
        protected virtual void OnConnectionHandlerError(T hHandler)
        {
            lock (m_hConnections)
            {
                m_hConnections.Remove(hHandler.Id);
            }

            ClientDisconnected?.Invoke(hHandler);
        }


        //Dedicate one thread to incoming connections
        private void ThreadListener()
        {
            try
            {
                ServerStarted?.Invoke();

                while (true)
                {
                    Socket hNewConnection = m_hListener.Accept(); //il thread sta in attesa per una connessione in ingresso

                    //T deve ricevere il socket, 2 modi: 1) T implementa un interfaccia interna, 2) creiamo un allocatore per T
                    T hConnHandler = new T();
                    hConnHandler.Socket = hNewConnection;
                    hConnHandler.CreateBuffer(m_iBufferSize);

                    lock (m_hConnections)
                    {
                        hConnHandler.Id    = s_iCounter;
                        hConnHandler.Owner = this;

                        m_hConnections.Add(hConnHandler.Id, hConnHandler);
                        s_iCounter++;                        
                    }

                    hConnHandler.BeginReceive(); //connection now active, let the user code handle requests and resposes

                    try
                    {
                        ClientConnected?.Invoke(hConnHandler);
                    }
                    catch (Exception)
                    {
                        //Se qualcuno nell'implementazione dell'evento fa danni... a noi non ci interessa
                    }
                }
            }
            catch (Exception)
            {
                //se siamo qui è xche abbiamo chiamato stop quindi possiamo interrompere il ciclo
                ServerStopped?.Invoke();                
            }
        }

        public void Dispatch(byte[] hData, T hToExclude)
        {
            lock (m_hConnections)
            {
                foreach (KeyValuePair<int, T> hHandler in m_hConnections)
                {
                    if (hHandler.Value == hToExclude)
                        continue;

                    try
                    {
                        hHandler.Value.Writer.Write(hData);
                    }
                    catch (Exception)
                    {
                        //se qualche cosa va storto, non ci preoccupiamo che tanto il thread dell'handler gestisce la disconnessione
                    }
                }
            }
        }


        public abstract class ConnectionHandler : IDisposable
        {
            public  int             Id     { get; internal set; }  //il set lo possiamo fare solo noi
            public BinaryWriter     Writer { get; internal set; }
            public ServerOTPC<T>    Owner  { get; internal set; }           //il server invece lo possiamo leggere e scrivere solamente noi
            internal Socket         Socket { get; set; } 
                        

            private Thread          m_hRecvThread;

            private NetworkStream   m_hNetworkStream;
            private BinaryReader    m_hReader;
            

            protected EndPoint      m_hEndPoint;

            public ConnectionHandler()
            {
                m_hRecvThread   = new Thread(ConnectionThread);
            }

            internal void CreateBuffer(int iBufferSize)
            {
                this.Socket.ReceiveBufferSize = 1024;
                m_hNetworkStream = new NetworkStream(Socket);
                Writer           = new BinaryWriter(m_hNetworkStream);
                m_hReader        = new BinaryReader(m_hNetworkStream);
            }

            protected abstract void OnDataReceived(BinaryReader hReader);


            //Ora possiamo gestire la connessione con il client
            //per prima cosa creiamo un meccanismo per rilevare le disconnessioni
            private void ConnectionThread()
            {
                try
                {
                    while (true)
                    {                       
                        OnDataReceived(m_hReader);
                    }
                }
                catch (Exception)
                {
                    //se cè un errore sulla ricezione, possiamo considerare la connessione persa e la rimuoviamo
                    //questo downcast è accettabile per via dei constraits su T
                    this.Owner.OnConnectionHandlerError(this as T);
                    Dispose();
                }
            }

            internal void BeginReceive()
            {
                m_hEndPoint = Socket.RemoteEndPoint;
                m_hRecvThread.Start();
            }



            #region IDisposable Support
            private bool disposedValue = false;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        Socket.Shutdown(SocketShutdown.Send);
                        Socket.Close();
                    }
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }
    }
}
