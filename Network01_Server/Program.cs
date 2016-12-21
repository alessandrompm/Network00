using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;       //<= il  socket è il tassello fondamentale per quanto riguarda la costruzione di programmi che comunicano in network
using System.Net;
using System.Threading;
using System.IO;
using Network01_Shared;

//Un applicazione server, solitamente definisce un "servizio"
namespace Network01_Server
{
    //Creiamo il nostro handler
    public class ChatUserHandler : ServerOTPC<ChatUserHandler>.ConnectionHandler
    {
        
        public ChatUserHandler()
        {                  
        }

        protected override void OnDataReceived(BinaryReader hReader)
        {

            //byte id             = hReader.ReadByte();
            //short size          = hReader.ReadInt16();

            //string sUsername    = hReader.ReadString();
            try
            {
                string sPassword = hReader.ReadString();
                Console.WriteLine(sPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }


    //Il server
    public class ChatServer : ServerOTPC<ChatUserHandler>
    {
        public ChatServer() : base(50, 512)
        {
        }


        //L'untente che ha effettuato l'operazione è detto contesto e solitamente viene passato come parametro sull'implementazione del metodo associato
        internal void Login(ChatUserHandler hContext, string username, string password)
        {
        }


        internal void JoinChannel(ChatUserHandler hContext, int iChannelID)
        {
            //hContext.IsLoggedIn, <= (critical)
        }

        internal void Message(ChatUserHandler hContext, string sMessage)
        {
            //hContext.CurrentChannel != null (critical)
        }

        internal void LeaveChannel(ChatUserHandler hContext)
        {
            //hContext.CurrentChannel != null (non critical)
        }

        internal void BanUser(ChatUserHandler hContext, int banId)
        {
            //hContext.IsAdmin && Exist(banId)
        }

    }




    class Program
    {

        static void Main(string[] args)
        {
            ChatServer hServer = new ChatServer();

            
            hServer.ServerStarted       += OnServerStarted;
            hServer.ServerStopped       += OnServerStopped;
            hServer.ClientConnected     += OnClientConnected;
            hServer.ClientDisconnected  += OnClientDisconnected;

            hServer.Start(2800);

            Thread.CurrentThread.Join();
        }

        private static void OnClientDisconnected(ChatUserHandler obj)
        {
            Console.WriteLine("OnClientDisconnected");
        }

        private static void OnClientConnected(ChatUserHandler obj)
        {
            Console.WriteLine("OnClientConnected");
        }

        private static void OnServerStopped()
        {
            Console.WriteLine("OnServerStopped");
        }

        private static void OnServerStarted()
        {
            Console.WriteLine("OnServerStarted");
        }
    }
}
