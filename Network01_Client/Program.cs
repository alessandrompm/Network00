using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Network01_Client
{
    //Creazione di un semplice port scanner

    static class PortScanner
    {

        //Utilizziamo tante connessioni sincrone a seconda di quanti thread ci da l'OS
        public static void ParallelScan(string sIpAddress, int iStartPort, int iEndPort)
        {
            Parallel.For(0, iEndPort + 1, (i) =>
            {
                using (Socket hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    try
                    {
                        hSocket.Connect(new IPEndPoint(IPAddress.Parse(sIpAddress), i));

                        Console.WriteLine(sIpAddress + ":" + i + " is Open");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(sIpAddress + ":" + i);
                    }
                }
            });          
        }


        //Eseguiamo tanti tentativi di connessioni quanto l'OS riesce a gestire
        public static void AsyncScan(string sIpAddress, int iStartPort, int iEndPort)
        {
            int iAmmount = iEndPort - iStartPort;

            using (CountdownEvent hCountDown = new CountdownEvent(iAmmount))
            {

                for (int i = iStartPort; i < iEndPort; i++)
                {
                    Socket hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    ConnData vData = new ConnData();
                    vData.S = hSocket;
                    vData.Port = i;

                    
                    //Il threading in questo modello viene gestito dietro le quinte dall'OS che lo fa sicuramente meglio di noi
                    hSocket.BeginConnect(sIpAddress, i, (hRes) =>
                    {
                        ConnData hConn = hRes.AsyncState as ConnData;

                        try
                        {
                            hConn.S.EndConnect(hRes); //dammi il risultato dell'operazione
                            Console.WriteLine(sIpAddress + ":" + hConn.Port + " is Open");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(sIpAddress + ":" + hConn.Port);
                        }
                        finally
                        {
                            hConn.S.Close();
                            hCountDown.Signal();
                        }

                    }, vData);

                }

                hCountDown.Wait();
            }



        }

        
        public static void NonBlockingScan(string sIpAddress, int iStartPort, int iEndPort)
        {
            LinkedList<ConnData> hConnections = new LinkedList<ConnData>();

            for (int i = iStartPort; i < iEndPort; i++)
            {
                ConnData hData = new ConnData();
                hData.S = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                hData.S.Blocking = false;
                hData.Port = i;
                hData.Node = hConnections.AddFirst(hData);
            }


            List<ConnData> hToRemove = new List<ConnData>();
            //dobbiamo interrogare i socket fino a quando non hanno finito tutti di lavorare
            while (hConnections.Count > 0)
            {
                
                foreach (var node in hConnections)
                {
                    try
                    {
                        node.S.Connect(sIpAddress, node.Port);

                        Console.WriteLine(sIpAddress + ":" + node.Port + " is Open");
                        hToRemove.Add(node);
                    }
                    catch(SocketException hEx)
                    {
                        if (hEx.SocketErrorCode == SocketError.WouldBlock)
                        {
                            //In questo caso devo aspettare e riprovare dopo.. quindi non faccio niente
                        }
                        else
                        {
                            Console.WriteLine(sIpAddress + ":" + node.Port);
                            hToRemove.Add(node);
                        }
                    }
                }

                hToRemove.ForEach(x => hConnections.Remove(x));

            }                        
        }
    }

    class ConnData
    {
        public Socket S;
        public int Port;
        public LinkedListNode<ConnData> Node;
    }




    class ChatClient
    {
        //per stabilire la connessione
        private Socket m_hSocket;

        //x la ricezione dedichiamo un thread a se stante
        private Thread m_hRecvThread;

        private NetworkStream m_hNS;
        private BinaryWriter m_hWriter;
        private BinaryReader m_hReader;

        private AutoResetEvent m_hEvent;
        private static bool m_bLastResult;

        public ChatClient()
        {
            m_hEvent = new AutoResetEvent(false);
        }


        public void Connect(string sAddr, int iPort)
        {
            m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            m_hSocket.Connect(sAddr, iPort);

            m_hNS = new NetworkStream(m_hSocket);
            
            m_hWriter = new BinaryWriter(m_hNS);
            m_hReader = new BinaryReader(m_hNS);

            m_hRecvThread = new Thread(RecvThread);
            m_hRecvThread.Start();
        }

        public bool Login(string sUsername, string sPassword)
        {
            //Scriviamo Header e dati utilizzando lo stream
            //m_hWriter.Write(1);
            //m_hWriter.Write((sUsername.Length + sPassword.Length) * sizeof(char));
            //m_hWriter.Write(sUsername);
            //m_hWriter.Write(sPassword);
            m_hWriter.Write("Macomecazzofunzioni");
            m_hWriter.Flush();              //tutti i stream espongono questo metodo, che blocca fino a quando la scrittura sul dispositivo non è stata ultimata

            m_hEvent.WaitOne();

            return m_bLastResult;
        }

        public bool Join(int iChannelID)
        {
            return false;
        }

        public void Message(string sMessage)
        {
        }

        public void Leave()
        {
        }

        private void RecvThread()
        {
            while (true)
            {
                //In fase di ricezione leggiamo prima il byte iniziale

                byte id = m_hReader.ReadByte();

                short size = m_hReader.ReadInt16();


                switch (id)
                {
                    //Ack, NACK
                    case 2:
                        m_bLastResult = m_hReader.ReadBoolean();                        
                        break;

                }

                m_hEvent.Set(); //è sempre TReceiver a comunicare in direzione di Main
            }
        }
        
    }

    class Program
    {
        static void Main(string[] args)
        {
            ChatClient hClient = new ChatClient();

            


            while (true)
            {

                Console.WriteLine("Waiting For Connect");
                Console.ReadLine();
                hClient.Connect("127.0.0.1", 2800);
                Console.WriteLine("Connection Enstablished");
                Console.ReadLine();
                bool bLoggedIn = hClient.Login("Mario", "lamer"); 
            }

            
            
        }
    }
}
